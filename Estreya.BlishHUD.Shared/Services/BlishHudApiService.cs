namespace Estreya.BlishHUD.Shared.Services
{
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.Shared.Models.BlishHudAPI;
    using Estreya.BlishHUD.Shared.Security;
    using Estreya.BlishHUD.Shared.Threading;
    using Estreya.BlishHUD.Shared.Utils;
    using Flurl.Http;
    using Humanizer;
    using Microsoft.Xna.Framework;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;

    public class BlishHudApiService : ManagedService
    {
        private const string API_PASSWORD_KEY = "estreyaBlishHudAPI";
        private SettingEntry<string> _usernameSetting;
        private PasswordManager _passwordManager;
        private IFlurlClient _flurlClient;
        private readonly string _apiRootUrl;
        private readonly string _apiVersion;

        private static TimeSpan _checkAPITokenInterval = TimeSpan.FromMinutes(5);
        private AsyncRef<double> _lastAPITokenCheck = new AsyncRef<double>(0);

        private APITokens? APITokens { get; set; }

        public string AccessToken => this.APITokens?.AccessToken;

        public event EventHandler RefreshedLogin;
        public event EventHandler NewLogin;
        public event EventHandler LoggedOut;

        public BlishHudApiService(ServiceConfiguration configuration, SettingEntry<string> usernameSetting, PasswordManager passwordManager, IFlurlClient flurlClient, string apiRootUrl, string apiVersion) : base(configuration)
        {
            this._usernameSetting = usernameSetting;
            this._passwordManager = passwordManager;
            this._flurlClient = flurlClient;
            this._apiRootUrl = apiRootUrl;
            this._apiVersion = apiVersion;
        }

        protected override Task Initialize() => Task.CompletedTask;

        protected override void InternalUnload()
        {
            this._usernameSetting = null;
            this._passwordManager = null;
            this._flurlClient = null;
        }

        protected override void InternalUpdate(GameTime gameTime)
        {
            _ = UpdateUtil.UpdateAsync(this.CheckAPITokenExpiration, gameTime, _checkAPITokenInterval.TotalMilliseconds, this._lastAPITokenCheck);
        }

        protected override async Task Load()
        {
            await this.APILogin(throwException: false);
        }

        public string GetAPIUsername() => this._usernameSetting.Value;
        public string SetAPIUsername(string username) => this._usernameSetting.Value = username;

        public async Task<string> GetAPIPassword()
        {
            if (this._passwordManager == null)
            {
                throw new ArgumentNullException(nameof(this._passwordManager));
            }

            var passwordData = await this._passwordManager.Retrive(API_PASSWORD_KEY, true);

            var password = passwordData == null ? null : Encoding.UTF8.GetString(passwordData);

            return password;
        }

        public async Task SetAPIPassword(string password)
        {
            if (this._passwordManager == null)
            {
                throw new ArgumentNullException(nameof(this._passwordManager));
            }

            if (password == null)
            {
                this._passwordManager.Delete(API_PASSWORD_KEY);
            }
            else
            {
                await this._passwordManager.Save(API_PASSWORD_KEY, Encoding.UTF8.GetBytes(password));
            }
        }

        private ApiJwtPayload? GetTokenPayload(string token = null)
        {
            token ??= this.APITokens?.AccessToken;

            if (!string.IsNullOrWhiteSpace(token))
            {
                return Jose.JWT.Payload<ApiJwtPayload>(token);
            }

            return null;
        }

        public async Task TestLogin(string username = null, string password = null)
        {
            await this.APILogin(username, password, true, true);
        }

        public async Task Login()
        {
            await this.APILogin(throwException: true);
        }

        public void Logout()
        {
            this.APITokens = null;
            this.LoggedOut?.Invoke(this, EventArgs.Empty);
        }

        private async Task APILogin(string username = null, string password = null, bool dryRun = false, bool throwException = false)
        {
            try
            {
                username ??= this._usernameSetting.Value;
                password ??= await this.GetAPIPassword();

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    this.Logger.Info("Credentials not available.");
                    if (throwException)
                    {
                        throw new ArgumentNullException("Credentials");
                    }

                    return;
                }

                var response = await this._flurlClient.Request(this._apiRootUrl, $"v{this._apiVersion}", "auth", "login").PostJsonAsync(new
                {
                    username = username,
                    password = password
                });

                var content = await response.Content.ReadAsStringAsync();
                var tokens = JsonConvert.DeserializeObject<APITokens>(content);

                if (!dryRun)
                {
                    ApiJwtPayload? priorPayload = this.GetTokenPayload();

                    this.APITokens = new APITokens()
                    {
                        AccessToken = tokens.AccessToken,
                        RefreshToken = tokens.RefreshToken
                    };

                    var currentPayload = this.GetTokenPayload();

                    if (!priorPayload.HasValue || (currentPayload.HasValue && priorPayload.Value.Id != currentPayload.Value.Id))
                    {
                        this.NewLogin?.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        this.RefreshedLogin?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
            catch (Exception ex)
            {
                this.Logger.Debug(ex, "API Login failed:");

                if (!dryRun)
                {
                    this.APITokens = null;
                }

                if (throwException)
                {
                    throw;
                }
            }
        }

        private async Task RefreshAPILogin()
        {
            try
            {
                if (!this.APITokens.HasValue || string.IsNullOrWhiteSpace(this.APITokens.Value.RefreshToken))
                {
                    throw new ArgumentNullException("Refresh API Token");
                }

                var response = await this._flurlClient.Request(this._apiRootUrl, $"v{this._apiVersion}", "auth", "refresh").PostJsonAsync(new
                {
                    refreshToken = this.APITokens.Value.RefreshToken
                });

                var content = await response.Content.ReadAsStringAsync();
                var tokens = JsonConvert.DeserializeObject<APITokens>(content);

                ApiJwtPayload? priorPayload = this.GetTokenPayload();

                this.APITokens = new APITokens()
                {
                    AccessToken = tokens.AccessToken,
                    RefreshToken = tokens.RefreshToken
                };

                var currentPayload = this.GetTokenPayload();

                if (!priorPayload.HasValue || (currentPayload.HasValue && priorPayload.Value.Id != currentPayload.Value.Id))
                {
                    this.NewLogin?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    this.RefreshedLogin?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                this.Logger.Debug(ex, "Refresh API Login failed:");
                this.APITokens = null;
            }
        }

        private bool IsTokenExpired(string token)
        {
            var payload = this.GetTokenPayload(token);

            if (!payload.HasValue) return true;

            var expiresAt = DateTimeOffset.FromUnixTimeSeconds(payload.Value.Expiration);

            return DateTime.UtcNow > expiresAt;
        }

        private async Task CheckAPITokenExpiration()
        {
            if (!this.APITokens.HasValue) return;

            var accessTokenExpired = this.IsTokenExpired(this.APITokens.Value.AccessToken);
            if (!accessTokenExpired) return;

            var refreshTokenExpired = this.IsTokenExpired(this.APITokens.Value.RefreshToken);

            if (refreshTokenExpired)
            {
                // Trigger complete new login
                await this.APILogin(throwException: false);
            }
            else
            {
                await this.RefreshAPILogin();
                // Trigger login refresh 
            }
        }
    }
}
