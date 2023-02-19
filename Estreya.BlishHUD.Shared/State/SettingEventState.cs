namespace Estreya.BlishHUD.Shared.State
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

    public class SettingEventState : ManagedState
    {
        private static readonly Logger _logger = Logger.GetLogger<SettingEventState>();

        private Dictionary<SettingEntry, IComplianceRequisite> _registeredForRangeUpdates;

        private Dictionary<SettingEntry, IComplianceRequisite> _registeredForDisabledUpdates;

        public event EventHandler<ComplianceUpdated> RangeUpdated;
        public event EventHandler<ComplianceUpdated> DisabledUpdated;

        public SettingEventState(StateConfiguration configuration) : base(configuration)
        {
        }

        protected override Task Initialize()
        {
            this._registeredForRangeUpdates = new Dictionary<SettingEntry, IComplianceRequisite>();
            this._registeredForDisabledUpdates = new Dictionary<SettingEntry, IComplianceRequisite>();

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
            if (!this._registeredForRangeUpdates.ContainsKey(settingEntry))
            {
                this._registeredForRangeUpdates.Add(settingEntry, defaultRange);
                _logger.Debug($"Started tracking setting \"{settingEntry.EntryKey}\" for range updates.");
            }
        }
        public void RemoveFromRangeCheck(SettingEntry settingEntry)
        {
            if (this._registeredForRangeUpdates.ContainsKey(settingEntry))
            {
                this._registeredForRangeUpdates.Remove(settingEntry);
                _logger.Debug($"Stopped tracking setting \"{settingEntry.EntryKey}\" for range updates.");
            }
        }
        public void AddForDisabledCheck(SettingEntry settingEntry, IComplianceRequisite defaultRange = null)
        {
            if (!this._registeredForDisabledUpdates.ContainsKey(settingEntry))
            {
                this._registeredForDisabledUpdates.Add(settingEntry, defaultRange);
                _logger.Debug($"Started tracking setting \"{settingEntry.EntryKey}\" for disabled updates.");
            }
        }
        public void RemoveFromDisabledCheck(SettingEntry settingEntry)
        {
            if (this._registeredForDisabledUpdates.ContainsKey(settingEntry))
            {
                this._registeredForDisabledUpdates.Remove(settingEntry);
                _logger.Debug($"Stopped tracking setting \"{settingEntry.EntryKey}\" for disabled updates.");
            }
        }

        private void CheckRangeUpdates()
        {
            for (int i = 0; i < _registeredForRangeUpdates.Count; i++)
            {
                var settingPair = _registeredForRangeUpdates.ElementAt(i);

                bool changed = false;

                var setting = settingPair.Key;
                var priorRange = settingPair.Value;
                var ranges = setting.GetComplianceRequisite();

                if (setting is SettingEntry<int> or SettingEntry<float>)
                {
                    var numberRanges = ranges.Where(r => r is IntRangeRangeComplianceRequisite or FloatRangeRangeComplianceRequisite).ToList();
                    if (numberRanges.Count == 0)
                    {
                        if (priorRange != null)
                        {
                            _registeredForRangeUpdates[setting] = null;
                            changed = true;
                        }
                    }
                    else
                    {
                        var numberRange = numberRanges.First();
                        _registeredForRangeUpdates[setting] = numberRange;
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
                        this.RangeUpdated?.Invoke(this, new ComplianceUpdated(setting, _registeredForRangeUpdates[setting]));
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
                var settingPair = _registeredForDisabledUpdates.ElementAt(i);

                bool changed = false;

                var setting = settingPair.Key;
                var priorRange = settingPair.Value;
                var ranges = setting.GetComplianceRequisite();

                var disabledRanges = ranges.Where(r => r is SettingDisabledComplianceRequisite).ToList();
                if (disabledRanges.Count == 0)
                {
                    if (priorRange != null)
                    {
                        _registeredForDisabledUpdates[setting] = new SettingDisabledComplianceRequisite(false);
                        changed = true;
                    }
                }
                else
                {
                    var disabledRange = disabledRanges.First();
                    _registeredForDisabledUpdates[setting] = disabledRange;
                    if (priorRange != disabledRange)
                    {
                        changed = true;
                    }
                }

                if (changed)
                {
                    try
                    {
                        this.DisabledUpdated?.Invoke(this, new ComplianceUpdated(setting, _registeredForDisabledUpdates[setting]));
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
