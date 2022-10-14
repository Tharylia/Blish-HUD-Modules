namespace Estreya.BlishHUD.Shared.State;

using Blish_HUD;
using Blish_HUD.ArcDps;
using Estreya.BlishHUD.Shared.Models.ArcDPS;
using Estreya.BlishHUD.Shared.Models.GW2API.Skills;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

public class ArcDPSState : ManagedState
{
    private SkillState _skillState;

    private ConcurrentQueue<RawCombatEventArgs> _rawCombatEventQueue;
    private ConcurrentQueue<(CombatEvent combatEvent, RawCombatEventArgs.CombatEventType scope)> _parsedCombatEventQueue;

    private bool _checkedFirstFrame = false;
    private bool _lastState = true;

    // Persitent Combat Data
    public ushort _selfInstId;

    /// <summary>
    /// Gets fired if the <see cref="ArcDpsService"/> has started running. State is compared against the state from last update frame.
    /// </summary>
    public event EventHandler Started;

    /// <summary>
    /// Gets fired if the <see cref="ArcDpsService"/> has stopped running. State is compared against the state from last update frame.
    /// </summary>
    public event EventHandler Stopped;

    /// <summary>
    /// Gets fired if ArcDPS is not available in the first update loop of BlishHUD.
    /// </summary>
    public event EventHandler Unavailable;

    public event EventHandler<CombatEvent> AreaCombatEvent;
    public event EventHandler<CombatEvent> LocalCombatEvent;

    public ArcDPSState(StateConfiguration configuration, SkillState skillState) : base(configuration)
    {
        this._skillState = skillState;
    }

    protected override Task Clear()
    {
        this._rawCombatEventQueue = new ConcurrentQueue<RawCombatEventArgs>();
        this._parsedCombatEventQueue = new ConcurrentQueue<(CombatEvent combatEvent, RawCombatEventArgs.CombatEventType scope)>();

        return Task.CompletedTask;
    }

    protected override Task Initialize()
    {
        this._rawCombatEventQueue = new ConcurrentQueue<RawCombatEventArgs>();
        this._parsedCombatEventQueue = new ConcurrentQueue<(CombatEvent combatEvent, RawCombatEventArgs.CombatEventType scope)>();

        GameService.ArcDps.RawCombatEvent += this.ArcDps_RawCombatEvent;

        return Task.CompletedTask;
    }

    protected override Task InternalReload()
    {
        return Task.CompletedTask;
    }

    protected override void InternalUnload()
    {
        GameService.ArcDps.RawCombatEvent -= this.ArcDps_RawCombatEvent;
        this._skillState = null;
        this._rawCombatEventQueue = null;
        this._parsedCombatEventQueue = null;
    }

    protected override void InternalUpdate(GameTime gameTime)
    {
        if (!_checkedFirstFrame && !GameService.ArcDps.Running)
        {
            this.Logger.Debug("ArcDPS Service not available.");

            this.Unavailable?.Invoke(this, EventArgs.Empty);

            this._checkedFirstFrame = true;
            this._lastState = false;
                }
        else if (GameService.ArcDps.Running != this._lastState)
        {
            if (GameService.ArcDps.Running)
                {
                    this.Logger.Debug("ArcDPS Service started.");

                    this.Started?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    this.Logger.Debug("ArcDPS Service stopped.");

                    this.Stopped?.Invoke(this, EventArgs.Empty);
                }

            this._lastState = GameService.ArcDps.Running;
        }

        GameService.Debug.StartTimeFunc($"{nameof(ArcDPSState)}-UpdateIterateQueue", 60);
        while (this._rawCombatEventQueue.TryDequeue(out RawCombatEventArgs eventData))
        {
            foreach (var parsedCombatEvent in this.ParseCombatEvent(eventData))
            {
                this.EmitEvent(parsedCombatEvent, eventData.EventType);
            }
        }
        GameService.Debug.StopTimeFunc($"{nameof(ArcDPSState)}-UpdateIterateQueue");
    }

    protected override Task Load()
    {
        return Task.CompletedTask;
    }

    protected override Task Save()
    {
        return Task.CompletedTask;
    }

    public void SimulateCombatEvent(RawCombatEventArgs rawCombatEventArgs)
    {
        if (!this.Running)
        {
            return;
        }

        this.ArcDps_RawCombatEvent(null, rawCombatEventArgs);
    }

