namespace Estreya.BlishHUD.EventTable.State
{
    using Blish_HUD.Modules.Managers;
    using Gw2Sharp.WebApi.V2;
    using Gw2Sharp.WebApi.V2.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class WorldbossState : APIState<string>
    {
        private readonly AccountState _accountState;

        public event EventHandler<string> WorldbossCompleted;
        public event EventHandler<string> WorldbossRemoved;

        public WorldbossState(Gw2ApiManager apiManager, AccountState accountState) :
            base(apiManager,
                new List<TokenPermission> { TokenPermission.Account, TokenPermission.Progression })
        {
            this._accountState = accountState;

            this.FetchAction = async (apiManager) =>
            {
                await this._accountState.WaitAsync();
                DateTime lastModifiedUTC = this._accountState.Account?.LastModified.UtcDateTime ?? DateTime.MinValue;

                DateTime now = EventTableModule.ModuleInstance.DateTimeNow.ToUniversalTime();
                DateTime lastResetUTC = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);

                if (lastModifiedUTC < lastResetUTC)
                {
                    return new List<string>();
                }

                IApiV2ObjectList<string> worldbosses = await apiManager.Gw2ApiClient.V2.Account.WorldBosses.GetAsync();
                return worldbosses.ToList();
            };

            this.APIObjectAdded += this.APIState_APIObjectAdded;
            this.APIObjectRemoved += this.APIState_APIObjectRemoved;
        }

        private void APIState_APIObjectRemoved(object sender, string e)
        {
            this.WorldbossRemoved?.Invoke(this, e);
        }

        private void APIState_APIObjectAdded(object sender, string e)
        {
            this.WorldbossCompleted?.Invoke(this, e);
        }

        public bool IsCompleted(string apiCode)
        {
            return this.APIObjectList.Contains(apiCode);
        }

        protected override Task Save()
        {
            return Task.CompletedTask;
        }

        public override Task DoClear()
        {
            return Task.CompletedTask;
        }

        protected override void DoUnload()
        {
            this.APIObjectAdded -= this.APIState_APIObjectAdded;
            this.APIObjectRemoved -= this.APIState_APIObjectRemoved;
        }
        /*
private static readonly Logger Logger = Logger.GetLogger<WorldbossState>();
private Gw2ApiManager ApiManager { get; set; }
private TimeSpan updateInterval = TimeSpan.FromMinutes(5).Add(TimeSpan.FromMilliseconds(100));
private double timeSinceUpdate = 0;
private List<string> completedWorldbosses = new List<string>();


public WorldbossState(Gw2ApiManager apiManager)
{
   this.ApiManager = apiManager;
}

private void ApiManager_SubtokenUpdated(object sender, ValueEventArgs<IEnumerable<Gw2Sharp.WebApi.V2.Models.TokenPermission>> e)
{
   _ = Task.Run(this.Reload);
}

public bool IsCompleted(string apiCode)
{
   return this.completedWorldbosses.Contains(apiCode);
}

protected override async Task InternalReload()
{
   await this.UpdateCompletedWorldbosses(null);
}

private async Task UpdateCompletedWorldbosses(GameTime gameTime)
{
   Logger.Info($"Check for completed worldbosses.");
   try
   {
       List<string> oldCompletedWorldbosses;
       lock (this.completedWorldbosses)
       {
           oldCompletedWorldbosses = this.completedWorldbosses.ToArray().ToList();
           this.completedWorldbosses.Clear();
       }

       Logger.Debug("Got {0} worldbosses from previous fetch: {1}", oldCompletedWorldbosses.Count, JsonConvert.SerializeObject(oldCompletedWorldbosses));

       if (this.ApiManager.HasPermissions(new[] { Gw2Sharp.WebApi.V2.Models.TokenPermission.Account, Gw2Sharp.WebApi.V2.Models.TokenPermission.Progression }))
       {
           List<string> bosses = (await this.ApiManager.Gw2ApiClient.V2.Account.WorldBosses.GetAsync()).ToList();

           Logger.Debug("API returned bosses: {0}", JsonConvert.SerializeObject(bosses));

           lock (this.completedWorldbosses)
           {
               this.completedWorldbosses.AddRange(bosses);
           }

           // Check if new worldbosses have been completed.
           foreach (string boss in bosses)
           {
               if (!oldCompletedWorldbosses.Contains(boss))
               {
                   Logger.Info($"Completed worldboss: {boss}");
                   try
                   {
                       this.WorldbossCompleted?.Invoke(this, boss);
                   }
                   catch (Exception ex)
                   {
                       Logger.Error(ex, "Error handling complete worldboss event:");
                   }
               }
           }

           // Immediately after login the api still reports some worldbosses as completed because the account record has not been updated yet.
           // After another request to the api they shoudl disappear.

           for (int i = oldCompletedWorldbosses.Count - 1; i >= 0; i--)
           {
               string oldBoss = oldCompletedWorldbosses[i];
               if (!bosses.Contains(oldBoss))
               {
                   Logger.Info($"Worldboss disappeared from the api: {oldBoss}");

                   _ = oldCompletedWorldbosses.Remove(oldBoss);

                   try
                   {
                       this.WorldbossRemoved?.Invoke(this, oldBoss);
                   }
                   catch (Exception ex)
                   {
                       Logger.Error(ex, "Error handling removed worldboss event:");
                   }
               }
           }
       }
       else
       {
           Logger.Info("API Manager does not have enough permissions.");
       }
   }
   catch (MissingScopesException msex)
   {
       Logger.Warn(msex, "Could not update completed worldbosses due to missing scopes:");
   }
   catch (InvalidAccessTokenException iatex)
   {
       Logger.Warn(iatex, "Could not update completed worldbosses due to invalid access token:");
   }
   catch (Exception ex)
   {
       Logger.Warn(ex, "Error updating completed worldbosses:");
   }
}

protected override Task Initialize()
{
   this.ApiManager.SubtokenUpdated += this.ApiManager_SubtokenUpdated;
   return Task.CompletedTask;
}

protected override void InternalUnload()
{
   this.ApiManager.SubtokenUpdated -= this.ApiManager_SubtokenUpdated;

   AsyncHelper.RunSync(this.Clear);
}

protected override void InternalUpdate(GameTime gameTime)
{
   _ = UpdateUtil.UpdateAsync(this.UpdateCompletedWorldbosses, gameTime, this.updateInterval.TotalMilliseconds, ref this.timeSinceUpdate);
}

protected override async Task Load()
{
   await this.UpdateCompletedWorldbosses(null);
}

protected override Task Save()
{
   return Task.CompletedTask;
}

public override Task Clear()
{
   lock (this.completedWorldbosses)
   {
       this.completedWorldbosses.Clear();
   }

   return Task.CompletedTask;
}
*/
    }
}
