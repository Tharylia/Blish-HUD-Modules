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

public class MapChangeAutomationService : AutomationService<MapChangeAutomationEntry, MapChangeActionInput>
{
    private int _lastMapId = -1;

    public MapChangeAutomationService(ServiceConfiguration configuration, IFlurlClient flurlClient, Gw2ApiManager apiManager, IHandlebars handlebarsContext) : base(configuration,flurlClient,apiManager, handlebarsContext) { }

    protected override Task Initialize()
    {
        base.Initialize();
        GameService.Gw2Mumble.CurrentMap.MapChanged += this.Mumble_MapChanged;
        return Task.CompletedTask;
    }

    private void Mumble_MapChanged(object sender, ValueEventArgs<int> e)
    {
        var mapChangeAutomations = this.GetAutomations().Where(a =>
            a.Type == AutomationType.MAP_CHANGE
            && a is MapChangeAutomationEntry mapChangeAutomationEntry
            && (mapChangeAutomationEntry.ToMapId == -1 || mapChangeAutomationEntry.ToMapId == e.Value) 
            && (mapChangeAutomationEntry.FromMapId == -1 || mapChangeAutomationEntry.FromMapId == this._lastMapId) // Last action input to map is now from map
        ).ToList();

        foreach (var automation in mapChangeAutomations)
        {
            this.EnqueueAutomation(automation, new MapChangeActionInput()
            {
                FromMapId = this._lastMapId,
                ToMapId = e.Value
            });
        }

        this._lastMapId = e.Value;
    }

    protected override void InternalUnload()
    {
        GameService.Gw2Mumble.CurrentMap.MapChanged -= this.Mumble_MapChanged;

        base.InternalUnload();
    }

    protected override async Task ProcessAutomation(MapChangeAutomationEntry automation, MapChangeActionInput input)
    {
        await automation.Execute(input, this._flurlClient, this._apiManager);
    }
}
