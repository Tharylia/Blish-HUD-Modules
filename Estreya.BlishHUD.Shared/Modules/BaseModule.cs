namespace Estreya.BlishHUD.Shared.Modules;

using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.GameIntegration;
using Blish_HUD.Graphics.UI;
using Blish_HUD.Input;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Estreya.BlishHUD.Shared.Controls;
using Estreya.BlishHUD.Shared.Controls.Input;
using Estreya.BlishHUD.Shared.Net;
using Estreya.BlishHUD.Shared.Services.Audio;
using Estreya.BlishHUD.Shared.Services.GameIntegration;
using Estreya.BlishHUD.Shared.Services.TradingPost;
using Estreya.BlishHUD.Shared.Threading;
using Estreya.BlishHUD.Shared.Threading.Events;
using Exceptions;
using Extensions;
using Flurl.Http;
using Gw2Sharp.Models;
using Helpers;
using Microsoft.Xna.Framework;
using Models;
using MonoGame.Extended.BitmapFonts;
using MumbleInfo.Map;
using Security;
using Services;
using Settings;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UI.Views;
using UI.Views.Settings;
using Utils;
using TabbedWindow = Controls.TabbedWindow;

public abstract class BaseModule<TModule, TSettings> : Module where TSettings : BaseModuleSettings where TModule : class
{
    private static TimeSpan _checkBackendInterval = TimeSpan.FromMinutes(2);
    private AsyncRef<double> _lastBackendCheck = new AsyncRef<double>(0);

    /// <summary>
    ///     The logger instance for implementing module classes.
    /// </summary>
    protected Logger Logger { get; }

    protected const string LIVE_FILE_SERVER_HOSTNAME = "files.estreya.de";
    protected const string DEV_FILE_SERVER_HOSTNAME = "files.estreya.dev";

    /// <summary>
    ///     The file root url for the Estreya file service.
    /// </summary>
    protected string FILE_ROOT_URL => $"https://{(this.ModuleSettings.UseDevelopmentAPI.Value ? DEV_FILE_SERVER_HOSTNAME : LIVE_FILE_SERVER_HOSTNAME)}";

    /// <summary>
    ///     The blish hud sub route from the <see cref="FILE_ROOT_URL" />.
    /// </summary>
    protected string FILE_BLISH_ROOT_URL => $"{this.FILE_ROOT_URL}/blish-hud";

    /// <summary>
    ///     The module sub route from the <see cref="FILE_BLISH_ROOT_URL" />.
    /// </summary>
    protected string MODULE_FILE_URL => $"{this.FILE_BLISH_ROOT_URL}/{this.UrlModuleName}";

    protected const string LIVE_API_HOSTNAME = "api.estreya.de";
    protected const string DEV_API_HOSTNAME = "api.estreya.dev";

    /// <summary>
    ///     The api root url for the Estreya BlishHUD api.
    /// </summary>
    protected string API_ROOT_URL => $"https://{(this.ModuleSettings.UseDevelopmentAPI.Value ? DEV_API_HOSTNAME : LIVE_API_HOSTNAME)}/blish-hud";

    private string API_HEALTH_URL => $"{this.API_ROOT_URL}/health";

    /// <summary>
    ///     The module sub route from the <see cref="API_ROOT_URL" /> including the specified api version from
    ///     <see cref="API_VERSION_NO" />.
    /// </summary>
    protected string MODULE_API_URL => $"{this.API_ROOT_URL}/v{this.API_VERSION_NO}/{this.UrlModuleName}";

    protected const string GITHUB_OWNER = "Tharylia";
    protected const string GITHUB_REPOSITORY = "Blish-HUD-Modules";
    private const string GITHUB_CLIENT_ID = "Iv1.9e4dc29d43243704";

    protected GitHubHelper GithubHelper { get; private set; }

    protected PasswordManager PasswordManager { get; private set; }

    /// <summary>
    ///     Specifies the url friendly name for the module.
    /// </summary>
    protected abstract string UrlModuleName { get; }

    /// <summary>
    ///     Specifies the api version to use with <see cref="API_ROOT_URL" />
    /// </summary>
    protected abstract string API_VERSION_NO { get; }

    protected virtual bool NeedsBackend => false;

    protected virtual bool EnableMetrics => false;

    protected ModuleState ModuleState => this._errorStates.Any(e => !string.IsNullOrWhiteSpace(e.Value)) ? ModuleState.Error : ModuleState.Normal;

    private string ErrorStateText;

    public bool IsPrerelease => !string.IsNullOrWhiteSpace(this.Version?.PreRelease);

    private ModuleSettingsView _defaultSettingView;

    private FlurlClient _flurlClient;

    private readonly ConcurrentDictionary<string, string> _loadingTexts = new ConcurrentDictionary<string, string>();

    private readonly ConcurrentDictionary<ModuleErrorStateGroup, string> _errorStates = new ConcurrentDictionary<ModuleErrorStateGroup, string>();

