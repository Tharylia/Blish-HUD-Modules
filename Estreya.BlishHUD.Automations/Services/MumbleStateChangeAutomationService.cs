namespace Estreya.BlishHUD.Automations.Services;

using Blish_HUD;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Automations.Models.Automations;
using Estreya.BlishHUD.Automations.Models.Automations.MumbleStateChange;
using Estreya.BlishHUD.Automations.Models.Automations.PositionChange;
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

public class MumbleStateChangeAutomationService : AutomationService<MumbleStateChangeAutomationEntry, MumbleStateChangeActionInput>
{
    private static TimeSpan _checkInterval = TimeSpan.FromMilliseconds(500);
    private double _lastCheck = 0;
    private Vector3 _lastPosition = Vector3.Zero;

    public MumbleStateChangeAutomationService(ServiceConfiguration configuration, IFlurlClient flurlClient, Gw2ApiManager apiManager, IHandlebars handlebarsContext) : base(configuration, flurlClient, apiManager, handlebarsContext) { }

    protected override Task Initialize()
    {
        base.Initialize();
        return Task.CompletedTask;
    }

    private void CheckPositionChange()
    {
        var currentPosition = GameService.Gw2Mumble.PlayerCharacter.Position;

        if (currentPosition == this._lastPosition) return;

        var mapChangeEntries = this.GetEntries().Where(entry => true).ToList();

        try
        {
            foreach (var entry in mapChangeEntries)
            {

                //this.EnqueueEntry(entry, new PositionChangeActionInput()
                //{
                //    From = this._lastPosition,
                //    To = currentPosition
                //});
            }
        }
        catch (Exception ex)
        {
            this.Logger.Warn(ex, "Could not enqueue automation entry.");
        }

        this._lastPosition = currentPosition;
    }

    protected override void InternalUpdate(GameTime gameTime)
    {
        base.InternalUpdate(gameTime);

        UpdateUtil.Update(this.CheckPositionChange, gameTime, _checkInterval.TotalMilliseconds, ref _lastCheck);
    }

    protected override void InternalUnload()
    {
        base.InternalUnload();
    }
}
