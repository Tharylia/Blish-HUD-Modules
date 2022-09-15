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
    protected readonly Gw2ApiManager _apiManager;
    protected new APIStateConfiguration Configuration { get; }

    private AsyncRef<double> _timeSinceUpdate = 0;

    protected Task _fetchTask;

    protected readonly AsyncLock _apiObjectListLock = new AsyncLock();
    public bool Loading { get; protected set; }

    protected List<T> APIObjectList { get; } = new List<T>();

    protected event EventHandler<T> APIObjectAdded;
    protected event EventHandler<T> APIObjectRemoved;

    /// <summary>
    /// Fired when the new api state has been fetched.
    /// </summary>
    public event EventHandler Updated;

    public APIState(Gw2ApiManager apiManager, APIStateConfiguration configuration) : base(configuration)
    {
        this._apiManager = apiManager;
        this.Configuration = configuration;
    }

    protected sealed override Task Initialize()
    {
        this._apiManager.SubtokenUpdated += this.ApiManager_SubtokenUpdated;
        return Task.CompletedTask;
    }

    protected virtual Task DoInitialize() => Task.CompletedTask;

    private void ApiManager_SubtokenUpdated(object sender, ValueEventArgs<IEnumerable<Gw2Sharp.WebApi.V2.Models.TokenPermission>> e)
    {
        Logger.Info("Received new subtoken with permissions: {0}", e.Value.Humanize());

        // Load already called. Don't refresh if no permissions needed anyway.
        if (this.Configuration.NeededPermissions.Count > 0)
        {
            AsyncHelper.RunSync(this.Clear);
            this._timeSinceUpdate.Value = this.Configuration.UpdateInterval.TotalMilliseconds;
        }
    }

    protected sealed override async Task Clear()
    {
        //await this.WaitAsync(false);

        using (await this._apiObjectListLock.LockAsync())
        {
            this.APIObjectList.Clear();
        }

        await this.DoClear();
    }

    protected virtual Task DoClear() => Task.CompletedTask;

    protected sealed override void InternalUnload()
    {
        this._apiManager.SubtokenUpdated -= this.ApiManager_SubtokenUpdated;

        this.DoUnload();
    }

    protected virtual void DoUnload() { }

    protected sealed override void InternalUpdate(GameTime gameTime)
    {
        if (this.Configuration.UpdateInterval != Timeout.InfiniteTimeSpan)
        {
            _ = UpdateUtil.UpdateAsync(this.Load, gameTime, this.Configuration.UpdateInterval.TotalMilliseconds, this._timeSinceUpdate);
        }

        this.DoUpdate(gameTime);
    }

    protected virtual void DoUpdate(GameTime gameTime) { }

    private async Task FetchFromAPI()
    {
        //if (this._fetchTask != null && (!this._fetchTask.IsCompleted && !this._fetchTask.IsFaulted))
        //{
        //    return this._fetchTask;
        //}

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

                Logger.Debug("Got {0} api objects from previous fetch.", oldAPIObjectList.Count);

                if (!this._apiManager.HasPermissions(this.Configuration.NeededPermissions))
                {
                    Logger.Warn("API Manager does not have needed permissions: {0}", this.Configuration.NeededPermissions.Humanize());
                    return;
                }

                List<T> apiObjects = await this.Fetch(this._apiManager);

                Logger.Debug("API returned {0} objects.", apiObjects.Count);

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

            this.Logger.Info($"Check for api objects finished.");
        }
        catch (MissingScopesException msex)
        {
            Logger.Warn(msex, "Could not update api objects due to missing scopes:");
            throw;
        }
        catch (InvalidAccessTokenException iatex)
        {
            Logger.Warn(iatex, "Could not update api objects due to invalid access token:");
            throw;
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Error updating api objects:");
            throw;
        }

        //return this._fetchTask;
    }

    protected abstract Task<List<T>> Fetch(Gw2ApiManager apiManager);

    /// <summary>
    /// Waits until the first or current fetch is completed.
    /// </summary>
    /// <returns></returns>
    //public async Task<bool> WaitAsync(bool waitForFirstFetch = true)
    //{
    //    if (this._fetchTask == null)
    //    {
    //        this.Logger.Debug("First fetch did not start yet.");

    //        if (!waitForFirstFetch)
    //        {
    //            this.Logger.Debug("Not waiting for first fetch.");
    //            return true;
    //        }

    //        int waitMs = 100;
    //        int counter = 0;
    //        int maxCounter = 60 * 5; // 5 Minutes
    //        while (this._fetchTask == null)
    //        {
    //            // Wait for first load
    //            await Task.Delay(waitMs);

    //            counter++;

    //            if (counter > maxCounter)
    //            {
    //                this.Logger.Debug("First fetch did not complete after {0} tries. ({1} minutes)", counter, counter * waitMs / 1000 / 60);
    //                return false;
    //            }
    //        }
    //    }

    //    await this._fetchTask;
    //    return true;
    //}

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