    /// <summary>
    ///     Gets a <see cref="IFlurlClient" /> with the module informations added.
    /// </summary>
    /// <returns>The prepared <see cref="IFlurlClient" />.</returns>
    protected IFlurlClient GetFlurlClient()
    {
        if (this._flurlClient == null)
        {
            this._flurlClient = new FlurlClient();
            this._flurlClient
                .WithHeader("User-Agent", $"{this.Name} {this.Version}")
                .WithHeader("Accept-Encoding", "gzip, delate")
                .Configure(c =>
                {
                    // AutomaticDecompression seems to be set by the default handler as well, but make sure.
                    c.HttpClientFactory = new FlurlHttpClientFactory();
                });
        }

        return this._flurlClient;
    }

    #region Service Managers

    protected SettingsManager SettingsManager => this.ModuleParameters.SettingsManager;
    protected ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
    protected DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
    protected Gw2ApiManager Gw2ApiManager => this.ModuleParameters.Gw2ApiManager;

    #endregion

#if DEBUG
    protected bool Debug => true;
#else
    protected bool Debug => false;
#endif

    protected bool ShowUI { get; private set; } = true;

    protected TSettings ModuleSettings { get; private set; }

    protected CornerIcon CornerIcon { get; set; }

    private LoadingSpinner _loadingSpinner;

    protected MessageContainer MessageContainer { get; private set; }

    protected TabbedWindow SettingsWindow { get; private set; }

    protected virtual BitmapFont Font => GameService.Content.DefaultFont16;

    #region Services

    private readonly AsyncLock _servicesLock = new AsyncLock();
    private readonly SynchronizedCollection<ManagedService> _services = new SynchronizedCollection<ManagedService>();

    protected IconService IconService { get; private set; }
    protected TranslationService TranslationService { get; private set; }
    protected SettingEventService SettingEventService { get; private set; }
    protected NewsService NewsService { get; private set; }
    protected WorldbossService WorldbossService { get; private set; }
    protected MapchestService MapchestService { get; private set; }
    protected PointOfInterestService PointOfInterestService { get; private set; }
    protected AccountService AccountService { get; private set; }
    protected SkillService SkillService { get; private set; }
    protected PlayerTransactionsService PlayerTransactionsService { get; private set; }
    protected TransactionsService TransactionsService { get; private set; }
    protected ItemService ItemService { get; private set; }
    protected ArcDPSService ArcDPSService { get; private set; }
    protected BlishHudApiService BlishHUDAPIService { get; private set; }

    protected AchievementService AchievementService { get; private set; }
    protected AccountAchievementService AccountAchievementService { get; private set; }

    protected MetricsService MetricsService { get; private set; }

    protected AudioService AudioService { get; private set; }

    protected ChatService ChatService { get; private set; }

    #endregion

    private CancellationTokenSource _cancellationTokenSource;

    protected CancellationToken CancellationToken => this._cancellationTokenSource.Token;

    protected event AsyncEventHandler BackendConnectionRestored;

    protected event AsyncEventHandler BackendConnectionLost;

    /// <summary>
    ///     Creates a new instance of the module class.
    /// </summary>
    /// <param name="moduleParameters">The default module parameters passed from blish hud core.</param>
    protected BaseModule(ModuleParameters moduleParameters) : base(moduleParameters)
    {
        this.Logger = Logger.GetLogger(this.GetType());
    }

    protected sealed override void DefineSettings(SettingCollection settings)
    {
        this.ModuleSettings = this.DefineModuleSettings(settings) as TSettings;
    }

    /// <summary>
    ///     Defines the module settings used in the module.
    /// </summary>
    /// <param name="settings">The default module settings.</param>
    /// <returns>The created settings for the module.</returns>
    protected abstract BaseModuleSettings DefineModuleSettings(SettingCollection settings);

    /// <summary>
    ///     Initializes all variables for a fresh start.
    /// </summary>
    protected override void Initialize()
    {
        this._cancellationTokenSource = new CancellationTokenSource();

        this.TEMP_FIX_SetTacOAsActive();

        string directoryName = this.GetDirectoryName();

        if (!string.IsNullOrWhiteSpace(directoryName))
        {
            string directoryPath = this.DirectoriesManager.GetFullDirectoryPath(directoryName);
            this.PasswordManager = new PasswordManager(directoryPath);
            this.PasswordManager.InitializeEntropy(Encoding.UTF8.GetBytes(this.Namespace));
        }
    }

    private void TEMP_FIX_SetTacOAsActive()
    {
        // SOTO Fix
        if (DateTime.UtcNow.Date >= new DateTime(2023, 8, 22, 0, 0, 0, DateTimeKind.Utc) && Program.OverlayVersion < new SemVer.Version(1, 1, 0))
        {
            try
            {
                var tacoActive = typeof(TacOIntegration).GetProperty(nameof(TacOIntegration.TacOIsRunning)).GetSetMethod(true);
                tacoActive?.Invoke(GameService.GameIntegration.TacO, new object[] { true });
            }
            catch { /* NOOP */ }
        }
    }

