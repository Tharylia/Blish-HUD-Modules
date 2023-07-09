namespace Estreya.BlishHUD.Shared.Services;

using Blish_HUD;
using Blish_HUD.ArcDps;
using Blish_HUD.ArcDps.Models;
using Microsoft.Xna.Framework;
using Models.ArcDPS;
using Models.ArcDPS.Buff;
using Models.ArcDPS.Damage;
using Models.ArcDPS.Heal;
using Models.ArcDPS.Shield;
using Models.ArcDPS.StateChange;
using Models.GW2API.Skills;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using CombatEvent = Models.ArcDPS.CombatEvent;

public class ArcDPSService : ManagedService
{
    private const int MAX_PARSE_PER_LOOP = 50;

    private bool _checkedFirstFrame;
    private bool _lastState = true;
    private ConcurrentQueue<(CombatEvent combatEvent, RawCombatEventArgs.CombatEventType scope)> _parsedCombatEventQueue;

    private ConcurrentQueue<RawCombatEventArgs> _rawCombatEventQueue;

    // Persitent Combat Data
    public ushort _selfInstId;
    private SkillService _skillState;

    public ArcDPSService(ServiceConfiguration configuration, SkillService skillState) : base(configuration)
    {
        this._skillState = skillState;
    }

    /// <summary>
    ///     Gets fired if the <see cref="ArcDpsService" /> has started running. State is compared against the state from last
    ///     update frame.
    /// </summary>
    public event EventHandler Started;

    /// <summary>
    ///     Gets fired if the <see cref="ArcDpsService" /> has stopped running. State is compared against the state from last
    ///     update frame.
    /// </summary>
    public event EventHandler Stopped;

    /// <summary>
    ///     Gets fired if ArcDPS is not available in the first update loop of BlishHUD.
    /// </summary>
    public event EventHandler Unavailable;

    public event EventHandler<CombatEvent> AreaCombatEvent;
    public event EventHandler<CombatEvent> LocalCombatEvent;

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
        if (!this._checkedFirstFrame && !GameService.ArcDps.Running)
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

        GameService.Debug.StartTimeFunc($"{nameof(ArcDPSService)}-UpdateIterateQueue", 60);

        int parseCounter = 0;

        // If parse counter is >= MAX_PARSE_PER_LOOP skip the following parses to the next frame.
        // This prevents BlishHUD from freezing in heavy fights.

        while (parseCounter < MAX_PARSE_PER_LOOP && this._rawCombatEventQueue.TryDequeue(out RawCombatEventArgs eventData))
        {
            foreach (CombatEvent parsedCombatEvent in this.ParseCombatEvent(eventData))
            {
                this.AddSkill(parsedCombatEvent, eventData.CombatEvent.SkillName);
                this.EmitEvent(parsedCombatEvent, eventData.EventType);
            }

            parseCounter++;
        }

        GameService.Debug.StopTimeFunc($"{nameof(ArcDPSService)}-UpdateIterateQueue");
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
        List<CombatEvent> combatEvents = new List<CombatEvent>();

        if (!this.Running)
        {
            return combatEvents;
        }

