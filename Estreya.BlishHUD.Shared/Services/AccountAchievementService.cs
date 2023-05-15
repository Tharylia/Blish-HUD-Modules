namespace Estreya.BlishHUD.Shared.Services
{
    using Blish_HUD.Modules.Managers;
    using Gw2Sharp.WebApi.V2.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class AccountAchievementService : APIService<AccountAchievement>
    {
        public AccountAchievementService(Gw2ApiManager apiManager, APIServiceConfiguration configuration) : base(apiManager, configuration)
        {
        }

        protected override async Task<List<AccountAchievement>> Fetch(Gw2ApiManager apiManager, IProgress<string> progress, CancellationToken cancellationToken)
        {
            progress.Report("Loading achievements...");
            var achievements = await apiManager.Gw2ApiClient.V2.Account.Achievements.GetAsync(cancellationToken);
            return achievements.ToList();
        }
    }
}