    /// <summary>
    ///     Loads all default services and resources. 
    /// </summary>
    protected override async Task LoadAsync()
    {
        await Task.Factory.StartNew(this.InitializeEssentialServices, TaskCreationOptions.LongRunning).Unwrap();

        if (this.ModuleSettings.UseDevelopmentAPI.Value)
        {
            this.Logger.Info($"User configured module to use development api: {this.MODULE_API_URL}");
            await this.MessageContainer.Add(this, MessageContainer.MessageType.Warning, "Using Development API");
        }

        await this.CheckBackendHealth();
        try
        {
            await this.VerifyModuleState();
        }
        catch (Exception ex)
        {
            this.Logger.Warn(ex, "Failed to verify module. Disabling module.");
            this.DisableSelf();
            await Task.Delay(1000); // Wait a tiny bit
            return;
        }

        await Task.Factory.StartNew(this.InitializeServices, TaskCreationOptions.LongRunning).Unwrap();

        if (this.EnableMetrics)
        {
            await this.MetricsService.AskMetricsConsent();
        }

        this.GithubHelper = new GitHubHelper(GITHUB_OWNER, GITHUB_REPOSITORY, GITHUB_CLIENT_ID, this.Name, this.PasswordManager, this.IconService, this.TranslationService, this.ModuleSettings);

        this.ModuleSettings.UpdateLocalization(this.TranslationService);

        this.ModuleSettings.RegisterCornerIcon.SettingChanged += this.RegisterCornerIcon_SettingChanged;

        this.BackendConnectionRestored += this.BaseModule_BackendConnectionRestored;
    }

    private async Task BaseModule_BackendConnectionRestored(object sender)
    {
        try
        {
            await this.BlishHUDAPIService.Reload();
        }
        catch (Exception) { }
    }

    /// <summary>
    ///     Checks if the current module satisfies all api backend criteria.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task VerifyModuleState()
    {
        if (this.HasErrorState(ModuleErrorStateGroup.BACKEND_UNAVAILABLE)) return;

        var isBackendUnavailable = (HttpResponseMessage response) => response is null || response.StatusCode is HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable;

        IFlurlRequest request = this.GetFlurlClient().Request(this.MODULE_API_URL, "validate").AllowAnyHttpStatus();

        ModuleValidationRequest data = new ModuleValidationRequest { Version = this.Version };

        HttpResponseMessage response = null;
        try
        {
            response = await request.PostJsonAsync(data);
        }
        catch (Exception ex)
        {
            this.Logger.Debug(ex, "Failed to validate module.");
        }

        if (response is not null && (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound))
        {
            // Everything is working correctly
            return;
        }

        if (isBackendUnavailable(response)) return;

        // Any unexpected status codes
        if (response.StatusCode != HttpStatusCode.Forbidden)
        {
            var content = await response.Content.ReadAsStringAsync();
            await this.MessageContainer.Add(this, MessageContainer.MessageType.Error, $"The module \"{this.Name}\" could not verify itself. Please check the latest log for more information.");

            this.Logger.Error($"Module validation failed with unexpected status code {response.StatusCode}: {content}");
            this.ReportErrorState(ModuleErrorStateGroup.MODULE_VALIDATION, $"Module validation failed. Check latest log for more information.");
            return;
        }

        this.ReportErrorState(ModuleErrorStateGroup.MODULE_VALIDATION, null);

        ModuleValidationResponse validationResponse;
        try
        {
            validationResponse = await response.GetJsonAsync<ModuleValidationResponse>();
        }
        catch (Exception)
        {
            var content = await response.Content.ReadAsStringAsync();
            await this.MessageContainer.Add(this, MessageContainer.MessageType.Error, $"The module \"{this.Name}\" could not verify itself. Please check the latest log for more information.");

            throw new ModuleInvalidException($"Could not read module validation response: {content}");
        }

        List<string> messages = new List<string>
        {
            $"[{this.Name}]",
            "The current module version is invalid!"
        };

        if (!string.IsNullOrWhiteSpace(validationResponse.Message) || !string.IsNullOrWhiteSpace(response.ReasonPhrase))
        {
            messages.Add(validationResponse.Message ?? response.ReasonPhrase);
        }

        await this.MessageContainer.Add(this, MessageContainer.MessageType.Error, $"\n{string.Join("\n", messages)}");

        throw new ModuleInvalidException(validationResponse.Message);
    }

    protected void DisableSelf()
    {
        GameService.Module.Modules.ToList().Find(m => m.ModuleInstance == this).Disable();
    }

    private async Task CheckBackendHealth()
    {
        if (!this.NeedsBackend) return;

        IFlurlRequest request = this.GetFlurlClient().Request(this.API_HEALTH_URL);

        HttpResponseMessage response = null;
        Stopwatch sw = Stopwatch.StartNew();
        try
        {
            response = await request.GetAsync();
        }
        catch (Exception ex)
        {
            this.Logger.Warn(ex, "Failed to validate backend health.");
        }

        sw.Stop();
        this.Logger.Debug($"Checked API backend at \"{request.Url}\". Response: {(response is null ? 999 : (int)response.StatusCode)} - {response?.ReasonPhrase ?? string.Empty} | Duration: {sw.Elapsed.TotalMilliseconds}ms");

        var backendOnline = response is not null && response.IsSuccessStatusCode;

        var wasUnavailable = this.HasErrorState(ModuleErrorStateGroup.BACKEND_UNAVAILABLE);

        if (!wasUnavailable && !backendOnline)
        {
            this.ReportErrorState(ModuleErrorStateGroup.BACKEND_UNAVAILABLE, "Backend unavailable.");

            await this.MessageContainer.Add(this, MessageContainer.MessageType.Error, $"The backend for \"{this.Name}\" is unavailable. Check Estreya BlishHUD Discord for news.");

            await (this.BackendConnectionLost?.Invoke(this) ?? Task.CompletedTask);
        }
        else if (wasUnavailable && backendOnline)
        {
            this.ReportErrorState(ModuleErrorStateGroup.BACKEND_UNAVAILABLE, null);

            try
            {
                await this.VerifyModuleState();
            }
            catch (Exception ex)
            {
                this.Logger.Warn(ex, "Failed to verify module. Disabling module.");
                this.DisableSelf();
                return;
            }

            await this.MessageContainer.Add(this, MessageContainer.MessageType.Info, $"The backend for \"{this.Name}\" is back online.");

            await (this.BackendConnectionRestored?.Invoke(this) ?? Task.CompletedTask);
        }
    }

