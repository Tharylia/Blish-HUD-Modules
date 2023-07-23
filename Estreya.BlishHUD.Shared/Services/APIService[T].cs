namespace Estreya.BlishHUD.Shared.Services;

using Blish_HUD.Modules.Managers;
using Gw2Sharp.WebApi.Exceptions;
using Gw2Sharp.WebApi.V2.Models;
using Humanizer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Utils;

public abstract class APIService<T> : APIService
{
    protected readonly AsyncLock _apiObjectListLock = new AsyncLock();

    public APIService(Gw2ApiManager apiManager, APIServiceConfiguration configuration) : base(apiManager, configuration)
    {
    }

    protected List<T> APIObjectList { get; } = new List<T>();

    protected event EventHandler<T> APIObjectAdded;
    protected event EventHandler<T> APIObjectRemoved;

    protected sealed override async Task Clear()
    {
        //await this.WaitAsync(false);

        using (await this._apiObjectListLock.LockAsync())
        {
            this.APIObjectList.Clear();
        }

        await this.DoClear();
    }

    protected virtual Task DoClear()
    {
        return Task.CompletedTask;
    }

    protected override async Task FetchFromAPI(Gw2ApiManager apiManager, IProgress<string> progress)
    {
        this.Logger.Info("Check for api objects.");

        if (apiManager == null)
        {
            this.Logger.Warn("API Manager is null");
            return;
        }

        if (this.Configuration.NeededPermissions.Count > 0 && !apiManager.HasPermission(TokenPermission.Account))
        {
            this.Logger.Debug("No token yet.");
            return;
        }

        try
        {
            List<T> oldAPIObjectList;
            using (await this._apiObjectListLock.LockAsync())
            {
                oldAPIObjectList = this.APIObjectList.ToArray().ToList() /*.Copy()*/;
                this.APIObjectList.Clear();

                this.Logger.Debug("Got {0} api objects from previous fetch.", oldAPIObjectList.Count);

                if (!this._apiManager.HasPermissions(this.Configuration.NeededPermissions))
                {
                    this.Logger.Warn("API Manager does not have needed permissions: {0}", this.Configuration.NeededPermissions.Humanize());
                    return;
                }

                List<T> apiObjects = await this.Fetch(apiManager, progress, this.CancellationToken).ConfigureAwait(false);

                this.Logger.Debug("API returned {0} objects.", apiObjects.Count);

                this.APIObjectList.AddRange(apiObjects);

                progress.Report($"Check what api objects are new.. 0/{apiObjects.Count}");
                // Check if new api objects have been added.
                for (int i = 0; i < apiObjects.Count; i++)
                {
                    progress.Report($"Check what api objects are new.. {i}/{apiObjects.Count}");

                    T apiObject = apiObjects[i];

                    if (!oldAPIObjectList.Any(oldApiObject => oldApiObject.GetHashCode() == apiObject.GetHashCode()))
                    {
                        if (apiObjects.Count <= 25)
                        {
                            this.Logger.Debug($"API Object added: {apiObject}");
                        }

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

                progress.Report($"Check what api objects are removed.. 0/{oldAPIObjectList.Count}");
                // Immediately after login the api still reports some objects as available because the account record has not been updated yet.
                // After another request to the api they should disappear.
                for (int i = oldAPIObjectList.Count - 1; i >= 0; i--)
                {
                    progress.Report($"Check what api objects are removed.. {oldAPIObjectList.Count - i}/{oldAPIObjectList.Count}");
                    T oldApiObject = oldAPIObjectList[i];

                    if (!apiObjects.Any(apiObject => apiObject.GetHashCode() == oldApiObject.GetHashCode()))
                    {
                        if (apiObjects.Count <= 25)
                        {
                            this.Logger.Debug($"API Object disappeared from the api: {oldApiObject}");
                        }

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

            this.Logger.Info("Check for api objects finished.");
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
    }

    protected abstract Task<List<T>> Fetch(Gw2ApiManager apiManager, IProgress<string> progress, CancellationToken cancellationToken);
}