        try
        {
            Blish_HUD.ArcDps.Models.CombatEvent rawCombatEvent = rawCombatEventArgs.CombatEvent;
            Ev ev = rawCombatEvent.Ev;
            Ag src = rawCombatEvent.Src;
            Ag dst = rawCombatEvent.Dst;
            ulong targetAgentId = 0;

            /* combat event. skillname may be null. non-null skillname will remain static until module is unloaded. refer to evtc notes for complete detail */
            if (ev != null)
            {
                string skillName = rawCombatEvent.SkillName;

                /* default names */
                if (string.IsNullOrWhiteSpace(src.Name))
                {
                    src = new Ag("Unknown Source", src.Id, src.Profession, src.Elite, src.Self, src.Team);
                }

                if (string.IsNullOrWhiteSpace(dst.Name))
                {
                    dst = new Ag("Unknown Target", dst.Id, dst.Profession, dst.Elite, dst.Self, dst.Team);
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

                rawCombatEvent = new Blish_HUD.ArcDps.Models.CombatEvent(ev, src, dst, skillName, rawCombatEvent.Id, rawCombatEvent.Revision);

                combatEvents.AddRange(this.HandleNormalCombatEvents(rawCombatEvent, this._selfInstId, targetAgentId, rawCombatEventArgs.EventType));
                combatEvents.AddRange(this.HandleActivationEvents(rawCombatEvent, this._selfInstId, targetAgentId, rawCombatEventArgs.EventType));
                combatEvents.AddRange(this.HandleStatechangeEvents(rawCombatEvent, this._selfInstId, targetAgentId, rawCombatEventArgs.EventType));
                combatEvents.AddRange(this.HandleBuffEvents(rawCombatEvent, this._selfInstId, targetAgentId, rawCombatEventArgs.EventType));
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

    private List<CombatEvent> HandleStatechangeEvents(Blish_HUD.ArcDps.Models.CombatEvent combatEvent, uint selfInstId, ulong targetAgentId, RawCombatEventArgs.CombatEventType scope)
    {
        List<CombatEvent> combatEvents = new List<CombatEvent>();

        return combatEvents;
    }

    private List<CombatEvent> HandleActivationEvents(Blish_HUD.ArcDps.Models.CombatEvent combatEvent, uint selfInstId, ulong targetAgentId, RawCombatEventArgs.CombatEventType scope)
    {
        List<CombatEvent> combatEvents = new List<CombatEvent>();

        return combatEvents;
    }

    private List<CombatEvent> HandleBuffEvents(Blish_HUD.ArcDps.Models.CombatEvent combatEvent, uint selfInstId, ulong targetAgentId, RawCombatEventArgs.CombatEventType scope)
    {
        List<CombatEvent> combatEvents = new List<CombatEvent>();
        Ev ev = combatEvent.Ev;

        CombatEvent parsedCombatEvent = null;
        CombatEventState state = CombatEvent.GetState(combatEvent.Ev);

        if (state == CombatEventState.BUFFAPPLY)
        {
            // Buff added
            parsedCombatEvent = this.GetCombatEvent(combatEvent, combatEvent.Src.Self == 1 ? CombatEventCategory.PLAYER_OUT : CombatEventCategory.PLAYER_IN, CombatEventType.BUFF);
        }
        else if (state == CombatEventState.BUFFREMOVE)
        {
            // Buff removed
            parsedCombatEvent = this.GetCombatEvent(combatEvent, combatEvent.Src.Self == 1 ? CombatEventCategory.PLAYER_IN : CombatEventCategory.PLAYER_OUT, CombatEventType.BUFF);
        }

        if (parsedCombatEvent != null)
        {
            combatEvents.Add(parsedCombatEvent);
        }

        return combatEvents;
    }

    private List<CombatEvent> HandleNormalCombatEvents(Blish_HUD.ArcDps.Models.CombatEvent combatEvent, ulong selfInstId, ulong targetAgentId, RawCombatEventArgs.CombatEventType scope)
    {
        List<CombatEvent> combatEvents = new List<CombatEvent>();
        List<CombatEventType> types = new List<CombatEventType>();

        Ev ev = combatEvent.Ev;

        /* statechange */
        if (ev.IsStateChange != ArcDpsEnums.StateChange.None)
        {
            return combatEvents;
        }
        /* activation */

        if (ev.IsActivation != ArcDpsEnums.Activation.None)
        {
            return combatEvents;
        }
        /* buff remove */

        if (ev.IsBuffRemove != ArcDpsEnums.BuffRemove.None)
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
                    // Subtract shield value from heal value
                    int buffDmg = ev.BuffDmg - (int)ev.OverStackValue;
                    ev = new Ev(ev.Time, ev.SrcAgent, ev.DstAgent, ev.Value, buffDmg, ev.OverStackValue, ev.SkillId, ev.SrcInstId, ev.DstInstId, ev.SrcMasterInstId, ev.DstMasterInstId, ev.Iff, ev.Buff,
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
                    ev = new Ev(ev.Time, ev.SrcAgent, ev.DstAgent, ev.Value, buffDmg, ev.OverStackValue, ev.SkillId, ev.SrcInstId, ev.DstInstId, ev.SrcMasterInstId, ev.DstMasterInstId, ev.Iff, ev.Buff,
                        ev.Result, ev.IsActivation, ev.IsBuffRemove, ev.IsNinety, ev.IsFifty, ev.IsMoving, ev.IsStateChange, ev.IsFlanking, ev.IsShields, ev.IsOffCycle, ev.Pad61, ev.Pad62, ev.Pad63, ev.Pad64);

                    types.Add(CombatEventType.SHIELD_REMOVE);
                }

                if (ev.BuffDmg < 0)
                {
                    switch (ev.SkillId)
                    {
                        case 723:
                            types.Add(CombatEventType.POISON);
                            break;
                        case 736:
                            types.Add(CombatEventType.BLEEDING);
                            break;
                        case 737:
                            types.Add(CombatEventType.BURNING);
                            break;
                        case 861:
                            types.Add(CombatEventType.CONFUSION);
                            break;
                        case 873:
                            types.Add(CombatEventType.RETALIATION);
                            break;
                        case 19426:
                            types.Add(CombatEventType.TORMENT);
                            break;
                        default:
                            types.Add(CombatEventType.DOT);
                            break;
                    }
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
                    ev = new Ev(ev.Time, ev.SrcAgent, ev.DstAgent, value, ev.BuffDmg, ev.OverStackValue, ev.SkillId, ev.SrcInstId, ev.DstInstId, ev.SrcMasterInstId, ev.DstMasterInstId, ev.Iff, ev.Buff,
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
                    ev = new Ev(ev.Time, ev.SrcAgent, ev.DstAgent, value, ev.BuffDmg, ev.OverStackValue, ev.SkillId, ev.SrcInstId, ev.DstInstId, ev.SrcMasterInstId, ev.DstMasterInstId, ev.Iff, ev.Buff,
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
                    }
                }
            }
        }

        //if (types.Count == 0)
        //{
        //    types.Add(CombatEventType.NONE);
        //}

        combatEvent = new Blish_HUD.ArcDps.Models.CombatEvent(ev, combatEvent.Src, combatEvent.Dst, combatEvent.SkillName, combatEvent.Id, combatEvent.Revision);

        foreach (CombatEventType type in types)
        {
            List<CombatEventCategory> categories = new List<CombatEventCategory>();
            if (combatEvent.Src.Self == 1 /* && (!Options::get()->outgoingOnlyToTarget || dst.Id == targetAgentId)*/)
            {
                if ( /*!Options::get()->selfMessageOnlyIncoming || */combatEvent.Dst.Self != 1)
                {
                    categories.Add(CombatEventCategory.PLAYER_OUT);
                }
            }
            else if (ev.SrcMasterInstId == selfInstId) // && (/*!Options::get()->outgoingOnlyToTarget || */dst.Id == targetAgentId))
            {
                categories.Add(CombatEventCategory.PET_OUT);
            }

            if (combatEvent.Dst.Self == 1)
            {
                categories.Add(CombatEventCategory.PLAYER_IN);
            }
            else if (ev.DstMasterInstId == selfInstId)
            {
                categories.Add(CombatEventCategory.PET_IN);
            }

            foreach (CombatEventCategory category in categories)
            {
                CombatEvent parsedCombatEvent = this.GetCombatEvent(combatEvent, category, type);

                if (parsedCombatEvent != null)
                {
                    combatEvents.Add(parsedCombatEvent);
                }
            }
        }

        return combatEvents;
    }

    private CombatEvent GetCombatEvent(Blish_HUD.ArcDps.Models.CombatEvent combatEvent, CombatEventCategory category, CombatEventType type)
    {
        CombatEventState state = CombatEvent.GetState(combatEvent.Ev);

        switch (state)
        {
            case CombatEventState.NORMAL:
                return this.GetNormalCombatEvent(combatEvent, category, type);
            case CombatEventState.ACTIVATION:
                break;
            case CombatEventState.STATECHANGE:
                return this.GetStateChangeCombatEvent(combatEvent, category, type);
            case CombatEventState.BUFFREMOVE:
                return new BuffRemoveCombatEvent(combatEvent, category, type, CombatEventState.BUFFREMOVE);
            case CombatEventState.BUFFAPPLY:
                return new BuffApplyCombatEvent(combatEvent, category, type, CombatEventState.BUFFAPPLY);
        }

        return null;
    }

    private CombatEvent GetNormalCombatEvent(Blish_HUD.ArcDps.Models.CombatEvent combatEvent, CombatEventCategory category, CombatEventType type)
    {
        switch (type)
        {
            case CombatEventType.NONE:
            case CombatEventType.PHYSICAL:
            case CombatEventType.CRIT:
            case CombatEventType.BLEEDING:
            case CombatEventType.BURNING:
            case CombatEventType.POISON:
            case CombatEventType.CONFUSION:
            case CombatEventType.RETALIATION:
            case CombatEventType.TORMENT:
            case CombatEventType.DOT:
            case CombatEventType.BLOCK:
            case CombatEventType.EVADE:
            case CombatEventType.INVULNERABLE:
            case CombatEventType.MISS:
                return new DamageCombatEvent(combatEvent, category, type, CombatEventState.NORMAL);
            case CombatEventType.HEAL:
            case CombatEventType.HOT:
                return new HealCombatEvent(combatEvent, category, type, CombatEventState.NORMAL);
            case CombatEventType.SHIELD_RECEIVE:
            case CombatEventType.SHIELD_REMOVE:
                return new BarrierCombatEvent(combatEvent, category, type, CombatEventState.NORMAL);
        }

        return null;
    }

    private CombatEvent GetStateChangeCombatEvent(Blish_HUD.ArcDps.Models.CombatEvent combatEvent, CombatEventCategory category, CombatEventType type)
    {
        switch (combatEvent.Ev.IsStateChange)
        {
            case ArcDpsEnums.StateChange.EnterCombat:
                return new EnterCombatEvent(combatEvent, category, type, CombatEventState.STATECHANGE);
            case ArcDpsEnums.StateChange.ExitCombat:
                return new ExitCombatEvent(combatEvent, category, type, CombatEventState.STATECHANGE);
            case ArcDpsEnums.StateChange.ChangeUp:
                break;
            case ArcDpsEnums.StateChange.ChangeDead:
                break;
            case ArcDpsEnums.StateChange.ChangeDown:
                break;
            case ArcDpsEnums.StateChange.Spawn:
                break;
            case ArcDpsEnums.StateChange.Despawn:
                break;
            case ArcDpsEnums.StateChange.HealthUpdate:
                break;
            case ArcDpsEnums.StateChange.LogStart:
                break;
            case ArcDpsEnums.StateChange.LogEnd:
                break;
            case ArcDpsEnums.StateChange.WeaponSwap:
                break;
            case ArcDpsEnums.StateChange.MaxHealthUpdate:
                break;
            case ArcDpsEnums.StateChange.PointOfView:
                break;
            case ArcDpsEnums.StateChange.Language:
                break;
            case ArcDpsEnums.StateChange.GWBuild:
                break;
            case ArcDpsEnums.StateChange.ShardId:
                break;
            case ArcDpsEnums.StateChange.Reward:
                break;
            case ArcDpsEnums.StateChange.BuffInitial:
                break;
            case ArcDpsEnums.StateChange.Position:
                break;
            case ArcDpsEnums.StateChange.Velocity:
                break;
            case ArcDpsEnums.StateChange.Rotation:
                break;
            case ArcDpsEnums.StateChange.TeamChange:
                break;
            case ArcDpsEnums.StateChange.AttackTarget:
                break;
            case ArcDpsEnums.StateChange.Targetable:
                break;
            case ArcDpsEnums.StateChange.MapID:
                break;
            case ArcDpsEnums.StateChange.ReplInfo:
                break;
            case ArcDpsEnums.StateChange.StackActive:
                break;
            case ArcDpsEnums.StateChange.StackReset:
                break;
            case ArcDpsEnums.StateChange.Guild:
                break;
            case ArcDpsEnums.StateChange.BuffInfo:
                break;
            case ArcDpsEnums.StateChange.BuffFormula:
                break;
            case ArcDpsEnums.StateChange.SkillInfo:
                break;
            case ArcDpsEnums.StateChange.SkillTiming:
                break;
            case ArcDpsEnums.StateChange.BreakbarState:
                break;
            case ArcDpsEnums.StateChange.BreakbarPercent:
                break;
            case ArcDpsEnums.StateChange.Error:
                break;
            case ArcDpsEnums.StateChange.Tag:
                break;
            case ArcDpsEnums.StateChange.Unknown:
                break;
        }

        return null;
    }

    private void AddSkill(CombatEvent combatEvent, string skillNameByArcDPS)
    {
        //Skill skill = this._skillState.GetById((int)combatEvent.SkillId);
        Skill skill = this._skillState.GetBy(skill => skill.Id == (int)combatEvent.SkillId && skill.Name == skillNameByArcDPS);

        if (skill == null)
        {
            bool added = this._skillState.AddMissingSkill((int)combatEvent.SkillId, skillNameByArcDPS);
            if (added)
            {
                this.Logger.Debug($"Failed to fetch skill \"{combatEvent.SkillId}\". ArcDPS reports: {skillNameByArcDPS}");
            }

            skill = SkillService.UnknownSkill;
        }
        else
        {
            this._skillState.RemoveMissingSkill((int)combatEvent.SkillId);
        }

        combatEvent.Skill = skill;
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
            this.Logger.Error(ex, "Failed emit event:");
        }
    }
}