    /// <summary>
    ///     Handles the changed event for the setting <see cref="BaseModuleSettings.RegisterCornerIcon"/>.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The value changed event args.</param>
    private void RegisterCornerIcon_SettingChanged(object sender, ValueChangedEventArgs<bool> e)
    {
        this.HandleCornerIcon(this.ModuleSettings.RegisterCornerIcon.Value);
    }

    protected abstract string GetDirectoryName();

    protected virtual Task OnAfterEssentialsServicesInitialized()
    {
        this.MessageContainer = new MessageContainer(this.Gw2ApiManager, this.ModuleSettings, this.TranslationService, this.IconService);

        return Task.CompletedTask;
    }

    private async Task InitializeEssentialServices()
    {
        using (await this._servicesLock.LockAsync())
        {
            ServiceConfigurations configurations = new ServiceConfigurations();
            this.ConfigureServices(configurations);

            this.IconService = new IconService(new APIServiceConfiguration
            {
                Enabled = true,
                AwaitLoading = false
            }, this.ContentsManager);
            this._services.Add(this.IconService);

            this.TranslationService = new TranslationService(new ServiceConfiguration
            {
                Enabled = true,
                AwaitLoading = true
            }, this.GetFlurlClient(), this.MODULE_FILE_URL);
            this._services.Add(this.TranslationService);

            this.SettingEventService = new SettingEventService(new ServiceConfiguration
            {
                Enabled = true,
                AwaitLoading = false,
                SaveInterval = Timeout.InfiniteTimeSpan
            });
            this._services.Add(this.SettingEventService);

            this.NewsService = new NewsService(new ServiceConfiguration
            {
                AwaitLoading = true,
                Enabled = true
            }, this.GetFlurlClient(), this.MODULE_FILE_URL);
            this._services.Add(this.NewsService);

            this.MetricsService = new MetricsService(new ServiceConfiguration
            {
                Enabled = true,
                AwaitLoading = true
            }, this.GetFlurlClient(), this.API_ROOT_URL, this.Name, this.Namespace, this.ModuleSettings, this.IconService);
            this._services.Add(this.MetricsService);

            this.ChatService = new ChatService(new ServiceConfiguration
            {
                Enabled = true,
                AwaitLoading = true,
            });
            this._services.Add(this.ChatService);

            await this.OnAfterEssentialsServicesInitialized();
        }

        await this.StartServices();
    }

