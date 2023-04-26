namespace Estreya.BlishHUD.Shared.Services
{
    using Blish_HUD;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.Shared.Extensions;
    using Microsoft.Xna.Framework;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class SettingEventService : ManagedService
    {
        private static readonly Logger _logger = Logger.GetLogger<SettingEventService>();

        private List<(SettingEntry SettingEntry, IComplianceRequisite ComplianceRequisite)> _registeredForRangeUpdates;

        private List<(SettingEntry SettingEntry, IComplianceRequisite ComplianceRequisite)> _registeredForDisabledUpdates;

        public event EventHandler<ComplianceUpdated> RangeUpdated;
        public event EventHandler<ComplianceUpdated> DisabledUpdated;

        public SettingEventService(ServiceConfiguration configuration) : base(configuration)
        {
        }

        protected override Task Initialize()
        {
            this._registeredForRangeUpdates = new List<(SettingEntry SettingEntry, IComplianceRequisite ComplianceRequisite)>();
            this._registeredForDisabledUpdates = new List<(SettingEntry SettingEntry, IComplianceRequisite ComplianceRequisite)>();

            return Task.CompletedTask;
        }

        protected override void InternalUnload()
        {
            this._registeredForRangeUpdates?.Clear();
            this._registeredForRangeUpdates = null;

            this._registeredForDisabledUpdates?.Clear();
            this._registeredForDisabledUpdates = null;
        }

        protected override void InternalUpdate(GameTime gameTime)
        {
            this.CheckRangeUpdates();
            this.CheckDisabledUpdates();
        }

        protected override Task Load() => Task.CompletedTask;

        public void AddForRangeCheck(SettingEntry settingEntry, IComplianceRequisite defaultRange = null)
        {
            if (settingEntry == null)
            {
                throw new ArgumentNullException(nameof(settingEntry));
            }

            if (!this._registeredForRangeUpdates.Any(p => p.SettingEntry.EntryKey == settingEntry.EntryKey))
            {
                this._registeredForRangeUpdates.Add((settingEntry, defaultRange));
                _logger.Debug($"Started tracking setting \"{settingEntry.EntryKey}\" for range updates.");
            }
        }

        public void RemoveFromRangeCheck(SettingEntry settingEntry)
        {
            if (settingEntry == null)
            {
                throw new ArgumentNullException(nameof(settingEntry));
            }

            this._registeredForRangeUpdates.RemoveAll(p => p.SettingEntry.EntryKey == settingEntry.EntryKey);
            _logger.Debug($"Stopped tracking setting \"{settingEntry.EntryKey}\" for range updates.");
        }

        public void AddForDisabledCheck(SettingEntry settingEntry, IComplianceRequisite defaultRange = null)
        {
            if (settingEntry == null)
            {
                throw new ArgumentNullException(nameof(settingEntry));
            }

            if (!this._registeredForDisabledUpdates.Any(p => p.SettingEntry.EntryKey == settingEntry.EntryKey))
            {
                this._registeredForDisabledUpdates.Add((settingEntry, defaultRange));
                _logger.Debug($"Started tracking setting \"{settingEntry.EntryKey}\" for disabled updates.");
            }
        }

        public void RemoveFromDisabledCheck(SettingEntry settingEntry)
        {
            if (settingEntry == null)
            {
                throw new ArgumentNullException(nameof(settingEntry));
            }

            this._registeredForDisabledUpdates.RemoveAll(p => p.SettingEntry.EntryKey == settingEntry.EntryKey);
            _logger.Debug($"Stopped tracking setting \"{settingEntry.EntryKey}\" for disabled updates.");
        }

        private void CheckRangeUpdates()
        {
            for (int i = 0; i < _registeredForRangeUpdates.Count; i++)
            {
                var settingPair = _registeredForRangeUpdates[i];

                bool changed = false;

                var setting = settingPair.SettingEntry;
                var priorRange = settingPair.ComplianceRequisite;
                var ranges = setting.GetComplianceRequisite();

                if (setting is SettingEntry<int> or SettingEntry<float>)
                {
                    var numberRanges = ranges.Where(r => r is IntRangeRangeComplianceRequisite or FloatRangeRangeComplianceRequisite);
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
                        var numberRange = numberRanges.First();
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

        private void CheckDisabledUpdates()
        {
            for (int i = 0; i < _registeredForDisabledUpdates.Count; i++)
            {
                var settingPair = _registeredForDisabledUpdates[i];

                bool changed = false;

                var setting = settingPair.SettingEntry;
                var priorRange = settingPair.ComplianceRequisite;
                var ranges = setting.GetComplianceRequisite();

                var disabledRanges = ranges.Where(r => r is SettingDisabledComplianceRequisite);
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
                    var disabledRange = disabledRanges.First();
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

    public class ComplianceUpdated
    {
        public SettingEntry SettingEntry { get; }
        public IComplianceRequisite NewCompliance { get; }

        public ComplianceUpdated(SettingEntry settingEntry, IComplianceRequisite newCompliance)
        {
            this.SettingEntry = settingEntry;
            this.NewCompliance = newCompliance;
        }
    }
}
