namespace Estreya.BlishHUD.Automations.Services;

using Blish_HUD;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Automations.Models.Automations.IntervalChange;
using Estreya.BlishHUD.Shared.Services;
using Estreya.BlishHUD.Shared.Threading;
using Estreya.BlishHUD.Shared.Utils;
using Flurl.Http;
using Gw2Sharp.WebApi.V2.Models;
using HandlebarsDotNet;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class IntervalChangeAutomationService : AutomationService<IntervalChangeAutomationEntry, IntervalChangeActionInput>
{
    private static TimeSpan _checkInterval = TimeSpan.FromMilliseconds(500);
    private double _lastCheck = 0;
    private TimeSpan _lastGameTime;

    public IntervalChangeAutomationService(ServiceConfiguration configuration, IFlurlClient flurlClient, Gw2ApiManager apiManager, IHandlebars handlebarsContext) : base(configuration, flurlClient, apiManager, handlebarsContext) { }

    protected override Task Initialize()
    {
        base.Initialize();
        return Task.CompletedTask;
    }

    private void CheckIntervalChange()
    {
        var currentGameTime = GameService.Overlay.CurrentGameTime.TotalGameTime;

        if (currentGameTime == this._lastGameTime) return;

        var mapChangeEntries = this.GetAutomations().Where(entry => true).ToList();

        try
        {
            foreach (var entry in mapChangeEntries)
            {

                this.EnqueueAutomation(entry, new IntervalChangeActionInput()
                {
                    From = this._lastGameTime,
                    To = currentGameTime
                });
            }
        }
        catch (Exception ex)
        {
            this.Logger.Warn(ex, "Could not enqueue automation entry.");
        }

        this._lastGameTime = currentGameTime;
    }

    protected override void InternalUpdate(GameTime gameTime)
    {
        base.InternalUpdate(gameTime);

        UpdateUtil.Update(this.CheckIntervalChange, gameTime, _checkInterval.TotalMilliseconds, ref _lastCheck);
    }

    protected override void InternalUnload()
    {
        base.InternalUnload();
    }
}