    /// <summary>
    ///     Initializes all services and starts them.
    /// </summary>
    /// <exception cref="ArgumentNullException">Gets thrown if the directory path could not be loaded for depending services.</exception>
    private async Task InitializeServices()
    {
        this.Logger.Debug("Initialize services");
        string directoryName = this.GetDirectoryName();

        string directoryPath = null;
        if (!string.IsNullOrWhiteSpace(directoryName))
        {
            directoryPath = this.DirectoriesManager.GetFullDirectoryPath(directoryName);
        }

        using (await this._servicesLock.LockAsync())
        {
            ServiceConfigurations configurations = new ServiceConfigurations();
            this.ConfigureServices(configurations);

            if (configurations.BlishHUDAPI.Enabled)
            {
                if (this.PasswordManager == null)
                {
                    throw new ArgumentNullException(nameof(this.PasswordManager));
                }

                this.BlishHUDAPIService = new BlishHudApiService(configurations.BlishHUDAPI, this.ModuleSettings.BlishAPIUsername, this.PasswordManager, this.GetFlurlClient(), this.API_ROOT_URL);
                this._services.Add(this.BlishHUDAPIService);
            }

            if (configurations.Account.Enabled)
            {
                this.AccountService = new AccountService(configurations.Account, this.Gw2ApiManager);
                this._services.Add(this.AccountService);
            }

            if (configurations.Audio.Enabled)
            {
                this.AudioService = new AudioService(configurations.Audio, directoryPath);
                this._services.Add(this.AudioService);
            }

            if (configurations.Items.Enabled)
            {
                this.ItemService = new ItemService(configurations.Items, this.Gw2ApiManager, directoryPath, this.GetFlurlClient(), this.FILE_ROOT_URL);
                this._services.Add(this.ItemService);
            }

            if (configurations.PlayerTransactions.Enabled)
            {
                this.PlayerTransactionsService = new PlayerTransactionsService(configurations.PlayerTransactions, this.ItemService, this.Gw2ApiManager);
                this._services.Add(this.PlayerTransactionsService);
            }

            if (configurations.Transactions.Enabled)
            {
                this.TransactionsService = new TransactionsService(configurations.Transactions, this.ItemService, this.Gw2ApiManager);
                this._services.Add(this.TransactionsService);
            }

            if (configurations.Worldbosses.Enabled)
            {
                if (configurations.Account.Enabled)
                {
                    this.WorldbossService = new WorldbossService(configurations.Worldbosses, this.Gw2ApiManager, this.AccountService);
                    this._services.Add(this.WorldbossService);
                }
                else
                {
                    this.Logger.Debug($"{typeof(WorldbossService).Name} is not available because {typeof(AccountService).Name} is deactivated.");
                    configurations.Worldbosses.Enabled = false;
                }
            }

            if (configurations.Mapchests.Enabled)
            {
                if (configurations.Account.Enabled)
                {
                    this.MapchestService = new MapchestService(configurations.Mapchests, this.Gw2ApiManager, this.AccountService);
                    this._services.Add(this.MapchestService);
                }
                else
                {
                    this.Logger.Debug($"{typeof(MapchestService).Name} is not available because {typeof(AccountService).Name} is deactivated.");
                    configurations.Mapchests.Enabled = false;
                }
            }

            if (configurations.PointOfInterests.Enabled)
            {
                if (string.IsNullOrWhiteSpace(directoryPath))
                {
                    throw new ArgumentNullException(nameof(directoryPath), "Module directory is not specified.");
                }

                this.PointOfInterestService = new PointOfInterestService(configurations.PointOfInterests, this.Gw2ApiManager, directoryPath, this.GetFlurlClient(), this.FILE_ROOT_URL);
                this._services.Add(this.PointOfInterestService);
            }

            if (configurations.Skills.Enabled)
            {
                if (string.IsNullOrWhiteSpace(directoryPath))
                {
                    throw new ArgumentNullException(nameof(directoryPath), "Module directory is not specified.");
                }

                this.SkillService = new SkillService(configurations.Skills, this.Gw2ApiManager, this.IconService, directoryPath, this.GetFlurlClient(), this.FILE_ROOT_URL);
                this._services.Add(this.SkillService);
            }

            if (configurations.ArcDPS.Enabled)
            {
                if (configurations.Skills.Enabled)
                {
                    this.ArcDPSService = new ArcDPSService(configurations.ArcDPS, this.SkillService);
                    this._services.Add(this.ArcDPSService);
                }
                else
                {
                    this.Logger.Debug($"{typeof(ArcDPSService).Name} is not available because {typeof(SkillService).Name} is deactivated.");
                    configurations.ArcDPS.Enabled = false;
                }
            }

            if (configurations.Achievements.Enabled)
            {
                this.AchievementService = new AchievementService(this.Gw2ApiManager, configurations.Achievements, directoryPath, this.GetFlurlClient(), this.FILE_ROOT_URL);
                this._services.Add(this.AchievementService);
            }

            if (configurations.AccountAchievements.Enabled)
            {
                this.AccountAchievementService = new AccountAchievementService(this.Gw2ApiManager, configurations.AccountAchievements);
                this._services.Add(this.AccountAchievementService);
            }

            Collection<ManagedService> customServices = this.GetAdditionalServices(directoryPath);

            if (customServices != null && customServices.Count > 0)
            {
                foreach (ManagedService customService in customServices)
                {
                    this._services.Add(customService);
                }
            }

            this.OnBeforeServicesStarted();
        }

        await this.StartServices();
    }

    private async Task StartServices()
    {
        using (await this._servicesLock.LockAsync())
        {
            // Only start states not already running
            foreach (ManagedService state in this._services.Where(state => !state.Running))
            {
                // Order is important
                if (state.AwaitLoading)
                {
                    try
                    {
                        await state.Start();
                    }
                    catch (Exception ex)
                    {
                        this.Logger.Error(ex, "Failed starting state \"{0}\"", state.GetType().Name);
                    }
                }
                else
                {
                    _ = Task.Run(state.Start).ContinueWith(task =>
                    {
                        if (task.IsFaulted)
                        {
                            this.Logger.Error(task.Exception, "Not awaited state start failed for \"{0}\"", state.GetType().Name);
                        }
                    }).ConfigureAwait(false);
                }
            }
        }
    }

    /// <summary>
    ///     Used to configure the nessecary services for the module.
    /// </summary>
    /// <param name="configurations">The base configuration which can be adapted for the module.</param>
    protected virtual void ConfigureServices(ServiceConfigurations configurations) { }

    /// <summary>
    ///     Gets called before all services are started.
    ///     <para/>
    ///     Can be used to attach event handlers.
    /// </summary>
    protected virtual void OnBeforeServicesStarted() { }

    /// <summary>
    ///     Gets additional services which should be started.
    /// </summary>
    /// <param name="directoryPath">The directory root path for the module.</param>
    /// <returns>A collection of additional services.</returns>
    protected virtual Collection<ManagedService> GetAdditionalServices(string directoryPath) => null;

