namespace Estreya.BlishHUD.Shared.Services;

using Blish_HUD;
using Blish_HUD.Settings;
using Estreya.BlishHUD.Shared.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class SettingEventService : ManagedService
{
    private static readonly Logger _logger = Logger.GetLogger<SettingEventService>();

    private List<(SettingEntry SettingEntry, IComplianceRequisite ComplianceRequisite)> _registeredForDisabledUpdates;

    private List<(SettingEntry SettingEntry, IComplianceRequisite ComplianceRequisite)> _registeredForRangeUpdates;

    private AsyncLock _disabledStateLock = new AsyncLock();
    private AsyncLock _rangeStateLock = new AsyncLock();

    public SettingEventService(ServiceConfiguration configuration) : base(configuration)
    {
    }

    public event EventHandler<ComplianceUpdated> RangeUpdated;
    public event EventHandler<ComplianceUpdated> DisabledUpdated;

    protected override Task Initialize()
    {
        this._registeredForRangeUpdates = new List<(SettingEntry SettingEntry, IComplianceRequisite ComplianceRequisite)>();
        this._registeredForDisabledUpdates = new List<(SettingEntry SettingEntry, IComplianceRequisite ComplianceRequisite)>();

        return Task.CompletedTask;
    }

    protected override void InternalUnload()
    {
        using (this._rangeStateLock.Lock())
        {
            this._registeredForRangeUpdates?.Clear();
            this._registeredForRangeUpdates = null;
        }

        using (this._disabledStateLock.Lock())
        {
            this._registeredForDisabledUpdates?.Clear();
            this._registeredForDisabledUpdates = null;
        }
    }

    protected override void InternalUpdate(GameTime gameTime)
    {
        this.CheckRangeUpdates();
        this.CheckDisabledUpdates();
    }

    protected override Task Load()
    {
        return Task.CompletedTask;
    }

    public void AddForRangeCheck(SettingEntry settingEntry, IComplianceRequisite defaultRange = null)
    {
        if (settingEntry == null)
        {
            throw new ArgumentNullException(nameof(settingEntry));
        }

        using (this._rangeStateLock.Lock())
        {
            if (!this._registeredForRangeUpdates.Any(p => p.SettingEntry?.EntryKey == settingEntry.EntryKey))
            {
                this._registeredForRangeUpdates.Add((settingEntry, defaultRange));
                _logger.Debug($"Started tracking setting \"{settingEntry.EntryKey}\" for range updates.");
            }
        }
    }

    public void RemoveFromRangeCheck(SettingEntry settingEntry)
    {
        if (settingEntry == null)
        {
            throw new ArgumentNullException(nameof(settingEntry));
        }

        using (this._rangeStateLock.Lock())
        {
            this._registeredForRangeUpdates.RemoveAll(p => p.SettingEntry?.EntryKey == settingEntry.EntryKey);
        }

        _logger.Debug($"Stopped tracking setting \"{settingEntry.EntryKey}\" for range updates.");
    }

    public void AddForDisabledCheck(SettingEntry settingEntry, IComplianceRequisite defaultRange = null)
    {
        if (settingEntry == null)
        {
            throw new ArgumentNullException(nameof(settingEntry));
        }

        using (this._disabledStateLock.Lock())
        {
            if (!this._registeredForDisabledUpdates.Any(p => p.SettingEntry?.EntryKey == settingEntry.EntryKey))
            {
                this._registeredForDisabledUpdates.Add((settingEntry, defaultRange));
                _logger.Debug($"Started tracking setting \"{settingEntry.EntryKey}\" for disabled updates.");
            }
        }
    }

    public void RemoveFromDisabledCheck(SettingEntry settingEntry)
    {
        if (settingEntry == null)
        {
            throw new ArgumentNullException(nameof(settingEntry));
        }

        using (this._disabledStateLock.Lock())
        {
            this._registeredForDisabledUpdates.RemoveAll(p => p.SettingEntry?.EntryKey == settingEntry.EntryKey);
        }

        _logger.Debug($"Stopped tracking setting \"{settingEntry.EntryKey}\" for disabled updates.");
    }

    private void CheckRangeUpdates()
    {
        if (!this._rangeStateLock.IsFree()) return; // Skip this loop

        using (this._rangeStateLock.Lock())
        {
            for (int i = 0; i < this._registeredForRangeUpdates.Count; i++)
            {
                (SettingEntry SettingEntry, IComplianceRequisite ComplianceRequisite) settingPair = this._registeredForRangeUpdates[i];

                bool changed = false;

                SettingEntry setting = settingPair.SettingEntry;
                IComplianceRequisite priorRange = settingPair.ComplianceRequisite;
                IEnumerable<IComplianceRequisite> ranges = setting.GetComplianceRequisite();

                if (setting is SettingEntry<int> or SettingEntry<float>)
                {
                    IEnumerable<IComplianceRequisite> numberRanges = ranges.Where(r => r is IntRangeRangeComplianceRequisite or FloatRangeRangeComplianceRequisite);
                    if (!numberRanges.Any())
                    {
                        if (priorRange != null)
                        {
                            settingPair.ComplianceRequisite = null;
                            changed = true;
                        }
                    }
                    else
                    {
                        IComplianceRequisite numberRange = numberRanges.First();
                        settingPair.ComplianceRequisite = numberRange;
                        if (priorRange != numberRange)
                        {
                            changed = true;
                        }
                    }
                }

                if (changed)
                {
                    try
                    {
                        this.RangeUpdated?.Invoke(this, new ComplianceUpdated(setting, settingPair.ComplianceRequisite));
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }
    }

    private void CheckDisabledUpdates()
    {
        if (!this._disabledStateLock.IsFree()) return; // Skip this loop

        using (this._disabledStateLock.Lock())
        {
            for (int i = 0; i < this._registeredForDisabledUpdates.Count; i++)
            {
                (SettingEntry SettingEntry, IComplianceRequisite ComplianceRequisite) settingPair = this._registeredForDisabledUpdates[i];

                bool changed = false;

                SettingEntry setting = settingPair.SettingEntry;
                IComplianceRequisite priorRange = settingPair.ComplianceRequisite;
                IEnumerable<IComplianceRequisite> ranges = setting.GetComplianceRequisite();

                IEnumerable<IComplianceRequisite> disabledRanges = ranges.Where(r => r is SettingDisabledComplianceRequisite);
                if (!disabledRanges.Any())
                {
                    if (priorRange != null)
                    {
                        settingPair.ComplianceRequisite = new SettingDisabledComplianceRequisite(false);
                        changed = true;
                    }
                }
                else
                {
                    IComplianceRequisite disabledRange = disabledRanges.First();
                    settingPair.ComplianceRequisite = disabledRange;
                    if (priorRange != disabledRange)
                    {
                        changed = true;
                    }
                }

                if (changed)
                {
                    try
                    {
                        this.DisabledUpdated?.Invoke(this, new ComplianceUpdated(setting, settingPair.ComplianceRequisite));
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }
    }
}

public class ComplianceUpdated
{
    public ComplianceUpdated(SettingEntry settingEntry, IComplianceRequisite newCompliance)
    {
        this.SettingEntry = settingEntry;
        this.NewCompliance = newCompliance;
    }

    public SettingEntry SettingEntry { get; }
    public IComplianceRequisite NewCompliance { get; }
}