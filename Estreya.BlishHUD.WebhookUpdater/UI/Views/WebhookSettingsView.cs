namespace Estreya.BlishHUD.WebhookUpdater.UI.Views;

using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Controls.Intern;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.Controls;
using Estreya.BlishHUD.Shared.Helpers;
using Estreya.BlishHUD.Shared.Models.ArcDPS;
using Estreya.BlishHUD.Shared.State;
using Estreya.BlishHUD.Shared.UI.Views;
using Estreya.BlishHUD.Shared.Utils;
using Estreya.BlishHUD.WebhookUpdater.Models;
using Humanizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Blish_HUD.ContentService;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using Menu = Shared.Controls.Menu;

public class WebhookSettingsView : BaseSettingsView
{
    private const int PADDING_X = 20;
    private const int PADDING_Y = 20;

    private readonly Func<IEnumerable<WebhookConfiguration>> _webhookConfigurationFunc;
    private IEnumerable<WebhookConfiguration> _webhookConfigurations;
    private Dictionary<string, MenuItem> _menuItems = new Dictionary<string, MenuItem>();
    private Panel _webhookPanel;

    public class AddWebhookEventArgs
    {
        public string Name { get; set; }
        public WebhookConfiguration Configuration { get; set; }
    }

    public event EventHandler<AddWebhookEventArgs> AddWebhook;
    public event EventHandler<WebhookConfiguration> RemoveWebhook;

    public WebhookSettingsView(Func<IEnumerable<WebhookConfiguration>> getWebhookConfigurations, Gw2ApiManager apiManager, IconState iconState, TranslationState translationState, SettingEventState settingEventState, BitmapFont font = null) : base(apiManager, iconState, translationState, settingEventState, font)
    {
        this._webhookConfigurationFunc = getWebhookConfigurations;
    }

    private void LoadConfigurations()
    {
        this._webhookConfigurations = this._webhookConfigurationFunc.Invoke().ToList();
    }

    protected override void BuildView(FlowPanel parent)
    {
        this.LoadConfigurations();

        Panel newParent = this.GetPanel(parent.Parent);
        newParent.Location = parent.Location;
        newParent.Size = parent.Size;
        newParent.HeightSizingMode = parent.HeightSizingMode;
        newParent.WidthSizingMode = parent.WidthSizingMode;

        Rectangle bounds = new Rectangle(PADDING_X, PADDING_Y, newParent.ContentRegion.Width - PADDING_X, newParent.ContentRegion.Height - PADDING_Y * 2);

        Panel webhookOverviewPanel = this.GetPanel(newParent);
        webhookOverviewPanel.ShowBorder = true;
        webhookOverviewPanel.CanScroll = true;
        webhookOverviewPanel.HeightSizingMode = SizingMode.Standard;
        webhookOverviewPanel.WidthSizingMode = SizingMode.Standard;
        webhookOverviewPanel.Location = new Point(bounds.X, bounds.Y);
        webhookOverviewPanel.Size = new Point(Panel.MenuStandard.Size.X - 75, bounds.Height - StandardButton.STANDARD_CONTROL_HEIGHT);

        Shared.Controls.Menu webhookOverviewMenu = new Shared.Controls.Menu
        {
            Parent = webhookOverviewPanel,
            WidthSizingMode = SizingMode.Fill
        };

        foreach (WebhookConfiguration webhookConfiguration in this._webhookConfigurations)
        {
            string itemName = webhookConfiguration.Name;

            if (string.IsNullOrWhiteSpace(itemName))
            {
                continue;
            }

            MenuItem menuItem = new MenuItem(itemName)
            {
                Parent = webhookOverviewMenu,
                Text = itemName,
                WidthSizingMode = SizingMode.Fill,
                HeightSizingMode = SizingMode.AutoSize
            };

            this._menuItems.Add(itemName, menuItem);
        }

        int x = webhookOverviewPanel.Right + Panel.MenuStandard.PanelOffset.X;
        Rectangle webhookPanelBounds = new Rectangle(x, bounds.Y, bounds.Width - x, bounds.Height);

        this._menuItems.ToList().ForEach(menuItem =>
        {
            menuItem.Value.Click += (s, e) =>
            {
                WebhookConfiguration webhookConfiguration = this._webhookConfigurations.Where(webhookConfiguration => webhookConfiguration.Name == menuItem.Key).First();
                this.BuildEditPanel(newParent, webhookPanelBounds, menuItem.Value, webhookConfiguration);
            };
        });

        StandardButton addButton = this.RenderButton(newParent, this.TranslationState.GetTranslation("webhookSettingsView-add-btn", "Add"), () =>
        {
            this.BuildAddPanel(newParent, webhookPanelBounds, webhookOverviewMenu);
        });

        addButton.Location = new Point(webhookOverviewPanel.Left, webhookOverviewPanel.Bottom + 10);
        addButton.Width = webhookOverviewPanel.Width;

        if (this._menuItems.Count > 0)
        {
            var menuItem = this._menuItems.First();
            WebhookConfiguration webhookConfiguration = this._webhookConfigurations.Where(webhookConfiguration => webhookConfiguration.Name == menuItem.Key).First();
            this.BuildEditPanel(newParent, webhookPanelBounds, menuItem.Value, webhookConfiguration);
        }
    }
    private void CreateWebhookPanel(Panel parent, Rectangle bounds)
    {
        this.ClearWebhookPanel();

        this._webhookPanel = this.GetPanel(parent);
        this._webhookPanel.ShowBorder = true;
        this._webhookPanel.CanScroll = false; // Should not be needed
        this._webhookPanel.HeightSizingMode = SizingMode.Standard;
        this._webhookPanel.WidthSizingMode = SizingMode.Standard;
        this._webhookPanel.Location = new Point(bounds.X, bounds.Y);
        this._webhookPanel.Size = new Point(bounds.Width, bounds.Height);
    }