    private void HandleCornerIcon(bool show)
    {
        if (show)
        {
            if (this.CornerIcon == null)
            {
                this.CornerIcon = new CornerIcon
                {
                    IconName = this.Name,
                    Priority = this.CornerIconPriority
                };

                this.UpdateCornerIcon();

                this.OnCornerIconBuild();
            }
        }
        else
        {
            if (this.CornerIcon != null)
            {
                this.OnCornerIconDispose();
                this.CornerIcon.Dispose();
                this.CornerIcon = null;
            }
        }
    }

    private void UpdateCornerIcon()
    {
        if (this.CornerIcon is null) return;

        this.CornerIcon.Icon = this.ModuleState is ModuleState.Error ? this.GetErrorCornerIcon() : this.GetCornerIcon();

        this.CornerIcon.BasicTooltipText = this.ErrorStateText;
    }

    /// <summary>
    ///     Gets the corner icon priority.
    /// </summary>
    /// <returns></returns>
    protected abstract int CornerIconPriority { get; }

    protected virtual void OnCornerIconBuild()
    {
        this.CornerIcon.Click += this.CornerIcon_Click;
        this.CornerIcon.RightMouseButtonPressed += this.CornerIcon_RightMouseButtonPressed;
    }

    protected virtual void OnCornerIconDispose()
    {
        this.CornerIcon.Click -= this.CornerIcon_Click;
        this.CornerIcon.RightMouseButtonPressed -= this.CornerIcon_RightMouseButtonPressed;
    }

    private void CornerIcon_Click(object sender, MouseEventArgs e)
    {
        switch (this.ModuleSettings.CornerIconLeftClickAction.Value)
        {
            case CornerIconClickAction.Settings:
                this.SettingsWindow.ToggleWindow();
                break;
            case CornerIconClickAction.Visibility:
                this.ModuleSettings.GlobalDrawerVisible.Value = !this.ModuleSettings.GlobalDrawerVisible.Value;
                break;
        }
    }

    private void CornerIcon_RightMouseButtonPressed(object sender, MouseEventArgs e)
    {
        switch (this.ModuleSettings.CornerIconRightClickAction.Value)
        {
            case CornerIconClickAction.Settings:
                this.SettingsWindow.ToggleWindow();
                break;
            case CornerIconClickAction.Visibility:
                this.ModuleSettings.GlobalDrawerVisible.Value = !this.ModuleSettings.GlobalDrawerVisible.Value;
                break;
        }
    }

    public override IView GetSettingsView()
    {
        if (this._defaultSettingView == null)
        {
            this._defaultSettingView = new ModuleSettingsView(this.IconService, this.TranslationService);
            this._defaultSettingView.OpenClicked += this.DefaultSettingView_OpenClicked;
            this._defaultSettingView.CreateGithubIssueClicked += this.DefaultSettingView_CreateGithubIssueClicked;
            this._defaultSettingView.OpenMessageLogClicked += this.DefaultSettingView_OpenMessageLogClicked;
        }

        return this._defaultSettingView;
    }

    private void DefaultSettingView_OpenMessageLogClicked(object sender, EventArgs e)
    {
        this.MessageContainer?.Show();
    }

    private void DefaultSettingView_CreateGithubIssueClicked(object sender, EventArgs e)
    {
        this.GithubHelper.OpenIssueWindow();
    }

    private void DefaultSettingView_OpenClicked(object sender, EventArgs e)
    {
        this.SettingsWindow.ToggleWindow();
    }

    protected override void OnModuleLoaded(EventArgs e)
    {
        // Base handler must be called
        base.OnModuleLoaded(e);

        this.Logger.Debug("Start building settings window.");

        this.SettingsWindow ??= WindowUtil.CreateTabbedWindow(this.ModuleSettings, this.Name, this.GetType(), Guid.Parse("6bd04be4-dc19-4914-a2c3-8160ce76818b"), this.IconService, this.GetEmblem());

        this.SettingsWindow.Tabs.Add(new Tab(this.IconService.GetIcon("482926.png"), () => new NewsView(this.GetFlurlClient(), this.Gw2ApiManager, this.IconService, this.TranslationService, this.NewsService) { DefaultColor = this.ModuleSettings.DefaultGW2Color }, "News"));

        this.OnSettingWindowBuild(this.SettingsWindow);

        this.SettingsWindow.Tabs.Add(new Tab(this.IconService.GetIcon("156331.png"), () => new DonationView(this.GetFlurlClient(), this.Gw2ApiManager, this.IconService, this.TranslationService) { DefaultColor = this.ModuleSettings.DefaultGW2Color }, "Donations"));

        if (this.Debug)
        {
            this.SettingsWindow.Tabs.Add(
                new Tab(
                    this.IconService.GetIcon("155052.png"),
                    () => new ServiceSettingsView(this._services, this.Gw2ApiManager, this.IconService, this.TranslationService, this.SettingEventService) { DefaultColor = this.ModuleSettings.DefaultGW2Color },
                    "Debug"));
        }

        this.Logger.Debug("Finished building settings window.");

        this.HandleCornerIcon(this.ModuleSettings.RegisterCornerIcon.Value);

        _ = this.MetricsService.SendMetricAsync("loaded");
    }

    /// <summary>
    ///     Gets the emblem for the settings window.
    /// </summary>
    /// <returns>The emblem as <see cref="AsyncTexture2D" />.</returns>
    protected abstract AsyncTexture2D GetEmblem();

