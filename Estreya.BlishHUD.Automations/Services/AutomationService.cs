namespace Estreya.BlishHUD.Automations.Services;

using Blish_HUD;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Automations.Models.Automations;
using Estreya.BlishHUD.Automations.Models.Automations.MapChange;
using Estreya.BlishHUD.Shared.Services;
using Estreya.BlishHUD.Shared.Threading;
using Estreya.BlishHUD.Shared.Utils;
using Flurl.Http;
using HandlebarsDotNet;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public abstract class AutomationService<TAutomationEntry, TActionInput> : ManagedService where TAutomationEntry : AutomationEntry<TActionInput>
{
    private static TimeSpan _processingInterval = TimeSpan.FromSeconds(0.5);
    private AsyncRef<double> _lastProcessed = new AsyncRef<double>(0);

    private List<TAutomationEntry> _entries;

    private ConcurrentQueue<(TAutomationEntry Automation, TActionInput Input)> _entryQueue;
    protected readonly IFlurlClient _flurlClient;
    protected readonly Gw2ApiManager _apiManager;
    protected readonly IHandlebars _handlebarsContext;

    public AutomationService(ServiceConfiguration configuration, IFlurlClient flurlClient, Gw2ApiManager apiManager, IHandlebars handlebarsContext) : base(configuration)
    {
        this._entries = new List<TAutomationEntry>();
        this._flurlClient = flurlClient;
        this._apiManager = apiManager;
        this._handlebarsContext = handlebarsContext;
    }

    protected override Task Initialize()
    {
        this._entryQueue = new ConcurrentQueue<(TAutomationEntry Automation, TActionInput Input)>();

        return Task.CompletedTask;
    }

    protected override Task Clear()
    {
        this._entryQueue = null;
        return Task.CompletedTask;
    }

    protected override void InternalUnload()
    {
        this._entries?.Clear();
        this._entries = null;
        this._entryQueue = null;
    }

    protected List<TAutomationEntry> GetEntries()
    {
        return this._entries;
    }

    protected void EnqueueEntry(TAutomationEntry automation, TActionInput input)
    {
        this._entryQueue.Enqueue((automation, input));
    }

    public void AddEntry(TAutomationEntry entry)
    {
        if (entry == null) throw new ArgumentNullException(nameof(entry));

        if (this._entries.Any(a => a.Name == entry.Name)) throw new ArgumentException($"An entry with the name \"{entry.Name}\" already exists.");

        this._entries.Add(entry);
    }

    public void RemoveEntry(string name)
    {
        if (!this._entries.Any(a => a.Name == name)) throw new ArgumentException($"An entry with the name \"{name}\" does not exist.");

        _ = this._entries.RemoveAll(a => a.Name == name);
    }

    private async Task ProcessEntries()
    {
        const int maxEntries = 10;
        int processEntries = 0;
        while (processEntries <= maxEntries && this._entryQueue.TryDequeue(out var queueEntry))
        {
            try
            {
                await this.ProcessEntry(queueEntry.Automation, queueEntry.Input);
                this.Logger.Debug(message: $"Executed entry \"{queueEntry.Automation.Name}\".");

                if (queueEntry.Automation.ExecutionCount != -1)
                {
                    queueEntry.Automation.ExecutionCount--;
                    if (queueEntry.Automation.ExecutionCount <= 0)
                    {
                        this.RemoveEntry(queueEntry.Automation.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                this.Logger.Warn(ex, $"Failed to execute entry \"{queueEntry.Automation.Name}\".");
            }
        }
    }

    protected virtual async Task ProcessEntry(TAutomationEntry entry, TActionInput input)
    {
        await entry.Execute(input, this._flurlClient, this._apiManager);
    }

    protected override void InternalUpdate(GameTime gameTime)
    {
        _ = UpdateUtil.UpdateAsync(this.ProcessEntries, gameTime, _processingInterval.TotalMilliseconds, _lastProcessed, false);
    }

    protected override Task Load()
    {
        return Task.CompletedTask;
    }
}
