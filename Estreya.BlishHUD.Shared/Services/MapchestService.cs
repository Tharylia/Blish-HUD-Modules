namespace Estreya.BlishHUD.Shared.Services
{
    using Blish_HUD;
    using Blish_HUD.Modules.Managers;
    using Gw2Sharp.WebApi.Exceptions;
    using Gw2Sharp.WebApi.V2;
    using Gw2Sharp.WebApi.V2.Models;
    using Microsoft.Xna.Framework;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class MapchestService : APIService<string>
    {
        private readonly AccountService _accountService;

        public event EventHandler<string> MapchestCompleted;
        public event EventHandler<string> MapchestRemoved;

        public MapchestService(APIServiceConfiguration configuration, Gw2ApiManager apiManager, AccountService accountService) :
            base(apiManager, configuration)
        {
            this._accountService = accountService;

            this.APIObjectAdded += this.APIService_APIObjectAdded;
            this.APIObjectRemoved += this.APIService_APIObjectRemoved;
        }

        private void APIService_APIObjectRemoved(object sender, string e)
        {
            this.MapchestRemoved?.Invoke(this, e);
        }

        private void APIService_APIObjectAdded(object sender, string e)
        {
            this.MapchestCompleted?.Invoke(this, e);
        }

        public bool IsCompleted(string apiCode)
        {
            return this.APIObjectList.Contains(apiCode);
        }

        protected override void DoUnload()
        {
            this.APIObjectAdded -= this.APIService_APIObjectAdded;
            this.APIObjectRemoved -= this.APIService_APIObjectRemoved;
        }

        protected override async Task<List<string>> Fetch(Gw2ApiManager apiManager, IProgress<string> progress)
        {
            DateTime lastModifiedUTC = this._accountService.Account?.LastModified.UtcDateTime ?? DateTime.MinValue;

            DateTime now = DateTime.UtcNow;
            DateTime lastResetUTC = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);

            if (lastModifiedUTC < lastResetUTC)
            {
                Logger.Warn("Account has not been modified after reset.");
                return new List<string>();
            }

            IApiV2ObjectList<string> mapchests = await apiManager.Gw2ApiClient.V2.Account.MapChests.GetAsync();
            return mapchests.ToList();
        }
    }
}
