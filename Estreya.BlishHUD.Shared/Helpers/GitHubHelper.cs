namespace Estreya.BlishHUD.Shared.Helpers;

using Blish_HUD;
using Controls;
using Humanizer;
using Estreya.BlishHUD.Shared.Controls.Input;
using Microsoft.Xna.Framework;
using Octokit;
using Security;
using Services;
using Settings;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using UI.Views;
using Utils;

public class GitHubHelper : IDisposable
{
    private static readonly Logger Logger = Logger.GetLogger<GitHubHelper>();
    private readonly BaseModuleSettings _baseModuleSettings;
    private readonly string _clientId;
    private readonly GitHubClient _github;
    private readonly IconService _iconService;
    private readonly string _moduleName;

    private readonly string _owner;
    private readonly PasswordManager _passwordManager;
    private readonly string _repository;
    private readonly TranslationService _translationService;

    #region Views

    private GitHubCreateIssueView _issueView;

    #endregion

    private StandardWindow _window;

    public GitHubHelper(string owner, string repository, string clientId, string moduleName, PasswordManager passwordManager, IconService iconService, TranslationService translationService, BaseModuleSettings baseModuleSettings)
    {
        this._owner = owner;
        this._repository = repository;
        this._clientId = clientId;
        this._moduleName = moduleName;
        this._passwordManager = passwordManager;
        this._iconService = iconService;
        this._translationService = translationService;
        this._baseModuleSettings = baseModuleSettings;
        this._github = new GitHubClient(new ProductHeaderValue(moduleName.Dehumanize()));
        this.CreateWindow();
    }

    public void Dispose()
    {
        this._window?.Hide();

        this.UnloadIssueView();

        this._window?.Dispose();
    }

    private async Task Login()
    {
        bool needNewToken = this._github.Credentials?.AuthenticationType != AuthenticationType.Oauth || string.IsNullOrWhiteSpace(this._github.Credentials?.Password);

        if (needNewToken)
        {
            if (this._passwordManager != null)
            {
                byte[] githubTokenData = await this._passwordManager.Retrive("github", true);

                if (githubTokenData != null)
                {
                    string githubToken = Encoding.UTF8.GetString(githubTokenData);
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
            Process.Start(deviceFlowResponse.VerificationUri);
            ConfirmDialog confirmDialog = new ConfirmDialog("GitHub Login", $"Enter the code \"{deviceFlowResponse.UserCode}\" in the opened GitHub browser window.", this._iconService);

            var confirmResult = await confirmDialog.ShowDialog();

            if (confirmResult != DialogResult.OK) throw new Exception("Login cancelled");

            OauthToken token = await this._github.Oauth.CreateAccessTokenForDeviceFlow(this._clientId, deviceFlowResponse);

            if (this._passwordManager != null)
            {
                await this._passwordManager.Save("github", Encoding.UTF8.GetBytes(token.AccessToken), true);
            }

            this._github.Credentials = new Credentials(token.AccessToken);
        }
    }

    private void CreateWindow()
    {
        this._window?.Dispose();

        this._window = new StandardWindow(this._baseModuleSettings, this._iconService.GetIcon("155985.png"), new Rectangle(40, 26, 913, 691), new Rectangle(70, 71, 839, 605))
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

        this._issueView = new GitHubCreateIssueView(this._moduleName, this._iconService, this._translationService, title, message);
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

        string issueMessage = $"**Message**:\n{message}\n\n**Discord**: {discordName ?? string.Empty}\n\n**This Issue was created automatically by the module {this._moduleName}**";

        await this.Login();

        NewIssue newIssue = new NewIssue(title) { Body = issueMessage };

        newIssue.Labels.Add($"Module: {this._moduleName}");

        Issue issue = await this._github.Issue.Create(this._owner, this._repository, newIssue);

        if (includeSystemInformation)
        {
            string dxDiagInformation = await WindowsUtil.GetDxDiagInformation();

            if (string.IsNullOrWhiteSpace(dxDiagInformation))
            {
                Logger.Warn("Could not fetch dx diag information.");
            }
            else
            {
                await this._github.Issue.Comment.Create(this._owner, this._repository, issue.Number, dxDiagInformation);
            }
        }

        Process.Start(issue.HtmlUrl);
    }
}