namespace Estreya.BlishHUD.WebhookUpdater.Models;

using Blish_HUD.Settings;
using Estreya.BlishHUD.Shared.Threading;
using Humanizer.Localisation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class WebhookConfiguration
{
    public string Name { get; set; }

    public SettingEntry<bool> Enabled { get; set; }

    public SettingEntry<UpdateMode> Mode { get; set; }

    public SettingEntry<string> Interval { get; set; }

    public SettingEntry<TimeUnit> IntervalUnit { get; set; }

    public SettingEntry<bool> OnlyOnUrlOrDataChange { get; set; }

    public SettingEntry<string> Url { get; set; }

    public SettingEntry<string> Content { get; set; }

    public SettingEntry<string> ContentType { get; set; }

    public SettingEntry<HTTPMethod> HTTPMethod { get; set; }

    public SettingEntry<bool> CollectProtocols { get; set; }

    public SettingEntry<List<WebhookProtocol>> Protocol { get; set; }

    public WebhookConfiguration(string name)
    {
        this.Name = name;
    }
}
