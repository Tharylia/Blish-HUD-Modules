namespace Estreya.BlishHUD.LookingForGroup;


using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Modules;
using Blish_HUD.Settings;
using Estreya.BlishHUD.LookingForGroup.Controls;
using Flurl.Http;
using Gw2Sharp.Models;
using Humanizer;
using Humanizer.Localisation;
using Microsoft.Xna.Framework;
using Models;
using Newtonsoft.Json;
using Shared.Modules;
using Shared.MumbleInfo.Map;
using Shared.Services;
using Shared.Settings;
using Shared.Threading;
using Shared.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using UI.Views;
using StandardWindow = Shared.Controls.StandardWindow;
using TabbedWindow = Shared.Controls.TabbedWindow;

/// <summary>
/// The event table module class.
/// </summary>
[Export(typeof(Module))]
public class LookingForGroupModule : BaseModule<LookingForGroupModule, ModuleSettings>
{
    private List<CategoryDefinition> _categories;
    private List<Models.LFGEntry> _entries;
    private TabbedWindow _lfgWindow;
    private LookingForGroupView _lfgView;

    private static TimeSpan _entryUpdateInterval = TimeSpan.FromSeconds(30);
    private AsyncRef<double> _lastEntryUpdate = new AsyncRef<double>(0);

    [ImportingConstructor]
    public LookingForGroupModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters)
    {
    }

    public override string UrlModuleName => "looking-for-group";

    protected override string API_VERSION_NO => "1";

    protected override bool FailIfBackendDown => true;

    protected override void Initialize()
    {
        base.Initialize();
        this._categories = new List<CategoryDefinition>();
        this._entries = new List<Models.LFGEntry>();
    }

    protected override async Task LoadAsync()
    {
        Stopwatch sw = Stopwatch.StartNew();

        await base.LoadAsync();

        await this.LoadCategories();
        await this.LoadEntries();

        this._lfgWindow = WindowUtil.CreateTabbedWindow(
            this.ModuleSettings,
            "LFG",
            this.GetType(),
            Guid.Parse("7c8abbcb-26e4-4f33-a77a-d6e9cdd0f3c8"),
            this.IconService, this.IconService.GetIcon("102423.png"));

        _lfgWindow.CanResize = true;
        _lfgWindow.RebuildViewAfterResize = true;
        _lfgWindow.UnloadOnRebuild = false;
        _lfgWindow.SavesPosition = true;
        _lfgWindow.SavesSize = true;
        _lfgWindow.MinSize = _lfgWindow.Size;
        _lfgWindow.MaxSize = new Point(_lfgWindow.Size.X * 2, _lfgWindow.Size.Y * 2);

        this._lfgView = new LookingForGroupView(this.AccountService, () => this._categories, () => this._entries, () => GameService.Gw2Mumble.CurrentMap.Id, this.Gw2ApiManager, this.IconService, this.TranslationService);
        this._lfgView.JoinClicked += this.LfgView_JoinClicked;

        this._lfgWindow.Tabs.Add(new Tab(this.IconService.GetIcon("156680.png"), () => this._lfgView, "Looking for Group"));

        this._lfgWindow.Show();

        sw.Stop();
        this.Logger.Debug($"Loaded in {sw.Elapsed.TotalMilliseconds.ToString(CultureInfo.InvariantCulture)}ms");
    }

    private async Task LfgView_JoinClicked(object sender, Models.LFGEntry e)
    {
        // This can fail

        await this.HandlePlayerJoin(e);
    }

    private async Task HandlePlayerJoin(Models.LFGEntry entry)
    {
        Stopwatch sw = Stopwatch.StartNew();
        if (this.AccountService.Account is null) throw new Exception("Account unavailable.");

        var request = this.GetFlurlClient().Request(this.MODULE_API_URL, "entries", entry.CategoryKey, entry.MapKey, entry.ID, "players");

        var response = await request.PostJsonAsync(new
        {
            accountName = this.AccountService.Account.Name,
        });
        sw.Stop();
        Logger.Debug($"Added player took {sw.Elapsed.TotalMilliseconds} ms");

        this._lastEntryUpdate.Value = _entryUpdateInterval.TotalMilliseconds;
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        _ = UpdateUtil.UpdateAsync(this.LoadEntries, gameTime, _entryUpdateInterval.TotalMilliseconds, this._lastEntryUpdate);
    }

    private async Task LoadCategories()
    {
        var categories = new List<CategoryDefinition>()
        {
            new CategoryDefinition()
            {
                Key = "centralTyria",
                Name = "Central Tyria",
                Description= "Assemble your party or squad and explore the main continent of Tyria.",
                Maps = new []
                {
                    new MapDefinition()
                    {
                        Key = "parties",
                        Name = "Parties",
                    },
                    new MapDefinition()
                    {
                        Key = "squads",
                        Name = "Squads"
                    },
                    new MapDefinition()
                    {
                        Key = "livingWorld",
                        Name = "Living World"
                    },
                    new MapDefinition()
                    {
                        Key = "worldBosses",
                        Name = "World Bosses"
                    }
                }
            },
            new CategoryDefinition()
            {
                Key = "endOfDragons",
                Name = "End of Dragons",
                Description = "Venture to a long-shuttered corner of the world and untangle the mysteries of Cantha, a metropolitan nation of innovation fueled by powerful jade technologies.",
                Maps = new []
                {
                    new MapDefinition()
                    {
                        Key = "seitungProvince",
                        Name = "Seitung Province"
                    },
                    new MapDefinition()
                    {
                        Key = "newKainengCity",
                        Name = "New Kaineng City"
                    },
                    new MapDefinition()
                    {
                        Key = "theEchovaldWilds",
                        Name = "The Echovald Wilds"
                    },
                    new MapDefinition()
                    {
                        Key = "dragonsEnd",
                        Name = "Dragon's End"
                    },
                    new MapDefinition()
                    {
                        Key = "gyalaDelve",
                        Name = "Gyala Delve",
                        MapId = 1490
                    }
                }
            },
            new CategoryDefinition()
            {
                Key = "livingWorldIcebroodSaga",
                Name = "Living World: Icebrood Saga",
                Description = "Explore Tyria's frozen north and immerse yourself in charr and norn culture as the Elder Dragon threat intensifies.",
                Maps = new []
                {
                    new MapDefinition()
                    {
                        Key = "grothmarValley",
                        Name = "Grothmar Valley"
                    },
                    new MapDefinition()
                    {
                        Key = "bjoraMarches",
                        Name = "Bjora Marches"
                    },
                    new MapDefinition()
                    {
                        Key = "steelAndFire",
                        Name = "Steel and Fire"
                    },
                    new MapDefinition()
                    {
                        Key = "drizzlewoodCoast",
                        Name = "Drizzlewood Coast"
                    },
                    new MapDefinition()
                    {
                        Key = "champions",
                        Name = "Champions"
                    }
                }
            },
            new CategoryDefinition()
            {
                Key = "livingWorldSeason4",
                Name = "Living World: Season 4",
                Description = "Return to the plains of Elona and beyond to confront the Crystal Elder Dragon, Kralkatorrik.",
                Maps = new []
                {
                    new MapDefinition()
                    {
                        Key = "domainOfInstan",
                        Name = "Domain of Instan"
                    },
                    new MapDefinition()
                    {
                        Key = "sandsweptIsles",
                        Name = "Sandswept Isles"
                    },
                    new MapDefinition()
                    {
                        Key = "domainOfKourna",
                        Name = "Domain of Kourna"
                    },
                    new MapDefinition()
                    {
                        Key = "jahaiBluffs",
                        Name = "Jahai Bluffs"
                    },
                    new MapDefinition()
                    {
                        Key = "thunderheadPeaks",
                        Name = "Thunderhead Peaks"
                    },
                    new MapDefinition()
                    {
                        Key = "dragonfall",
                        Name = "Dragonfall"
                    }
                }
            }
        };


        categories.ForEach(category => category.Maps.ToList().ForEach(map => map.Load(category)));


        this._categories = categories;
    }

    private async Task LoadEntries()
    {
        Stopwatch sw = Stopwatch.StartNew();
        try
        {
            this._entries?.Clear();

            var request = this.GetFlurlClient().Request(this.MODULE_API_URL, "entries");
            var entries = await request.GetJsonAsync<List<Models.LFGEntry>>();

            this._entries = entries;
        }
        catch (Exception ex)
        {
            this.Logger.Warn(ex, "Failed to load entries.");
        }
        sw.Stop();
        Logger.Debug($"Loading entries took {sw.Elapsed.TotalMilliseconds} ms");

        this._lfgView?.UpdateEntries();
    }


    protected override BaseModuleSettings DefineModuleSettings(SettingCollection settings) => new ModuleSettings(settings);

    protected override void OnSettingWindowBuild(TabbedWindow settingWindow)
    {
        settingWindow.SavesSize = true;
        settingWindow.CanResize = true;
        settingWindow.RebuildViewAfterResize = true;
        settingWindow.UnloadOnRebuild = false;
        settingWindow.MinSize = settingWindow.Size;
        settingWindow.MaxSize = new Point(settingWindow.Width * 2, settingWindow.Height * 3);
        settingWindow.RebuildDelay = 500;
        // Reorder Icon: 605018

    }

    protected override string GetDirectoryName() => null;

    protected override void ConfigureServices(ServiceConfigurations configurations)
    {
        configurations.Account.Enabled = true;
        configurations.Account.AwaitLoading = true;
    }

    protected override void OnBeforeServicesStarted()
    {
        this.AccountService.Updated += this.AccountService_Updated;
    }

    private void AccountService_Updated(object sender, EventArgs e)
    {
        this._lfgView?.UpdateEntries();
    }

    protected override Collection<ManagedService> GetAdditionalServices(string directoryPath)
    {
        return null;
    }

    protected override AsyncTexture2D GetEmblem()
    {
        return this.IconService.GetIcon("156680.png");
    }

    protected override AsyncTexture2D GetCornerIcon()
    {
        return this.IconService.GetIcon("156680.png");
    }

    protected override void Unload()
    {
        this.Logger.Debug("Unload module.");

        this._lfgWindow?.Dispose();
        this._lfgWindow = null;

        this.Logger.Debug("Unload base.");

        base.Unload();

        this.Logger.Debug("Unloaded base.");
    }

    protected override int CornerIconPriority => 1_289_351_272;
}