namespace Estreya.BlishHUD.Shared.State;

using Blish_HUD;
using Blish_HUD.ArcDps;
using Estreya.BlishHUD.Shared.Models.ArcDPS;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

public class ArcDPSState : ManagedState
{
    private SkillState _skillState;
    private Dictionary<uint, uint> RemappedSkills = new Dictionary<uint, uint>();

    public event EventHandler<CombatEvent> AreaCombatEvent;
    public event EventHandler<CombatEvent> LocalCombatEvent;

    public ArcDPSState(SkillState skillState) : base(saveInterval: -1)
    {
        this._skillState = skillState;
    }

    public override Task Clear()
    {
        return Task.CompletedTask;
    }

    protected override Task Initialize()
    {
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
    }

    protected override void InternalUpdate(GameTime gameTime) { }

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
            string skillName = rawCombatEvent.SkillName;
            uint selfInstID = 0;
            ulong targetAgentId = 0;
            List<CombatEventType> types = new List<CombatEventType>();
            CombatEventCategory? category = null;

            /* combat event. skillname may be null. non-null skillname will remain static until module is unloaded. refer to evtc notes for complete detail */
            if (ev != null)
            {
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
                    selfInstID = ev.SrcInstId;
                }

                if (dst.Self == 1)
                {
                    selfInstID = ev.DstInstId;
                }

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
                                case 736: types.Add(CombatEventType.BLEEDING); break;
                                case 737: types.Add(CombatEventType.BURNING); break;
                                case 723: types.Add(CombatEventType.POISON); break;
                                case 861: types.Add(CombatEventType.CONFUSION); break;
                                case 873: types.Add(CombatEventType.RETALIATION); break;
                                case 19426: types.Add(CombatEventType.TORMENT); break;
                                default: types.Add(CombatEventType.DOT); break;
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
                        if (ev.OverStackValue <= 0 || ev.Value < 0)
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

                if (types.Count > 0)
                {
                    uint skillId = this.RemapSkillID(ev.SkillId);
                    ev = new Blish_HUD.ArcDps.Models.Ev(ev.Time, ev.SrcAgent, ev.DstAgent, ev.Value, ev.BuffDmg, ev.OverStackValue, skillId, ev.SrcInstId, ev.DstInstId, ev.SrcMasterInstId, ev.DstMasterInstId, ev.Iff, ev.Buff,
                         ev.Result, ev.IsActivation, ev.IsBuffRemove, ev.IsNinety, ev.IsFifty, ev.IsMoving, ev.IsStateChange, ev.IsFlanking, ev.IsShields, ev.IsOffCycle, ev.Pad61, ev.Pad62, ev.Pad63, ev.Pad64);

                }

                foreach (CombatEventType type in types)
                {
                    if (src.Self == 1/* && (!Options::get()->outgoingOnlyToTarget || dst.Id == targetAgentId)*/)
                    {
                        if (/*!Options::get()->selfMessageOnlyIncoming || */dst.Self != 1)
                        {
                            category = CombatEventCategory.PLAYER_OUT;
                        }
                    }
                    else if (ev.SrcMasterInstId == selfInstID && (/*!Options::get()->outgoingOnlyToTarget || */dst.Id == targetAgentId))
                    {
                        category = CombatEventCategory.PET_OUT;
                    }

                    if (dst.Self == 1)
                    {
                        category = CombatEventCategory.PLAYER_IN;
                    }
                    else if (ev.DstMasterInstId == selfInstID)
                    {
                        category = CombatEventCategory.PET_IN;
                    }

                    if (category.HasValue)
                    {
                        CombatEvent combatEvent = new CombatEvent(ev, src, dst, category.Value, type)
                        {
                            Skill = _skillState.GetById((int)ev.SkillId),
                        };

                        if (combatEvent.Skill == null)
                        {
                            Logger.Debug($"Failed to fetch skill \"{ev.SkillId}\". ArcDPS reports: {skillName}");
                        }

                        this.EmitEvent(combatEvent, rawCombatEventArgs.EventType);
                    }
                }
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

    private uint RemapSkillID(uint skillId)
    {
        if (!this.RemappedSkills.ContainsKey(skillId))
        {
            return skillId;
        }

        return this.RemappedSkills[skillId];
    }

    private void EmitEvent(CombatEvent combatEvent, RawCombatEventArgs.CombatEventType scope)
    {
        try
        {
            switch (scope)
            {
                case RawCombatEventArgs.CombatEventType.Area:
                    this.AreaCombatEvent?.Invoke(this, combatEvent);
                    break;
                case RawCombatEventArgs.CombatEventType.Local:
                    this.LocalCombatEvent?.Invoke(this, combatEvent);
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Failed emit event:");
        }
    }
}
