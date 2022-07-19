namespace Estreya.BlishHUD.Shared.State;

using Blish_HUD;
using Blish_HUD.ArcDps;
using Estreya.BlishHUD.Shared.Models.ArcDPS;
using Estreya.BlishHUD.Shared.Models.GW2API.Skills;
using Estreya.BlishHUD.Shared.Threading.Events;
using Humanizer;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

public class ArcDPSState : ManagedState
{
    private SkillState _skillState;

    private ConcurrentQueue<RawCombatEventArgs> _combatEventQueue;

    private bool _notified = false;

    public event EventHandler ArcDPSServiceStopped;

    public event AsyncEventHandler<CombatEvent> AreaCombatEvent;
    public event AsyncEventHandler<CombatEvent> LocalCombatEvent;

    public ArcDPSState(SkillState skillState) : base(saveInterval: -1)
    {
        this._skillState = skillState;
    }

    public override Task Clear()
    {
        _combatEventQueue = new ConcurrentQueue<RawCombatEventArgs>();

        return Task.CompletedTask;
    }

    protected override Task Initialize()
    {
        _combatEventQueue = new ConcurrentQueue<RawCombatEventArgs>();

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
        this._combatEventQueue = null;
    }

    protected override void InternalUpdate(GameTime gameTime)
    {
        if (!GameService.ArcDps.RenderPresent && !_notified)
        {
            Logger.Error("ArcDPS Service stopped.");

            this.ArcDPSServiceStopped?.Invoke(this, EventArgs.Empty);

            _notified = true;

            this.Stop();
        }

        while (this._combatEventQueue.TryDequeue(out RawCombatEventArgs eventData))
        {
            this.ParseCombatEvent(eventData);
        }
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
        this.ArcDps_RawCombatEvent(null, rawCombatEventArgs);
    }

    private void ArcDps_RawCombatEvent(object _, RawCombatEventArgs rawCombatEventArgs)
    {
        try
        {
            this._combatEventQueue.Enqueue(rawCombatEventArgs);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed adding combat event to queue.");
        }
    }

    private void ParseCombatEvent(RawCombatEventArgs rawCombatEventArgs)
    {
        if (!this.Running)
        {
            return;
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
                uint selfInstId = 0;

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
                    selfInstId = ev.SrcInstId;
                }

                if (dst.Self == 1)
                {
                    selfInstId = ev.DstInstId;
                }

                this.HandleNormalCombatEvents(ev, src, dst, skillName, selfInstId, targetAgentId, rawCombatEventArgs.EventType);
                this.HandleActivationEvents(ev, src, dst, skillName, selfInstId, targetAgentId, rawCombatEventArgs.EventType);
                this.HandleStatechangeEvents(ev, src, dst, skillName, selfInstId, targetAgentId, rawCombatEventArgs.EventType);
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
            Logger.Error(ex, "Failed parsing combat event:");
        }
    }

    private void HandleStatechangeEvents(Blish_HUD.ArcDps.Models.Ev ev, Blish_HUD.ArcDps.Models.Ag src, Blish_HUD.ArcDps.Models.Ag dst, string skillName, uint selfInstId, ulong targetAgentId, RawCombatEventArgs.CombatEventType scope)
    {
    }

    private void HandleActivationEvents(Blish_HUD.ArcDps.Models.Ev ev, Blish_HUD.ArcDps.Models.Ag src, Blish_HUD.ArcDps.Models.Ag dst, string skillName, uint selfInstId, ulong targetAgentId, RawCombatEventArgs.CombatEventType scope)
    {
    }

    private void HandleNormalCombatEvents(Blish_HUD.ArcDps.Models.Ev ev, Blish_HUD.ArcDps.Models.Ag src, Blish_HUD.ArcDps.Models.Ag dst, string skillName, ulong selfInstId, ulong targetAgentId, RawCombatEventArgs.CombatEventType scope)
    {
        List<CombatEventType> types = new List<CombatEventType>();

        /* statechange */
        if (ev.IsStateChange != ArcDpsEnums.StateChange.None)
        {
            return;
        }

        /* activation */
        else if (ev.IsActivation != ArcDpsEnums.Activation.None)
        {
            return;
        }

        /* buff remove */
        else if (ev.IsBuffRemove != ArcDpsEnums.BuffRemove.None)
        {
            return;
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

        if (types.Count == 0) types.Add(CombatEventType.NONE);

        Skill skill = null;
        if (types.Count > 0)
        {
            skill = _skillState.GetById((int)ev.SkillId);

            if (skill == null)
            {
                Logger.Debug($"Failed to fetch skill \"{ev.SkillId}\". ArcDPS reports: {skillName}");
                _ = _skillState._missingSkillsFromAPIReportedByArcDPS.TryAdd((int)ev.SkillId, skillName);
            }
            else
            {
                _ = _skillState._missingSkillsFromAPIReportedByArcDPS.TryRemove((int)ev.SkillId, out var unused);
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
            else if (ev.SrcMasterInstId == selfInstId && (/*!Options::get()->outgoingOnlyToTarget || */dst.Id == targetAgentId))
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
                CombatEvent combatEvent = new CombatEvent(ev, src, dst, category, type, CombatEventGroup.NORMAL)
                {
                    Skill = skill,
                };

                this.EmitEvent(combatEvent, scope);
            }
        }
    }

    private void EmitEvent(CombatEvent combatEvent, RawCombatEventArgs.CombatEventType scope)
    {
        try
        {
            switch (scope)
            {
                case RawCombatEventArgs.CombatEventType.Area:
                    // Fire task and don't worry about it.
                    _ = this.AreaCombatEvent?.Invoke(this, combatEvent);
                    break;
                case RawCombatEventArgs.CombatEventType.Local:
                    // Fire task and don't worry about it.
                    _ = this.LocalCombatEvent?.Invoke(this, combatEvent);
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Failed emit event:");
        }
    }
}