    private void BuildAddPanel(Panel parent, Rectangle bounds, Menu menu)
    {
        this.CreateWebhookPanel(parent, bounds);

        Rectangle panelBounds = this._webhookPanel.ContentRegion;

        TextBox webhookName = new TextBox()
        {
            Parent = this._webhookPanel,
            Location = new Point(20, 20),
            PlaceholderText = "Webhook Name"
        };

        StandardButton saveButton = this.RenderButton(this._webhookPanel, this.TranslationState.GetTranslation("webhookSettingsView-save-btn", "Save"), () =>
        {
            try
            {
                string name = webhookName.Text;

                if (this._webhookConfigurations.Any(configuration => configuration.Name == name))
                {
                    this.ShowError("Name already used");
                    return;
                }

                AddWebhookEventArgs addWebhookEventArgs = new AddWebhookEventArgs()
                {
                    Name = name
                };

                this.AddWebhook?.Invoke(this, addWebhookEventArgs);

                WebhookConfiguration configuration = addWebhookEventArgs.Configuration;

                if (configuration == null)
                {
                    throw new ArgumentNullException("Webhook configuration could not be created.");
                }

                MenuItem menuItem = menu.AddMenuItem(name);
                menuItem.Click += (s, e) =>
                {
                    this.BuildEditPanel(parent, bounds, menuItem, configuration);
                };

                this.BuildEditPanel(parent, bounds, menuItem, configuration);
            }
            catch (Exception ex)
            {
                this.ShowError(ex.Message);
            }
        });
        saveButton.Enabled = false;
        saveButton.Right = panelBounds.Right - 20;
        saveButton.Bottom = panelBounds.Bottom - 20;

        webhookName.TextChanged += (s, e) =>
        {
            var textBox = s as TextBox;
            saveButton.Enabled = !string.IsNullOrWhiteSpace(textBox.Text);
        };

        StandardButton cancelButton = this.RenderButton(this._webhookPanel, this.TranslationState.GetTranslation("webhookSettingsView-cancel-btn", "Cancel"), () =>
        {
            this.ClearWebhookPanel();
        });
        cancelButton.Right = saveButton.Left - 10;
        cancelButton.Bottom = panelBounds.Bottom - 20;

        webhookName.Width = (panelBounds.Right - 20) - webhookName.Left;
    }

