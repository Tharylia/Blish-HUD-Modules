namespace Estreya.BlishHUD.Shared.State;

using Blish_HUD;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.Extensions;
using Estreya.BlishHUD.Shared.Helpers;
using Estreya.BlishHUD.Shared.Threading;
using Estreya.BlishHUD.Shared.Utils;
using Gw2Sharp.WebApi.Exceptions;
using Gw2Sharp.WebApi.V2.Clients;
using Gw2Sharp.WebApi.V2.Models;
using Humanizer;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public abstract class APIState<T> : APIState
{
    protected readonly AsyncLock _apiObjectListLock = new AsyncLock();

    protected List<T> APIObjectList { get; } = new List<T>();

    protected event EventHandler<T> APIObjectAdded;
    protected event EventHandler<T> APIObjectRemoved;

    public APIState(Gw2ApiManager apiManager, APIStateConfiguration configuration) : base(apiManager, configuration)
    {
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

    protected override async Task FetchFromAPI(Gw2ApiManager apiManager, IProgress<string> progress)
    {
        this.Logger.Info($"Check for api objects.");

        if (apiManager == null)
        {
            Logger.Warn("API Manager is null");
            return;
        }

        try
        {
            List<T> oldAPIObjectList;
            using (await this._apiObjectListLock.LockAsync())
            {
                oldAPIObjectList = this.APIObjectList.ToArray().ToList()/*.Copy()*/;
                this.APIObjectList.Clear();

                Logger.Debug("Got {0} api objects from previous fetch.", oldAPIObjectList.Count);

                if (!this._apiManager.HasPermissions(this.Configuration.NeededPermissions))
                {
                    Logger.Warn("API Manager does not have needed permissions: {0}", this.Configuration.NeededPermissions.Humanize());
                    return;
                }

                List<T> apiObjects = await this.Fetch(apiManager, progress).ConfigureAwait(false);

                Logger.Debug("API returned {0} objects.", apiObjects.Count);

                this.APIObjectList.AddRange(apiObjects);

                progress.Report("Check what api objects are new..");
                // Check if new api objects have been added.
                foreach (T apiObject in apiObjects)
                {
                    if (!oldAPIObjectList.Any(oldApiObject => oldApiObject.GetHashCode() == apiObject.GetHashCode()))
                    {
                        if (apiObjects.Count <= 25)
                        {
                            Logger.Debug($"API Object added: {apiObject}");
                        }

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

                progress.Report("Check what api objects are removed..");
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
    }

    protected abstract Task<List<T>> Fetch(Gw2ApiManager apiManager, IProgress<string> progress);
}
