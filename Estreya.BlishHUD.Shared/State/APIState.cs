namespace Estreya.BlishHUD.Shared.State;

using Blish_HUD;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.Extensions;
using Estreya.BlishHUD.Shared.Helpers;
using Estreya.BlishHUD.Shared.Threading;
using Estreya.BlishHUD.Shared.Utils;
using Gw2Sharp.WebApi.Exceptions;
using Gw2Sharp.WebApi.V2.Models;
using Humanizer;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public abstract class APIState<T> : ManagedState
{
    private readonly Logger Logger;

    protected readonly Gw2ApiManager _apiManager;
    protected readonly List<TokenPermission> _neededPermissions = new List<TokenPermission>();

    private TimeSpan _updateInterval;
    private AsyncRef<double> _timeSinceUpdate = 0;

    protected Task _fetchTask;

    protected readonly AsyncLock _apiObjectListLock = new AsyncLock();
    public bool Loading { get; private set; }

    protected List<T> APIObjectList { get; } = new List<T>();

    protected event EventHandler<T> APIObjectAdded;
    protected event EventHandler<T> APIObjectRemoved;
    protected event EventHandler Updated;

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
        // Load already called. Don't refresh if no permissions needed anyway.
        if (this._neededPermissions.Count > 0)
        {
            _ = Task.Run(this.Reload);
        }
    }

    public sealed override async Task Clear()
    {
        await this.WaitAsync(false);

        using (this._apiObjectListLock.Lock())
        {
            this.APIObjectList.Clear();
        }

        await this.DoClear();
    }

    protected abstract Task DoClear();

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
            _ = UpdateUtil.UpdateAsync(this.FetchFromAPI, gameTime, this._updateInterval.TotalMilliseconds, this._timeSinceUpdate);
        }
    }

    private Task FetchFromAPI()
    {
        this._fetchTask = Task.Run(async () =>
        {
            this.Logger.Info($"Check for api objects.");

            if (this._apiManager == null)
            {
                Logger.Warn("API Manager is null");
                return;
            }

            try
            {
                List<T> oldAPIObjectList;
                using (await this._apiObjectListLock.LockAsync())
                {
                    oldAPIObjectList = this.APIObjectList.Copy();
                    this.APIObjectList.Clear();

                    Logger.Debug("Got {0} api objects from previous fetch: {1}", oldAPIObjectList.Count, string.Join(", ", oldAPIObjectList));

                    if (!this._apiManager.HasPermissions(this._neededPermissions))
                    {
                        Logger.Warn("API Manager does not have needed permissions: {0}", this._neededPermissions.Humanize());
                        return;
                    }

                    List<T> apiObjects = await this.Fetch(this._apiManager);

                    Logger.Debug("API returned objects: {0}", string.Join(", ", apiObjects));

                    this.APIObjectList.AddRange(apiObjects);

                    // Check if new api objects have been added.
                    foreach (T apiObject in apiObjects)
                    {
                        if (!oldAPIObjectList.Any(oldApiObject => oldApiObject.GetHashCode() == apiObject.GetHashCode()))
                        {
                            Logger.Debug($"API Object added: {apiObject}");
                            try
                            {
                                this.APIObjectAdded?.Invoke(this, apiObject);
                            }
                            catch (Exception ex)
                            {
                                Logger.Error(ex, "Error handling api object added event:");
                            }
                        }
                    }

                    // Immediately after login the api still reports some objects as available because the account record has not been updated yet.
                    // After another request to the api they should disappear.
                    for (int i = oldAPIObjectList.Count - 1; i >= 0; i--)
                    {
                        T oldApiObject = oldAPIObjectList[i];

                        if (!apiObjects.Any(apiObject => apiObject.GetHashCode() == oldApiObject.GetHashCode()))
                        {
                            Logger.Debug($"API Object disappeared from the api: {oldApiObject}");

                            _ = oldAPIObjectList.Remove(oldApiObject);

                            try
                            {
                                this.APIObjectRemoved?.Invoke(this, oldApiObject);
                            }
                            catch (Exception ex)
                            {
                                Logger.Error(ex, "Error handling api object removed event:");
                            }
                        }
                    }

                    this.Updated?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (MissingScopesException msex)
            {
                Logger.Warn(msex, "Could not update api objects due to missing scopes:");
            }
            catch (InvalidAccessTokenException iatex)
            {
                Logger.Warn(iatex, "Could not update api objects due to invalid access token:");
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Error updating api objects:");
            }
        });

        return this._fetchTask;
    }

    protected abstract Task<List<T>> Fetch(Gw2ApiManager apiManager);

    /// <summary>
    /// Waits until the first or current fetch is completed.
    /// </summary>
    /// <returns></returns>
    public async Task WaitAsync(bool waitForFirstFetch = true)
    {
        if (this._fetchTask == null)
        {
            this.Logger.Debug("First fetch did not start yet.");

            if (!waitForFirstFetch)
            {
                this.Logger.Debug("Not waiting for first fetch.");
                return;
            }

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
                    this.Logger.Debug("First fetch did not complete after {0} tries. ({1} minutes)", counter, counter * waitMs / 1000 / 60);
                    return;
                }
            }
        }

        await this._fetchTask;

    }

    protected override async Task Load()
    {
        lock (this)
        {
            this.Loading = true;
        }

        try
        {
            await this.FetchFromAPI();
        }
        finally
        {
            lock (this)
            {
                this.Loading = false;
            }
        }
    }
}
