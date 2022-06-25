namespace Estreya.BlishHUD.EventTable.State;

using Blish_HUD;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.EventTable.Extensions;
using Estreya.BlishHUD.EventTable.Helpers;
using Estreya.BlishHUD.EventTable.Utils;
using Gw2Sharp.WebApi.Exceptions;
using Gw2Sharp.WebApi.V2.Models;
using Humanizer;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public abstract class APIState<T> : ManagedState
{
    private readonly Logger Logger;

    private readonly Gw2ApiManager _apiManager;
    private readonly List<TokenPermission> _neededPermissions = new List<TokenPermission>();

    private TimeSpan _updateInterval;
    private double _timeSinceUpdate = 0;

    private Task _fetchTask;

    protected readonly AsyncLock _listLock = new AsyncLock();

    protected List<T> APIObjectList { get; } = new List<T>();
    public Func<Gw2ApiManager, Task<List<T>>> FetchAction { get; init; }

    public event EventHandler<T> APIObjectAdded;
    public event EventHandler<T> APIObjectRemoved;

    public APIState(Gw2ApiManager apiManager, List<TokenPermission> neededPermissions = null, TimeSpan? updateInterval = null, bool awaitLoad = true, int saveInterval = -1) : base(awaitLoad, saveInterval)
    {
        this.Logger = Logger.GetLogger(this.GetType());

        this._apiManager = apiManager;

        if (neededPermissions != null)
        {
            this._neededPermissions.AddRange(neededPermissions);
        }

        this._updateInterval = updateInterval ?? TimeSpan.FromMinutes(5).Add(TimeSpan.FromMilliseconds(100));
    }

    private void ApiManager_SubtokenUpdated(object sender, ValueEventArgs<IEnumerable<Gw2Sharp.WebApi.V2.Models.TokenPermission>> e)
    {
        _ = Task.Run(this.Reload);
    }

    public sealed override Task Clear()
    {
        using (this._listLock.Lock())
        {
            this.APIObjectList.Clear();
        }

        return this.DoClear();
    }

    public abstract Task DoClear();

    protected sealed override async Task InternalReload()
    {
        await this.Clear();
        await this.Load();
    }

    protected sealed override Task Initialize()
    {
        this._apiManager.SubtokenUpdated += this.ApiManager_SubtokenUpdated;
        return Task.CompletedTask;
    }

    protected override void InternalUnload()
    {
        this._apiManager.SubtokenUpdated -= this.ApiManager_SubtokenUpdated;
        AsyncHelper.RunSync(this.Clear);

        this.DoUnload();
    }

    protected abstract void DoUnload();

    protected override void InternalUpdate(GameTime gameTime)
    {
        if (this._updateInterval != Timeout.InfiniteTimeSpan)
        {
            _ = UpdateUtil.UpdateAsync(this.FetchFromAPI, gameTime, this._updateInterval.TotalMilliseconds, ref this._timeSinceUpdate);
        }
    }

    private Task FetchFromAPI(GameTime gameTime)
    {
        this._fetchTask = Task.Run(async () =>
        {
            this.Logger.Info($"Check for api objects.");

            if (this._apiManager == null)
            {
                this.Logger.Warn("API Manager is null");
                return;
            }

            if (this.FetchAction == null)
            {
                this.Logger.Warn("No fetchaction definied.");
                return;
            }

            try
            {
                List<T> oldAPIObjectList;
                using (await this._listLock.LockAsync())
                {
                    oldAPIObjectList = this.APIObjectList.Copy();
                    this.APIObjectList.Clear();
                }

                this.Logger.Debug("Got {0} api objects from previous fetch: {1}", oldAPIObjectList.Count, JsonConvert.SerializeObject(oldAPIObjectList));

                if (!this._apiManager.HasPermissions(this._neededPermissions))
                {
                    this.Logger.Warn("API Manager does not have needed permissions: {0}", this._neededPermissions.Humanize());
                    return;
                }

                List<T> apiObjects = await this.FetchAction.Invoke(this._apiManager);

                this.Logger.Debug("API returned objects: {0}", JsonConvert.SerializeObject(apiObjects));

                using (await this._listLock.LockAsync())
                {
                    this.APIObjectList.AddRange(apiObjects);
                }

                // Check if new api objects have been added.
                foreach (T apiObject in apiObjects)
                {
                    if (!oldAPIObjectList.Contains(apiObject))
                    {
                        this.Logger.Info($"API Object added: {apiObject}");
                        try
                        {
                            this.APIObjectAdded?.Invoke(this, apiObject);
                        }
                        catch (Exception ex)
                        {
                            this.Logger.Error(ex, "Error handling api object added event:");
                        }
                    }
                }

                // Immediately after login the api still reports some objects as available because the account record has not been updated yet.
                // After another request to the api they should disappear.
                for (int i = oldAPIObjectList.Count - 1; i >= 0; i--)
                {
                    T oldApiObject = oldAPIObjectList[i];

                    if (!apiObjects.Contains(oldApiObject))
                    {
                        this.Logger.Info($"API Object disappeared from the api: {oldApiObject}");

                        _ = oldAPIObjectList.Remove(oldApiObject);

                        try
                        {
                            this.APIObjectRemoved?.Invoke(this, oldApiObject);
                        }
                        catch (Exception ex)
                        {
                            this.Logger.Error(ex, "Error handling api object removed event:");
                        }
                    }
                }
            }
            catch (MissingScopesException msex)
            {
                this.Logger.Warn(msex, "Could not update api objects due to missing scopes:");
            }
            catch (InvalidAccessTokenException iatex)
            {
                this.Logger.Warn(iatex, "Could not update api objects due to invalid access token:");
            }
            catch (Exception ex)
            {
                this.Logger.Warn(ex, "Error updating api objects:");
            }
        });

        return this._fetchTask;
    }

    /// <summary>
    /// Waits until the first or current fetch is completed.
    /// </summary>
    /// <returns></returns>
    public async Task WaitAsync()
    {
        if (this._fetchTask == null)
        {
            this.Logger.Debug("{0}: First fetch did not start yet.", this.GetType().Name);

            int waitMs = 100;
            int counter = 0;
            int maxCounter = 60 * 5; // 5 Minutes
            while (this._fetchTask == null)
            {
                // Wait for first load
                await Task.Delay(waitMs);

                counter++;

                if (counter > maxCounter)
                {
                    this.Logger.Debug("{0}: First fetch did not complete after {1} tries. ({2} minutes)", this.GetType().Name, counter, counter * waitMs / 1000 / 60);
                    return;
                }
            }
        }

        await this._fetchTask;

    }

    protected override async Task Load()
    {
        await this.FetchFromAPI(null);
    }
}