    private void ArcDps_RawCombatEvent(object _, RawCombatEventArgs rawCombatEventArgs)
    {
        try
        {
            this._rawCombatEventQueue.Enqueue(rawCombatEventArgs);
        }
        catch (Exception ex)
        {
            this.Logger.Error(ex, "Failed adding combat event to queue.");
        }
    }

    private List<CombatEvent> ParseCombatEvent(RawCombatEventArgs rawCombatEventArgs)
    {
        var combatEvents = new List<CombatEvent>();

        if (!this.Running)
        {
            return combatEvents;
        }

        try
        {
            Blish_HUD.ArcDps.Models.CombatEvent rawCombatEvent = rawCombatEventArgs.CombatEvent;
            Blish_HUD.ArcDps.Models.Ev ev = rawCombatEvent.Ev;
            Blish_HUD.ArcDps.Models.Ag src = rawCombatEvent.Src;
            Blish_HUD.ArcDps.Models.Ag dst = rawCombatEvent.Dst;
            ulong targetAgentId = 0;

            /* combat event. skillname may be null. non-null skillname will remain static until module is unloaded. refer to evtc notes for complete detail */
            if (ev != null)
            {
                string skillName = rawCombatEvent.SkillName;

                /* default names */
                if (string.IsNullOrWhiteSpace(src.Name))
                {
                    src = new Blish_HUD.ArcDps.Models.Ag("Unknown Source", src.Id, src.Profession, src.Elite, src.Self, src.Team);
                }

                if (string.IsNullOrWhiteSpace(dst.Name))
                {
                    dst = new Blish_HUD.ArcDps.Models.Ag("Unknown Target", dst.Id, dst.Profession, dst.Elite, dst.Self, dst.Team);
                }

                if (string.IsNullOrWhiteSpace(skillName))
                {
                    skillName = "Unknown Skill";
                }

                if (src.Self == 1)
                {
                    this._selfInstId = ev.SrcInstId;
                }

                if (dst.Self == 1)
                {
                    this._selfInstId = ev.DstInstId;
                }

                combatEvents.AddRange(this.HandleNormalCombatEvents(ev, src, dst, skillName, this._selfInstId, targetAgentId, rawCombatEventArgs.EventType));
                combatEvents.AddRange(this.HandleActivationEvents(ev, src, dst, skillName, this._selfInstId, targetAgentId, rawCombatEventArgs.EventType));
                combatEvents.AddRange(this.HandleStatechangeEvents(ev, src, dst, skillName, this._selfInstId, targetAgentId, rawCombatEventArgs.EventType));
            }
            else
            {
                if (src != null)
                {
                    targetAgentId = src.Id;
                }
            }
        }
        catch (Exception ex)
        {
            this.Logger.Error(ex, "Failed parsing combat event:");
        }

        return combatEvents;
    }

    private List<CombatEvent> HandleStatechangeEvents(Blish_HUD.ArcDps.Models.Ev ev, Blish_HUD.ArcDps.Models.Ag src, Blish_HUD.ArcDps.Models.Ag dst, string skillName, uint selfInstId, ulong targetAgentId, RawCombatEventArgs.CombatEventType scope)
    {
        var combatEvents = new List<CombatEvent>();

        return combatEvents;
    }

    private List<CombatEvent> HandleActivationEvents(Blish_HUD.ArcDps.Models.Ev ev, Blish_HUD.ArcDps.Models.Ag src, Blish_HUD.ArcDps.Models.Ag dst, string skillName, uint selfInstId, ulong targetAgentId, RawCombatEventArgs.CombatEventType scope)
    {
        var combatEvents = new List<CombatEvent>();

        return combatEvents;
    }

