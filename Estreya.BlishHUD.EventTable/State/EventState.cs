namespace Estreya.BlishHUD.EventTable.State
{
    using Blish_HUD;
    using Estreya.BlishHUD.Shared.Helpers;
    using Estreya.BlishHUD.Shared.State;
    using Estreya.BlishHUD.Shared.Utils;
    using Microsoft.Xna.Framework;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.Eventing.Reader;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    public class EventState : ManagedState
    {
        private const string DATE_TIME_FORMAT = "yyyy-MM-ddTHH:mm:ss";

        public event EventHandler<ValueEventArgs<VisibleStateInfo>> StateAdded;
        public event EventHandler<ValueEventArgs<VisibleStateInfo>> StateRemoved;
        public enum EventStates
        {
            Completed,
            Hidden
        }

        public struct VisibleStateInfo
        {
            public string AreaName;
            public string EventKey;
            [JsonConverter(typeof(StringEnumConverter))]
            public EventStates State;
            public DateTime Until;
        }

        private const string FILE_NAME = "event_states.json";
        private bool dirty;

        private string _basePath { get; set; }

        private string _path;

        private string Path
        {
            get
            {
                this._path ??= System.IO.Path.Combine(this._basePath, FILE_NAME);

                return this._path;
            }
        }

        private readonly Func<DateTime> _getNowAction;

        public List<VisibleStateInfo> Instances { get; private set; } = new List<VisibleStateInfo>();

        public EventState(StateConfiguration configuration, string basePath, Func<DateTime> getNowAction) : base(configuration)
        {
            this._basePath = basePath;
            this._getNowAction = getNowAction;
        }

        protected override async Task InternalReload()
        {
            await this.Clear();

            await this.Load();
        }

        protected override void InternalUpdate(GameTime gameTime)
        {
            DateTime now = this._getNowAction().ToUniversalTime();
            lock (this.Instances)
            {
                for (int i = this.Instances.Count - 1; i >= 0; i--)
                {
                    var instance = this.Instances.ElementAt(i);

                    bool remove = now >= instance.Until;

                    if (remove)
                    {
                        this.Remove(instance.AreaName, instance.EventKey);
                    }
                }
            }
        }

        public void Add(string areaName, string eventKey, DateTime until, EventStates state)
        {
            lock (this.Instances)
            {
                this.Remove(areaName, eventKey);

                until = until.ToUniversalTime();

                var name = this.GetName(areaName, eventKey);

                Logger.Info($"Add event state for \"{name}\" with \"{state}\" until \"{until.ToString(DATE_TIME_FORMAT)}\" UTC.");

                var newInstance = new VisibleStateInfo()
                {
                    AreaName = areaName,
                    EventKey = eventKey,
                    State = state,
                    Until = until
                };

                this.Instances.Add(newInstance);

                try
                {
                    this.StateAdded?.Invoke(this, new ValueEventArgs<VisibleStateInfo>(newInstance));
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "StateAdded.Invoke failed.");
                }

                this.dirty = true;
            }
        }

        public void Remove(string areaName, EventStates? state)
        {
            lock (this.Instances)
            {
                var instancesToRemove = this.Instances.Where(instance => instance.AreaName == areaName && (!state.HasValue || instance.State == state.Value)).ToList();

                instancesToRemove.ForEach(i => this.Remove(areaName, i.EventKey));
            }
        }

        public void Remove(string areaName, string eventKey)
        {
            lock (this.Instances)
            {
                var instancesToRemove = this.Instances.Where(instance => instance.AreaName == areaName && instance.EventKey == eventKey).ToList();

                if (instancesToRemove.Count == 0)
                {
                    return;
                }

                var name = this.GetName(areaName, eventKey);

                Logger.Info($"Remove event states for \"{name}\".");

                for (int i = instancesToRemove.Count - 1; i >= 0; i--)
                {
                    var instance = instancesToRemove[i];
                    _ = this.Instances.Remove(instance);

                    try
                    {
                        this.StateRemoved?.Invoke(this, new ValueEventArgs<VisibleStateInfo>(instance));
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "StateRemoved.Invoke failed.");
                    }
                }

                this.dirty = true;
            }
        }

        private string GetName(string areaName, string eventKey)
        {
            return $"{areaName}-{eventKey}";
        }

        protected override Task Clear()
        {
            lock (this.Instances)
            {
                Logger.Info($"Remove all event states.");

                for (int i = this.Instances.Count - 1; i >= 0; i--)
                {
                    this.Remove(this.Instances[i].AreaName, this.Instances[i].EventKey);
                }

                this.dirty = true;
            }

            return Task.CompletedTask;
        }

        public bool Contains(string areaName, string eventKey, EventStates state)
        {
            lock (this.Instances)
            {
                return this.Instances.Any(instance => instance.AreaName == areaName && instance.EventKey == eventKey && instance.State == state);
            }
        }

        protected override Task Initialize() => Task.CompletedTask;

        protected override async Task Load()
        {
            Logger.Info("Load saved event states from filesystem.");

            if (!File.Exists(this.Path))
            {
                Logger.Info("File does not exist.");
                return;
            }

            try
            {
                string json = await FileUtil.ReadStringAsync(this.Path);

                if (string.IsNullOrWhiteSpace(json))
                {
                    return;
                }

                var instances = JsonConvert.DeserializeObject<List<VisibleStateInfo>>(json);

                foreach (var instance in instances)
                {
                    this.Add(instance.AreaName, instance.EventKey, instance.Until, instance.State);
                }

                //lock (this.Instances)
                //{
                    //foreach (string line in lines)
                    //{
                    //    string[] parts = line.Split(new[] { LINE_SPLIT }, StringSplitOptions.None);
                    //    if (parts.Length == 0)
                    //    {
                    //        Logger.Warn("Line is empty.");
                    //        continue;
                    //    }

                    //    string name = parts[0];

                    //    try
                    //    {
                    //        EventStates state = (EventStates)Enum.Parse(typeof(EventStates), parts[1]);
                    //        DateTime until = DateTime.ParseExact(parts[2], DATE_TIME_FORMAT, CultureInfo.InvariantCulture);
                    //        until = DateTime.SpecifyKind(until, DateTimeKind.Utc);

                    //        var newInstance = new VisibleStateInfo()
                    //        {
                    //            Key = name,
                    //            Until = until,
                    //            State = state
                    //        };

                    //        this.Add(name, until, state);
                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        Logger.Error(ex, "Loading line \"{0}\" failed. Parts: {1}", name, string.Join(", ", parts));
                    //    }
                    //}
                //}
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Loading \"{0}\" failed.", this.GetType().Name);
            }
        }

        protected override async Task Save()
        {
            if (!this.dirty)
            {
                return;
            }

            string json = null;

            lock (this.Instances)
            {
                json = JsonConvert.SerializeObject(this.Instances, Formatting.Indented);
                

                //foreach (var instance in this.Instances)
                //{
                //    lines.Add($"{instance.Key}{LINE_SPLIT}{instance.State}{LINE_SPLIT}{instance.Until.ToString(DATE_TIME_FORMAT)}");
                //}
            }

            if (!string.IsNullOrWhiteSpace(json))
            {
                await FileUtil.WriteStringAsync(this.Path, json);
            }

            //await FileUtil.WriteLinesAsync(this.Path, lines.ToArray());
            this.dirty = false;
        }

        protected override void InternalUnload()
        {
            AsyncHelper.RunSync(this.Save);
            AsyncHelper.RunSync(this.Clear);
        }
    }
}
