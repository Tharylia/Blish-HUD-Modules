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

public abstract class AutomationService<TAutomation, TActionInput> : ManagedService where TAutomation: AutomationEntry<TActionInput>
{
    private static TimeSpan _processingInterval = TimeSpan.FromSeconds(0.5);
    private AsyncRef<double> _lastProcessed = new AsyncRef<double>(0);

    private List<TAutomation> _automations;

    private ConcurrentQueue<(TAutomation Automation, TActionInput Input)> _automationsQueue;
    protected readonly IFlurlClient _flurlClient;
    protected readonly Gw2ApiManager _apiManager;
    protected readonly IHandlebars _handlebarsContext;

    public AutomationService(ServiceConfiguration configuration, IFlurlClient flurlClient, Gw2ApiManager apiManager, IHandlebars handlebarsContext) : base(configuration)
    {
        this._automations = new List<TAutomation>();
        this._flurlClient = flurlClient;
        this._apiManager = apiManager;
        this._handlebarsContext = handlebarsContext;
    }

    protected override Task Initialize()
    {
        this._automationsQueue = new ConcurrentQueue<(TAutomation Automation, TActionInput Input)>();

        return Task.CompletedTask;
    }

    protected override Task Clear()
    {
        this._automationsQueue = null;
        return Task.CompletedTask;
    }

    protected override void InternalUnload()
    {
        this._automations?.Clear();
        this._automations = null;
        this._automationsQueue = null;
    }

    protected List<TAutomation> GetAutomations()
    {
        return this._automations;
    }

    protected void EnqueueAutomation(TAutomation automation, TActionInput input)
    {
        this._automationsQueue.Enqueue((automation, input));
    }

    public void AddAutomation(TAutomation entry)
    {
        if (entry == null) throw new ArgumentNullException(nameof(entry));

        if (this._automations.Any(a => a.Name == entry.Name)) throw new ArgumentException($"An automation with the name \"{entry.Name}\" already exists.");

        this._automations.Add(entry);
    }

    public void RemoveAutomation(string name)
    {
        if (!this._automations.Any(a => a.Name == name)) throw new ArgumentException($"An automation with the name \"{name}\" does not exist.");

        _ = this._automations.RemoveAll(a => a.Name == name);
    }

    private async Task ProcessAutomations()
    {
        const int maxEntries = 10;
        int processEntries = 0;
        while (processEntries <= maxEntries && this._automationsQueue.TryDequeue(out var automation))
        {
            try
            {
                await this.ProcessAutomation(automation.Automation, automation.Input);
            }
            catch (Exception ex)
            {
                this.Logger.Warn(ex, $"Failed to execute automation \"{automation.Automation.Name}\".");
            }
        }
    }

    protected abstract Task ProcessAutomation(TAutomation automation, TActionInput input);

    protected override void InternalUpdate(GameTime gameTime)
    {
        _ = UpdateUtil.UpdateAsync(this.ProcessAutomations, gameTime, _processingInterval.TotalMilliseconds, _lastProcessed, false);
    }

    protected override Task Load()
    {
        return Task.CompletedTask;
    }
}
