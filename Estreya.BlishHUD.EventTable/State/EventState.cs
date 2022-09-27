namespace Estreya.BlishHUD.EventTable.State
{
    using Blish_HUD;
    using Estreya.BlishHUD.Shared.Helpers;
    using Estreya.BlishHUD.Shared.State;
    using Estreya.BlishHUD.Shared.Utils;
    using Microsoft.Xna.Framework;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    public class EventState : ManagedState
    {
        private const string DATE_TIME_FORMAT = "yyyy-MM-ddTHH:mm:ss";

        public event EventHandler<ValueEventArgs<VisibleStateInfo>> StateAdded;
        public event EventHandler<ValueEventArgs<string>> StateRemoved;
        public enum EventStates
        {
            Completed,
            Hidden
        }

        public struct VisibleStateInfo
        {
            public string Key;
            public EventStates State;
            public DateTime Until;
        }

        private static readonly Logger Logger = Logger.GetLogger<EventState>();
        private const string FILE_NAME = "event_states.txt";
        private const string LINE_SPLIT = "<-->";
        private bool dirty;

        private string _basePath { get; set; }

        private string _path;

        private string Path
        {
            get
            {
                if (this._path == null)
                {
                    this._path = System.IO.Path.Combine(this._basePath, FILE_NAME);
                }

                return this._path;
            }
        }

        private readonly Func<DateTime> _getNowAction;

        private List<VisibleStateInfo> Instances { get; set; } = new List<VisibleStateInfo>();

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
                        this.Remove(instance.Key);
                    }
                }
            }
        }

        public void Add(string name, DateTime until, EventStates state)
        {
            lock (this.Instances)
            {
                this.Remove(name);

                until = until.ToUniversalTime();

                Logger.Info($"Add event state for \"{name}\" with \"{state}\" until \"{until.ToString(DATE_TIME_FORMAT)}\" UTC.");

                var newInstance = new VisibleStateInfo()
                {
                    Key = name,
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

        public void Remove(string name)
        {
            lock (this.Instances)
            {
                var instancesToRemove = this.Instances.Where(instance => instance.Key == name).ToList();

                if (instancesToRemove.Count == 0)
                {
                    return;
                }

                Logger.Info($"Remove event states for \"{name}\".");

                for (int i = instancesToRemove.Count - 1; i >= 0; i--)
                {
                    _ = this.Instances.Remove(instancesToRemove[i]);
                }

                try
                {
                    this.StateRemoved?.Invoke(this, new ValueEventArgs<string>(name));
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "StateRemoved.Invoke failed.");
                }

                this.dirty = true;
            }
        }

        public override Task Clear()
        {
            lock (this.Instances)
            {
                Logger.Info($"Remove all event states.");

                for (int i = this.Instances.Count - 1; i >= 0; i--)
                {
                    this.Remove(this.Instances[i].Key);
                }

                this.dirty = true;
            }

            return Task.CompletedTask;
        }

        public bool Contains(string name, EventStates state)
        {
            lock (this.Instances)
            {
                return this.Instances.Any(instance => instance.Key == name && instance.State == state);
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
                string[] lines = await FileUtil.ReadLinesAsync(this.Path);

                if (lines == null || lines.Length == 0)
                {
                    return;
                }

                lock (this.Instances)
                {
                    foreach (string line in lines)
                    {
                        string[] parts = line.Split(new[] { LINE_SPLIT }, StringSplitOptions.None);
                        if (parts.Length == 0)
                        {
                            Logger.Warn("Line is empty.");
                            continue;
                        }

                        string name = parts[0];

                        try
                        {
                            EventStates state = (EventStates)Enum.Parse(typeof(EventStates), parts[1]);
                            DateTime until = DateTime.ParseExact(parts[2], DATE_TIME_FORMAT, CultureInfo.InvariantCulture);
                            until = DateTime.SpecifyKind(until, DateTimeKind.Utc);

                            var newInstance = new VisibleStateInfo()
                            {
                                Key = name,
                                Until = until,
                                State = state
                            };

                            this.Add(name, until, state);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex, "Loading line \"{0}\" failed. Parts: {1}", name, string.Join(", ", parts));
                        }
                    }
                }
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

            Collection<string> lines = new Collection<string>();

            lock (this.Instances)
            {
                foreach (var instance in this.Instances)
                {
                    lines.Add($"{instance.Key}{LINE_SPLIT}{instance.State}{LINE_SPLIT}{instance.Until.ToString(DATE_TIME_FORMAT)}");
                }
            }

            await FileUtil.WriteLinesAsync(this.Path, lines.ToArray());
            this.dirty = false;
        }

        protected override void InternalUnload()
        {
            AsyncHelper.RunSync(this.Save);
            AsyncHelper.RunSync(this.Clear);
        }
    }
}