    /// <summary>
    ///     Gets the icon used for corner icons.
    /// </summary>
    /// <returns>The corner icon as <see cref="AsyncTexture2D" />.</returns>
    protected abstract AsyncTexture2D GetCornerIcon();

    protected virtual AsyncTexture2D GetErrorEmblem() => this.GetEmblem();

    protected virtual AsyncTexture2D GetErrorCornerIcon() => this.GetCornerIcon();

    /// <summary>
    ///     Gets called after the base settings window has been constructed. Used to add custom tabs.
    /// </summary>
    /// <param name="settingWindow">The settings window.</param>
    protected virtual void OnSettingWindowBuild(TabbedWindow settingWindow) { }

    /// <inheritdoc />
    protected override void Update(GameTime gameTime)
    {
        _ = UpdateUtil.UpdateAsync(this.CheckBackendHealth, gameTime, _checkBackendInterval.TotalMilliseconds, this._lastBackendCheck, false);

        this.ShowUI = this.CalculateUIVisibility();

        using (this._servicesLock.Lock())
        {
            List<string> stateLoadingTexts = new List<string>();
            foreach (ManagedService state in this._services)
            {
                state.Update(gameTime);

                if (state is APIService apiService)
                {
                    bool loading = apiService.Loading;

                    if (loading)
                    {
                        if (!string.IsNullOrWhiteSpace(apiService.ProgressText))
                        {
                            stateLoadingTexts.Add($"{state.GetType().Name}: {apiService.ProgressText}");
                        }
                        else
                        {
                            stateLoadingTexts.Add(state.GetType().Name);
                        }
                    }
                }
            }

            string stateTexts = stateLoadingTexts.Count == 0 ? null : $"Services:\n{new string(' ', 4)}" + string.Join($"\n{new string(' ', 4)}", stateLoadingTexts);
            this.ReportLoading("states", stateTexts);
        }

        StringBuilder loadingTexts = new StringBuilder();
        foreach (KeyValuePair<string, string> loadingText in this._loadingTexts)
        {
            if (loadingText.Value == null)
            {
                continue;
            }

            loadingTexts.AppendLine(loadingText.Value);
        }

        this.HandleLoadingSpinner(loadingTexts.Length > 0, loadingTexts.ToString().Trim());
    }

    /// <summary>
    ///     Report a new loading text to display. Report <see cref="null" /> to finish.
    /// </summary>
    /// <param name="loadingText"></param>
    protected void ReportLoading(string group, string loadingText)
    {
        this._loadingTexts.AddOrUpdate(group, loadingText, (key, oldVal) => loadingText);
    }

    /// <summary>
    ///     Report a new error state to display. Report <see cref="null" /> to finish.
    /// </summary>
    /// <param name="group"></param>
    /// <param name="errorText"></param>
    protected void ReportErrorState(ModuleErrorStateGroup group, string errorText)
    {
        this._errorStates.AddOrUpdate(group, errorText, (key, oldVal) => errorText);

        StringBuilder errorStates = new StringBuilder();
        var hasMultipleErrorStates = this._errorStates.Where(e => e.Value != null).Count() > 1;
        foreach (KeyValuePair<ModuleErrorStateGroup, string> errorState in this._errorStates)
        {
            if (errorState.Value == null) continue;

            if (hasMultipleErrorStates)
            {
                errorStates.AppendLine($"- {errorState.Value.Trim()}");
            }
            else
            {
                errorStates.AppendLine(errorState.Value.Trim());
            }
        }

        this.ErrorStateText = errorStates.ToString().Trim();

        this.UpdateCornerIcon();
    }

    /// <summary>
    ///     Indicates if the specified group has declared any error state.
    /// </summary>
    /// <param name="group"></param>
    /// <returns></returns>
    protected bool HasErrorState(ModuleErrorStateGroup group)
    {
        return this._errorStates.Any(e => e.Key == group && !string.IsNullOrWhiteSpace(e.Value));
    }

