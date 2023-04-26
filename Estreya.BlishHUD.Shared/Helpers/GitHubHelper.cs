namespace Estreya.BlishHUD.Shared.Helpers;

using Blish_HUD;
using Blish_HUD.Controls;
using Estreya.BlishHUD.Shared.Controls;
using Estreya.BlishHUD.Shared.Security;
using Estreya.BlishHUD.Shared.Services;
using Estreya.BlishHUD.Shared.UI.Views;
using Estreya.BlishHUD.Shared.Utils;
using Humanizer;
using Microsoft.Xna.Framework;
using Octokit;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

public class GitHubHelper : IDisposable
{
    private static Logger Logger = Logger.GetLogger<GitHubHelper>();

    private readonly string _owner;
    private readonly string _repository;
    private readonly string _clientId;
    private readonly string _moduleName;
    private readonly PasswordManager _passwordManager;
    private readonly IconService _iconService;
    private readonly TranslationService _translationService;
    private StandardWindow _window;
    private readonly GitHubClient _github;

    #region Views
    private GitHubCreateIssueView _issueView;
    #endregion

    public GitHubHelper(string owner, string repository, string clientId, string moduleName, PasswordManager passwordManager, IconService iconService, TranslationService translationService)
    {
        this._owner = owner;
        this._repository = repository;
        this._clientId = clientId;
        this._moduleName = moduleName;
        this._passwordManager = passwordManager;
        this._iconService = iconService;
        this._translationService = translationService;
        this._github = new GitHubClient(new ProductHeaderValue(moduleName.Dehumanize()));
        this.CreateWindow();
    }

    private async Task Login()
    {
        bool needNewToken = this._github.Credentials?.AuthenticationType != AuthenticationType.Oauth || string.IsNullOrWhiteSpace(this._github.Credentials?.Password);

        if (needNewToken)
        {
            if (this._passwordManager != null)
            {
                var githubTokenData = await this._passwordManager.Retrive("github", true);

                if (githubTokenData != null)
                {
                    var githubToken = Encoding.UTF8.GetString(githubTokenData);
                    this._github.Credentials = new Credentials(githubToken);

                    needNewToken = false;
                }
            }
        }

        if (!needNewToken)
        {
            try
            {
                User user = await this._github.User.Current();
            }
            catch (AuthorizationException)
            {
                needNewToken = true;
            }
        }

        if (needNewToken)
        {
            OauthDeviceFlowRequest request = new OauthDeviceFlowRequest(this._clientId);
            OauthDeviceFlowResponse deviceFlowResponse = await this._github.Oauth.InitiateDeviceFlow(request);
            Controls.ScreenNotification notification = new Controls.ScreenNotification($"GITHUB: Enter the code {deviceFlowResponse.UserCode}", Controls.ScreenNotification.NotificationType.Info, duration: TimeSpan.FromMinutes(15).Seconds)
            {
                Parent = GameService.Graphics.SpriteScreen,
                Opacity = 1f,
                Visible = true
            };

            Process.Start(deviceFlowResponse.VerificationUri);

            try
            {
                OauthToken token = await this._github.Oauth.CreateAccessTokenForDeviceFlow(this._clientId, deviceFlowResponse);

                if (this._passwordManager != null)
                {
                    await this._passwordManager.Save("github", Encoding.UTF8.GetBytes(token.AccessToken), true);
                }

                this._github.Credentials = new Credentials(token.AccessToken);
            }
            finally
            {
                notification.Dispose();
            }
        }
    }

    private void CreateWindow()
    {
        this._window?.Dispose();

        this._window = new StandardWindow(this._iconService.GetIcon("155985.png"), new Rectangle(40, 26, 913, 691), new Rectangle(70, 71, 839, 605))
        {
            Parent = GameService.Graphics.SpriteScreen,
            Title = "Create Issue",
            Emblem = this._iconService.GetIcon("156022.png"),
            SavesPosition = true,
            Id = $"{nameof(GitHubHelper)}_ec5d3b09-b304-44c9-b70b-a4713ba8ffbf"
        };
    }

    public void OpenIssueWindow(string title = null, string message = null)
    {
        this.UnloadIssueView();

        this._issueView = new GitHubCreateIssueView(this._moduleName, this._iconService, this._translationService, GameService.Content.DefaultFont18, title, message);
        this._issueView.CreateClicked += this.IssueView_CreateClicked;
        this._issueView.CancelClicked += this.IssueView_CancelClicked;

        this._window.Show(this._issueView);
    }

    private async Task IssueView_CreateClicked(object sender, (string Title, string Message, string DiscordName, bool IncludeSystemInformation) e)
    {
        await this.CreateIssue(e.Title, e.Message, e.DiscordName, e.IncludeSystemInformation);
        this._window.Hide();
    }

    private void IssueView_CancelClicked(object sender, EventArgs e)
    {
        this._window.Hide();
    }

    private void UnloadIssueView()
    {
        if (this._issueView != null)
        {
            this._issueView.CreateClicked -= this.IssueView_CreateClicked;
            this._issueView.CancelClicked -= this.IssueView_CancelClicked;
            this._issueView.DoUnload();
        }
    }

    private async Task CreateIssue(string title, string message, string discordName = null, bool includeSystemInformation = false)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentNullException(nameof(title), "Title is required.");
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentNullException(nameof(message), "Message is required.");
        }

        if (!string.IsNullOrWhiteSpace(discordName) && !DiscordUtil.IsValidUsername(discordName))
        {
            throw new ArgumentException($"The username \"{discordName}\" is not valid.");
        }

        var issueMessage = $"**Message**:\n{message}\n\n**Discord**: {discordName ?? string.Empty}\n\n**This Issue was created automatically by the module {this._moduleName}**";

        await this.Login();

        var newIssue = new NewIssue(title)
        {
            Body = issueMessage,
        };

        newIssue.Labels.Add($"Module: {this._moduleName}");

        Issue issue = await this._github.Issue.Create(this._owner, this._repository, newIssue);

        if (includeSystemInformation)
        {
            var dxDiagInformation = await WindowsUtil.GetDxDiagInformation();

            if (string.IsNullOrWhiteSpace(dxDiagInformation))
            {
                Logger.Warn("Could not fetch dx diag information.");
            }
            else
            {
                await _github.Issue.Comment.Create(this._owner, this._repository, issue.Number, dxDiagInformation);
            }
        }

        Process.Start(issue.HtmlUrl);
    }

    public void Dispose()
    {
        this._window?.Hide();

        this.UnloadIssueView();

        this._window?.Dispose();
    }
}
