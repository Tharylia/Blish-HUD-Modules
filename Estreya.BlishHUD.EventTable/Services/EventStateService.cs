﻿namespace Estreya.BlishHUD.EventTable.Services;

using Blish_HUD;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NodaTime;
using Shared.Helpers;
using Shared.Services;
using Shared.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public class EventStateService : ManagedService
{
    public enum EventStates
    {
        Completed,
        Hidden
    }

    private const string DATE_TIME_FORMAT = "yyyy-MM-ddTHH:mm:ss";

    private const string FILE_NAME = "event_states.json";

    private readonly Func<Instant> _getNowAction;

    private string _path;
    private bool dirty;

    public EventStateService(ServiceConfiguration configuration, string basePath, Func<Instant> getNowAction) : base(configuration)
    {
        this._basePath = basePath;
        this._getNowAction = getNowAction;
    }

    private string _basePath { get; }

    private string Path
    {
        get
        {
            this._path ??= System.IO.Path.Combine(this._basePath, FILE_NAME);

            return this._path;
        }
    }

    public List<VisibleStateInfo> Instances { get; } = new List<VisibleStateInfo>();

    public event EventHandler<ValueEventArgs<VisibleStateInfo>> StateAdded;
    public event EventHandler<ValueEventArgs<VisibleStateInfo>> StateRemoved;

    protected override Task InternalReload()
    {
        return Task.CompletedTask;
    }

    protected override void InternalUpdate(GameTime gameTime)
    {
        Instant now = this._getNowAction();
        lock (this.Instances)
        {
            for (int i = this.Instances.Count - 1; i >= 0; i--)
            {
                VisibleStateInfo instance = this.Instances.ElementAt(i);

                bool remove = now >= instance.Until;

                if (remove)
                {
                    this.Remove(instance.AreaName, instance.EventKey);
                }
            }
        }
    }

    public void Add(string areaName, string eventKey, Instant until, EventStates state)
    {
        lock (this.Instances)
        {
            this.Remove(areaName, eventKey);

            string name = this.GetName(areaName, eventKey);

            this.Logger.Info($"Add event state for \"{name}\" with \"{state}\" until \"{until.ToString(DATE_TIME_FORMAT, CultureInfo.InvariantCulture)}\" UTC.");

            VisibleStateInfo newInstance = new VisibleStateInfo
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
                this.Logger.Error(ex, "StateAdded.Invoke failed.");
            }

            this.dirty = true;
        }
    }

    public void Remove(string areaName, EventStates? state)
    {
        lock (this.Instances)
        {
            List<VisibleStateInfo> instancesToRemove = this.Instances.Where(instance => instance.AreaName == areaName && (!state.HasValue || instance.State == state.Value)).ToList();

            instancesToRemove.ForEach(i => this.Remove(areaName, i.EventKey));
        }
    }

    public void Remove(string areaName, string eventKey)
    {
        lock (this.Instances)
        {
            List<VisibleStateInfo> instancesToRemove = this.Instances.Where(instance => instance.AreaName == areaName && instance.EventKey == eventKey).ToList();

            if (instancesToRemove.Count == 0)
            {
                return;
            }

            string name = this.GetName(areaName, eventKey);

            this.Logger.Info($"Remove event states for \"{name}\".");

            for (int i = instancesToRemove.Count - 1; i >= 0; i--)
            {
                VisibleStateInfo instance = instancesToRemove[i];
                _ = this.Instances.Remove(instance);

                try
                {
                    this.StateRemoved?.Invoke(this, new ValueEventArgs<VisibleStateInfo>(instance));
                }
                catch (Exception ex)
                {
                    this.Logger.Error(ex, "StateRemoved.Invoke failed.");
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
            this.Logger.Info("Remove all event states.");

            for (int i = this.Instances.Count - 1; i >= 0; i--)
            {
                this.Remove(this.Instances[i].AreaName, this.Instances[i].EventKey);
            }

            this.dirty = true;
        }

        return Task.CompletedTask;
    }

    public bool Contains(string eventKey)
    {
        lock (this.Instances)
        {
            return this.Instances.Any(instance => instance.EventKey == eventKey);
        }
    }

    public bool Contains(string eventKey, EventStates state)
    {
        lock (this.Instances)
        {
            return this.Instances.Any(instance => instance.EventKey == eventKey && instance.State == state);
        }
    }

    public bool Contains(string areaName, string eventKey)
    {
        lock (this.Instances)
        {
            return this.Instances.Any(instance => instance.AreaName == areaName && instance.EventKey == eventKey);
        }
    }

    public bool Contains(string areaName, string eventKey, EventStates state)
    {
        lock (this.Instances)
        {
            return this.Instances.Any(instance => instance.AreaName == areaName && instance.EventKey == eventKey && instance.State == state);
        }
    }

    protected override Task Initialize()
    {
        return Task.CompletedTask;
    }

    protected override async Task Load()
    {
        this.Logger.Info("Load saved event states from filesystem.");

        if (!File.Exists(this.Path))
        {
            this.Logger.Info("File does not exist.");
            return;
        }

        try
        {
            string json = await FileUtil.ReadStringAsync(this.Path);

            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            List<VisibleStateInfo> instances = JsonConvert.DeserializeObject<List<VisibleStateInfo>>(json);

            foreach (VisibleStateInfo instance in instances)
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
            this.Logger.Error(ex, "Loading \"{0}\" failed.", this.GetType().Name);
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
    }

    public struct VisibleStateInfo
    {
        public string AreaName;
        public string EventKey;

        [JsonConverter(typeof(StringEnumConverter))]
        public EventStates State;

        public Instant Until;
    }
}