    /// <summary>
    ///     Calculates the ui visibility based on settings or mumble parameters.
    /// </summary>
    /// <returns>The newly calculated ui visibility or the last value of <see cref="ShowUI" />.</returns>
    protected virtual bool CalculateUIVisibility()
    {
        if (!this.ModuleSettings.GlobalDrawerVisible.Value)
        {
            return false;
        }

        bool show = true;
        if (this.ModuleSettings.HideOnOpenMap.Value)
        {
            show &= !GameService.Gw2Mumble.UI.IsMapOpen;
        }

        if (this.ModuleSettings.HideOnMissingMumbleTicks.Value)
        {
            show &= GameService.Gw2Mumble.TimeSinceTick.TotalSeconds < 0.5;
        }

        if (this.ModuleSettings.HideInCombat.Value)
        {
            show &= !GameService.Gw2Mumble.PlayerCharacter.IsInCombat;
        }

        // All maps not specified as competetive will be treated as open world
        if (this.ModuleSettings.HideInPvE_OpenWorld.Value)
        {
            MapType[] pveOpenWorldMapTypes =
            {
                MapType.Public,
                MapType.Instance,
                MapType.Tutorial,
                MapType.PublicMini
            };

            show &= !(!GameService.Gw2Mumble.CurrentMap.IsCompetitiveMode && pveOpenWorldMapTypes.Any(type => type == GameService.Gw2Mumble.CurrentMap.Type) && !MapInfo.MAP_IDS_PVE_COMPETETIVE.Contains(GameService.Gw2Mumble.CurrentMap.Id));
        }

        if (this.ModuleSettings.HideInPvE_Competetive.Value)
        {
            MapType[] pveCompetetiveMapTypes =
            {
                MapType.Instance
            };

            show &= !(!GameService.Gw2Mumble.CurrentMap.IsCompetitiveMode && pveCompetetiveMapTypes.Any(type => type == GameService.Gw2Mumble.CurrentMap.Type) && MapInfo.MAP_IDS_PVE_COMPETETIVE.Contains(GameService.Gw2Mumble.CurrentMap.Id));
        }

        if (this.ModuleSettings.HideInWvW.Value)
        {
            MapType[] wvwMapTypes =
            {
                MapType.EternalBattlegrounds,
                MapType.GreenBorderlands,
                MapType.RedBorderlands,
                MapType.BlueBorderlands,
                MapType.EdgeOfTheMists
            };

            show &= !(GameService.Gw2Mumble.CurrentMap.IsCompetitiveMode && wvwMapTypes.Any(type => type == GameService.Gw2Mumble.CurrentMap.Type));
        }

        if (this.ModuleSettings.HideInPvP.Value)
        {
            MapType[] pvpMapTypes =
            {
                MapType.Pvp,
                MapType.Tournament
            };

            show &= !(GameService.Gw2Mumble.CurrentMap.IsCompetitiveMode && pvpMapTypes.Any(type => type == GameService.Gw2Mumble.CurrentMap.Type));
        }

        return show;
    }

    protected void HandleLoadingSpinner(bool show, string text = null)
    {
        show &= this.CornerIcon != null && this.CornerIcon.Visible;

        this._loadingSpinner ??= new LoadingSpinner
        {
            Parent = GameService.Graphics.SpriteScreen,
            Size = this.CornerIcon?.Size ?? new Point(0, 0),
            Visible = false
        };

        if (this.CornerIcon != null)
        {
            this._loadingSpinner.Location = new Point(this.CornerIcon.Location.X, this.CornerIcon.Location.Y + this.CornerIcon.Height + 5);
        }

        this._loadingSpinner.BasicTooltipText = text;
        this._loadingSpinner.Visible = show;
    }

    protected async Task ReloadServices()
    {
        List<Task> tasks = new List<Task>();

        using (await this._servicesLock.LockAsync())
        {
            tasks.AddRange(this._services.Select(s => s.Reload()));
        }

        await Task.WhenAll(tasks);
    }

    protected override void Unload()
    {
        this._cancellationTokenSource?.Cancel();

        this.BackendConnectionRestored -= this.BaseModule_BackendConnectionRestored;

        this.Logger.Debug("Unload settings...");

        if (this.ModuleSettings != null)
        {
            this.ModuleSettings.RegisterCornerIcon.SettingChanged -= this.RegisterCornerIcon_SettingChanged;
            this.ModuleSettings.Unload();
            this.ModuleSettings = null;
        }

        this.Logger.Debug("Unloaded settings.");

        this.Logger.Debug("Unload default settings view...");

        if (this._defaultSettingView != null)
        {
            this._defaultSettingView.OpenClicked -= this.DefaultSettingView_OpenClicked;
            this._defaultSettingView.CreateGithubIssueClicked -= this.DefaultSettingView_CreateGithubIssueClicked;
            this._defaultSettingView.OpenMessageLogClicked -= this.DefaultSettingView_OpenMessageLogClicked;
            this._defaultSettingView.DoUnload();
            this._defaultSettingView = null;
        }

        this.Logger.Debug("Unloaded default settings view.");

        this.Logger.Debug("Unload settings window...");

        this.SettingsWindow?.Hide();
        this.SettingsWindow?.Dispose();
        this.SettingsWindow = null;

        this.Logger.Debug("Unloaded settings window.");

        this.Logger.Debug("Unloading states...");

        using (this._servicesLock.Lock())
        {
            this._services?.ToList().ForEach(state => state?.Dispose());
            this._services?.Clear();

            this.AccountService = null;
            this.ArcDPSService = null;
            this.IconService = null;
            this.ItemService = null;
            this.MapchestService = null;
            this.WorldbossService = null;
            this.PointOfInterestService = null;
            this.SettingEventService = null;
            this.SkillService = null;
            this.PlayerTransactionsService = null;
            this.TransactionsService = null;
            this.TranslationService = null;
        }

        this.Logger.Debug("Unloaded states.");

        this.Logger.Debug("Unload flurl client...");

        this._flurlClient?.Dispose();
        this._flurlClient = null;

        this.Logger.Debug("Unloaded flurl client.");

        this.Logger.Debug("Unload corner icon...");

        this._loadingTexts?.Clear();

        this.HandleCornerIcon(false);
        this._loadingSpinner?.Dispose();
        this._loadingSpinner = null;

        this.Logger.Debug("Unloaded corner icon.");

        this.MessageContainer?.Dispose();
        this.MessageContainer = null;
    }
}