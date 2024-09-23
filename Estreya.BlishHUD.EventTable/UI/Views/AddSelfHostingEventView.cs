namespace Estreya.BlishHUD.EventTable.UI.Views;

using Blish_HUD.Controls;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.EventTable.Services;
using Estreya.BlishHUD.Shared.Services;
using Estreya.BlishHUD.Shared.UI.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class AddSelfHostingEventView : BaseView
{
    private readonly SelfHostingEventService _selfHostingEventService;
    private readonly AccountService _accountService;

    private readonly Dictionary<string, List<string>> _categories;
    private readonly TimeSpan _maxHostingDuration;

    public AddSelfHostingEventView(SelfHostingEventService selfHostingEventService, Gw2ApiManager apiManager, IconService iconService, TranslationService translationService, AccountService accountService, Dictionary<string, List<string>> categories, TimeSpan maxHostingDuration) : base(apiManager, iconService, translationService)
    {
        this._selfHostingEventService = selfHostingEventService;
        this._accountService = accountService;
        this._categories = categories;
        this._maxHostingDuration = maxHostingDuration;
    }

    protected override void InternalBuild(Panel parent)
    {
    }

    protected override Task<bool> InternalLoad(IProgress<string> progress)
    {
        return Task.FromResult(true);
    }
}