    private List<CombatEvent> HandleNormalCombatEvents(Blish_HUD.ArcDps.Models.Ev ev, Blish_HUD.ArcDps.Models.Ag src, Blish_HUD.ArcDps.Models.Ag dst, string skillName, ulong selfInstId, ulong targetAgentId, RawCombatEventArgs.CombatEventType scope)
    {
        var combatEvents = new List<CombatEvent>();
        List<CombatEventType> types = new List<CombatEventType>();

        /* statechange */
        if (ev.IsStateChange != ArcDpsEnums.StateChange.None)
        {
            return combatEvents;
        }

        /* activation */
        else if (ev.IsActivation != ArcDpsEnums.Activation.None)
        {
            return combatEvents;
        }

        /* buff remove */
        else if (ev.IsBuffRemove != ArcDpsEnums.BuffRemove.None)
        {
            return combatEvents;
        }

        if (ev.Buff)
        {
            bool buffAdded = ev.IsBuffRemove == ArcDpsEnums.BuffRemove.None;

            if (ev.BuffDmg > 0)
            {
                if (ev.OverStackValue != 0)
                {
                    int buffDmg = ev.BuffDmg - (int)ev.OverStackValue;
                    ev = new Blish_HUD.ArcDps.Models.Ev(ev.Time, ev.SrcAgent, ev.DstAgent, ev.Value, buffDmg, ev.OverStackValue, ev.SkillId, ev.SrcInstId, ev.DstInstId, ev.SrcMasterInstId, ev.DstMasterInstId, ev.Iff, ev.Buff,
                         ev.Result, ev.IsActivation, ev.IsBuffRemove, ev.IsNinety, ev.IsFifty, ev.IsMoving, ev.IsStateChange, ev.IsFlanking, ev.IsShields, ev.IsOffCycle, ev.Pad61, ev.Pad62, ev.Pad63, ev.Pad64);

                    types.Add(CombatEventType.SHIELD_RECEIVE);
                }
                if (ev.BuffDmg > 0)
                {
                    types.Add(CombatEventType.HOT);
                }
            }
            else if (ev.BuffDmg < 0)
            {
                if (ev.OverStackValue > 0)
                {
                    int buffDmg = ev.BuffDmg + (int)ev.OverStackValue;
                    ev = new Blish_HUD.ArcDps.Models.Ev(ev.Time, ev.SrcAgent, ev.DstAgent, ev.Value, buffDmg, ev.OverStackValue, ev.SkillId, ev.SrcInstId, ev.DstInstId, ev.SrcMasterInstId, ev.DstMasterInstId, ev.Iff, ev.Buff,
                         ev.Result, ev.IsActivation, ev.IsBuffRemove, ev.IsNinety, ev.IsFifty, ev.IsMoving, ev.IsStateChange, ev.IsFlanking, ev.IsShields, ev.IsOffCycle, ev.Pad61, ev.Pad62, ev.Pad63, ev.Pad64);

                    types.Add(CombatEventType.SHIELD_REMOVE);
                }

                if (ev.BuffDmg < 0)
                {
                    switch (ev.SkillId)
                    {
                        case 723: types.Add(CombatEventType.POISON); break;
                        case 736: types.Add(CombatEventType.BLEEDING); break;
                        case 737: types.Add(CombatEventType.BURNING); break;
                        case 861: types.Add(CombatEventType.CONFUSION); break;
                        case 873: types.Add(CombatEventType.RETALIATION); break;
                        case 19426: types.Add(CombatEventType.TORMENT); break;
                        default: types.Add(CombatEventType.DOT); break;
                    }
                }
            }
            else
            {
                // Buff Dmg == 0
                switch (ev.SkillId)
                {
                    case 717: types.Add(CombatEventType.PROTECTION); break;
                    case 718: types.Add(CombatEventType.REGENERATION); break;
                    case 719: types.Add(CombatEventType.SWIFTNESS); break;
                    case 725: types.Add(CombatEventType.FURY); break;
                    case 726: types.Add(CombatEventType.VIGOR); break;
                    case 740: types.Add(CombatEventType.MIGHT); break;
                    case 743: types.Add(CombatEventType.AEGIS); break;
                    case 873: types.Add(CombatEventType.RESOLUTION); break;
                    case 1122: types.Add(CombatEventType.STABILITY); break;
                    case 1187: types.Add(CombatEventType.QUICKNESS); break;
                    case 26980: types.Add(CombatEventType.RESISTENCE); break;
                    case 30328: types.Add(CombatEventType.ALACRITY); break;
                    default: types.Add(CombatEventType.BUFF); break;
                }
            }
        }
        else
        {
            if (ev.Value > 0)
            {
                if (ev.OverStackValue != 0)
                {
                    int value = ev.Value + (int)ev.OverStackValue;
                    ev = new Blish_HUD.ArcDps.Models.Ev(ev.Time, ev.SrcAgent, ev.DstAgent, value, ev.BuffDmg, ev.OverStackValue, ev.SkillId, ev.SrcInstId, ev.DstInstId, ev.SrcMasterInstId, ev.DstMasterInstId, ev.Iff, ev.Buff,
                         ev.Result, ev.IsActivation, ev.IsBuffRemove, ev.IsNinety, ev.IsFifty, ev.IsMoving, ev.IsStateChange, ev.IsFlanking, ev.IsShields, ev.IsOffCycle, ev.Pad61, ev.Pad62, ev.Pad63, ev.Pad64);

                    types.Add(CombatEventType.SHIELD_RECEIVE);
                }
                if (ev.Value > 0)
                {
                    types.Add(CombatEventType.HEAL);
                }
            }
            else
            {
                if (ev.OverStackValue > 0)
                {
                    int value = ev.Value + (int)ev.OverStackValue;
                    ev = new Blish_HUD.ArcDps.Models.Ev(ev.Time, ev.SrcAgent, ev.DstAgent, value, ev.BuffDmg, ev.OverStackValue, ev.SkillId, ev.SrcInstId, ev.DstInstId, ev.SrcMasterInstId, ev.DstMasterInstId, ev.Iff, ev.Buff,
                         ev.Result, ev.IsActivation, ev.IsBuffRemove, ev.IsNinety, ev.IsFifty, ev.IsMoving, ev.IsStateChange, ev.IsFlanking, ev.IsShields, ev.IsOffCycle, ev.Pad61, ev.Pad62, ev.Pad63, ev.Pad64);

                    types.Add(CombatEventType.SHIELD_REMOVE);
                }

                if (ev.OverStackValue <= 0 || ev.Value <= 0)
                {
                    switch (ev.Result)
                    {
                        case (byte)CombatEventResult.GLANCE:
                        case (byte)CombatEventResult.INTERRUPT:
                        case (byte)CombatEventResult.NORMAL:
                            types.Add(CombatEventType.PHYSICAL);
                            break;
                        case (byte)CombatEventResult.CRIT:
                            types.Add(CombatEventType.CRIT);
                            break;
                        case (byte)CombatEventResult.BLOCK:
                            types.Add(CombatEventType.BLOCK);
                            break;
                        case (byte)CombatEventResult.EVADE:
                            types.Add(CombatEventType.EVADE);
                            break;
                        case (byte)CombatEventResult.ABSORB:
                            types.Add(CombatEventType.INVULNERABLE);
                            break;
                        case (byte)CombatEventResult.BLIND:
                            types.Add(CombatEventType.MISS);
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        //if (types.Count == 0)
        //{
        //    types.Add(CombatEventType.NONE);
        //}

        Skill skill = null;
        if (types.Count > 0)
        {
            skill = this._skillState.GetById((int)ev.SkillId);

            if (skill == null)
            {
               var added = this._skillState.AddMissingSkill((int)ev.SkillId, skillName);
                if (added)
                {
                    this.Logger.Debug($"Failed to fetch skill \"{ev.SkillId}\". ArcDPS reports: {skillName}");
                }
            }
            else
            {
                this._skillState.RemoveMissingSkill((int)ev.SkillId);
            }
        }

        foreach (CombatEventType type in types)
        {
            List<CombatEventCategory> categories = new List<CombatEventCategory>();
            if (src.Self == 1/* && (!Options::get()->outgoingOnlyToTarget || dst.Id == targetAgentId)*/)
            {
                if (/*!Options::get()->selfMessageOnlyIncoming || */dst.Self != 1)
                {
                    categories.Add(CombatEventCategory.PLAYER_OUT);
                }
            }
            else if (ev.SrcMasterInstId == selfInstId)// && (/*!Options::get()->outgoingOnlyToTarget || */dst.Id == targetAgentId))
            {
                categories.Add(CombatEventCategory.PET_OUT);
            }

            if (dst.Self == 1)
            {
                categories.Add(CombatEventCategory.PLAYER_IN);
            }
            else if (ev.DstMasterInstId == selfInstId)
            {
                categories.Add(CombatEventCategory.PET_IN);
            }

            foreach (CombatEventCategory category in categories)
            {
                CombatEvent combatEvent = new CombatEvent(ev, src, dst, category, type, CombatEventState.NORMAL)
                {
                    Skill = skill,
                };

                combatEvents.Add(combatEvent);
            }
        }

        return combatEvents;
    }

    private void EmitEvent(CombatEvent combatEvent, RawCombatEventArgs.CombatEventType scope)
    {
        try
        {
            switch (scope)
            {
                case RawCombatEventArgs.CombatEventType.Area:
                    // Fire task and don't worry about it.
                    this.AreaCombatEvent?.Invoke(this, combatEvent);
                    break;
                case RawCombatEventArgs.CombatEventType.Local:
                    // Fire task and don't worry about it.
                    this.LocalCombatEvent?.Invoke(this, combatEvent);
                    break;
            }
        }
        catch (Exception ex)
        {
            this.Logger.Error(ex, $"Failed emit event:");
        }
    }
}
