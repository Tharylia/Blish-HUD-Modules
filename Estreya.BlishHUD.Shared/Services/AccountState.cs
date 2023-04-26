namespace Estreya.BlishHUD.Shared.State;

using Blish_HUD.Modules.Managers;
using Gw2Sharp.WebApi.V2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class AccountState : APIState<Account>
{
    public Account Account => this.APIObjectList.Any() ? this.APIObjectList.First() : null;

    public AccountState(APIStateConfiguration configuration, Gw2ApiManager apiManager) : base(apiManager, configuration) { }

    protected override void DoUnload() { }

    protected override async Task<List<Account>> Fetch(Gw2ApiManager apiManager, IProgress<string> progress)
    {
        Account account = await apiManager.Gw2ApiClient.V2.Account.GetAsync();

        return new List<Account>() { account };
    }
}