    private void BuildEditPanel(Panel parent, Rectangle bounds, MenuItem menuItem, WebhookConfiguration webhookConfiguration)
    {
        if (webhookConfiguration == null)
        {
            throw new ArgumentNullException(nameof(webhookConfiguration));
        }

        this.CreateWebhookPanel(parent, bounds);

        Rectangle panelBounds = new Rectangle(this._webhookPanel.ContentRegion.Location, new Point(this._webhookPanel.ContentRegion.Size.X - 50, this._webhookPanel.ContentRegion.Size.Y));

        Label areaName = new Label()
        {
            Location = new Point(20, 20),
            Parent = this._webhookPanel,
            Font = GameService.Content.DefaultFont18,
            AutoSizeHeight = true,
            Text = webhookConfiguration.Name,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        FlowPanel settingsPanel = new FlowPanel()
        {
            Left = areaName.Left,
            Top = areaName.Bottom + 50,
            Parent = this._webhookPanel,
            HeightSizingMode = SizingMode.Fill,
            WidthSizingMode = SizingMode.Fill,
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            CanScroll = true
        };

        settingsPanel.DoUpdate(GameService.Overlay.CurrentGameTime); // Dirty trick to get actual height and width

        this.RenderEnabledSettings(settingsPanel, webhookConfiguration);

        this.RenderEmptyLine(settingsPanel);

        this.RenderBehaviourSettings(settingsPanel, webhookConfiguration);

        this.RenderEmptyLine(settingsPanel);

        var lastAdded = settingsPanel.Children.Last();

        StandardButton removeButton = this.RenderButtonAsync(this._webhookPanel, this.TranslationState.GetTranslation("webhookSettingsView-remove-btn", "Remove"), async () =>
        {
            var dialog = new ConfirmDialog(
                    $"Delete Webhook \"{webhookConfiguration.Name}\"", $"Your are in the process of deleting the webhook \"{webhookConfiguration.Name}\".\nThis action will delete all settings.\n\nContinue?",
                    this.IconState,
                    new[]
                    {
                        new ButtonDefinition("Yes", System.Windows.Forms.DialogResult.Yes),
                        new ButtonDefinition("No", System.Windows.Forms.DialogResult.No)
                    })
            {
                SelectedButtonIndex = 1
            };

            var result = await dialog.ShowDialog();
            dialog.Dispose();

            if (result != System.Windows.Forms.DialogResult.Yes) return;

            this.RemoveWebhook?.Invoke(this, webhookConfiguration);
            Menu menu = menuItem.Parent as Menu;
            menu.RemoveChild(menuItem);
            this._menuItems.Remove(webhookConfiguration.Name);
            this.ClearWebhookPanel();
            this.LoadConfigurations();
        });

        removeButton.Top = areaName.Top;
        removeButton.Right = panelBounds.Right;

        areaName.Left = 0;
        areaName.Width = removeButton.Left - areaName.Left;
    }

    private void RenderEnabledSettings(FlowPanel settingsPanel, WebhookConfiguration webhookConfiguration)
    {
        FlowPanel groupPanel = new FlowPanel()
        {
            Parent = settingsPanel,
            HeightSizingMode = SizingMode.AutoSize,
            Width = settingsPanel.Width - 30,
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            OuterControlPadding = new Vector2(20, 20),
            ShowBorder = true,
            CanCollapse = true,
            Collapsed = false,
            Title = "Enabled"
        };

        this.RenderBoolSetting(groupPanel, webhookConfiguration.Enabled);
        this.RenderEmptyLine(groupPanel, 20); // Fake bottom padding
    }

    private void RenderBehaviourSettings(FlowPanel settingsPanel, WebhookConfiguration webhookConfiguration)
    {
        FlowPanel groupPanel = new FlowPanel()
        {
            Parent = settingsPanel,
            HeightSizingMode = SizingMode.AutoSize,
            Width = settingsPanel.Width - 30,
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            OuterControlPadding = new Vector2(20, 20),
            ShowBorder = true,
            CanCollapse = true,
            Collapsed = false,
            Title = "Behaviours"
        };

        this.RenderEnumSetting(groupPanel, webhookConfiguration.Mode);
        this.RenderTextSetting(groupPanel, webhookConfiguration.Interval);
        this.RenderEnumSetting(groupPanel, webhookConfiguration.IntervalUnit);
        this.RenderBoolSetting(groupPanel, webhookConfiguration.OnlyOnUrlOrDataChange);

        this.RenderEmptyLine(groupPanel);

        var urlTextBox = this.RenderTextSetting(groupPanel, webhookConfiguration.Url).textBox;
        urlTextBox.Width = groupPanel.ContentRegion.Width - urlTextBox.Left - 20 * 2;

        this.RenderTextSetting(groupPanel, webhookConfiguration.ContentType);

        this.RenderEmptyLine(groupPanel);

        this.RenderButtonAsync(groupPanel, "Edit Content", async () =>
        {
            var tempFile = FileUtil.CreateTempFile("handlebars");
            await FileUtil.WriteStringAsync(tempFile, webhookConfiguration.Content.Value);

            await VSCodeHelper.EditAsync(tempFile);

            webhookConfiguration.Content.Value = await FileUtil.ReadStringAsync(tempFile);
            File.Delete(tempFile);
        });

        this.RenderEmptyLine(groupPanel, 20); // Fake bottom padding
    }    

    private void ClearWebhookPanel()
    {
        if (this._webhookPanel != null)
        {
            this._webhookPanel.Hide();
            this._webhookPanel.Children?.ToList().ForEach(child => child.Dispose());
            this._webhookPanel.ClearChildren();
            this._webhookPanel.Dispose();
            this._webhookPanel = null;
        }
    }

    protected override Task<bool> InternalLoad(IProgress<string> progress)
    {
        return Task.FromResult(true);
    }

    protected override void Unload()
    {
        base.Unload();

        this.ClearWebhookPanel();
        this._webhookConfigurations = null;
        this._menuItems?.Clear();
    }
}
