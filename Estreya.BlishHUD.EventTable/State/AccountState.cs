namespace Estreya.BlishHUD.EventTable.State;

using Blish_HUD.Modules.Managers;
using Gw2Sharp.WebApi.V2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class AccountState : APIState<Account>
{
    public Account Account
    {
        get
        {
            return this.APIObjectList.Any() ? this.APIObjectList.First() : null;
        }
    }

    public AccountState(Gw2ApiManager apiManager) : base(apiManager, new List<TokenPermission>() { TokenPermission.Account })
    {
        this.FetchAction = async (apiManager) =>
        {
            var account = await apiManager.Gw2ApiClient.V2.Account.GetAsync();

            return new List<Account>() { account };
        };
    }

    public override Task DoClear() => Task.CompletedTask;

    protected override void DoUnload() { }

    protected override Task Save() => Task.CompletedTask;
}
