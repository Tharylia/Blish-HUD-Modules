namespace Estreya.BlishHUD.WebhookUpdater.UI.Views;

using Blish_HUD;
using Blish_HUD.Content;
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
    private static readonly Logger Logger = Logger.GetLogger<WebhookSettingsView>();
    private const int PADDING_X = 20;
    private const int PADDING_Y = 20;

    private readonly Func<IEnumerable<Webhook>> _webhooksFunc;
    private IEnumerable<Webhook> _webhooks;
    private Dictionary<string, MenuItem> _menuItems = new Dictionary<string, MenuItem>();
    private Panel _webhookPanel;
    private StandardWindow _protocolWindow;

    public class AddWebhookEventArgs
    {
        public string Name { get; set; }
        public Webhook Webhook { get; set; }
    }

    public event EventHandler<AddWebhookEventArgs> AddWebhook;
    public event EventHandler<Webhook> RemoveWebhook;

    public WebhookSettingsView(Func<IEnumerable<Webhook>> getWebhooks, Gw2ApiManager apiManager, IconState iconState, TranslationState translationState, SettingEventState settingEventState, BitmapFont font = null) : base(apiManager, iconState, translationState, settingEventState, font)
    {
        this._webhooksFunc = getWebhooks;
    }

    private void LoadConfigurations()
    {
        this._webhooks = this._webhooksFunc.Invoke().ToList();
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

        foreach (Webhook webhook in this._webhooks)
        {
            string itemName = webhook.Configuration.Name;

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
                Webhook webhook = this._webhooks.Where(webhook => webhook.Configuration.Name == menuItem.Key).First();
                this.BuildEditPanel(newParent, webhookPanelBounds, menuItem.Value, webhook);
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
            Webhook webhook = this._webhooks.Where(webhook => webhook.Configuration.Name == menuItem.Key).First();
            this.BuildEditPanel(newParent, webhookPanelBounds, menuItem.Value, webhook);
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

                if (this._webhooks.Any(configuration => configuration.Configuration.Name == name))
                {
                    this.ShowError("Name already used");
                    return;
                }

                AddWebhookEventArgs addWebhookEventArgs = new AddWebhookEventArgs()
                {
                    Name = name
                };

                this.AddWebhook?.Invoke(this, addWebhookEventArgs);

                Webhook webhook = addWebhookEventArgs.Webhook;

                if (webhook == null)
                {
                    throw new ArgumentNullException("Webhook configuration could not be created.");
                }

                MenuItem menuItem = menu.AddMenuItem(name);
                menuItem.Click += (s, e) =>
                {
                    this.BuildEditPanel(parent, bounds, menuItem, webhook);
                };

                this.BuildEditPanel(parent, bounds, menuItem, webhook);
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

    private void BuildEditPanel(Panel parent, Rectangle bounds, MenuItem menuItem, Webhook webhook)
    {
        if (webhook == null)
        {
            throw new ArgumentNullException(nameof(webhook));
        }

        this.CreateWebhookPanel(parent, bounds);

        Rectangle panelBounds = new Rectangle(this._webhookPanel.ContentRegion.Location, new Point(this._webhookPanel.ContentRegion.Size.X - 50, this._webhookPanel.ContentRegion.Size.Y));

        Label webhookName = new Label()
        {
            Location = new Point(20, 20),
            Parent = this._webhookPanel,
            Font = GameService.Content.DefaultFont18,
            AutoSizeHeight = true,
            Text = webhook.Configuration.Name,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        var triggerButton = this.RenderButtonAsync(this._webhookPanel, "Trigger", async () =>
        {
            await webhook.TriggerAsync();
            Logger.Info($"Webhook \"{webhook.Configuration.Name}\" was triggered manually.");
        });

        triggerButton.Top = webhookName.Top;
        triggerButton.Left = webhookName.Left;

        var showProtocolsButton = this.RenderButton(this._webhookPanel, "Protocols", () =>
        {
            this.ShowProtocols(webhook);
        });

        showProtocolsButton.Top = triggerButton.Bottom + 5;
        showProtocolsButton.Left = triggerButton.Left;

        FlowPanel settingsPanel = new FlowPanel()
        {
            Left = webhookName.Left,
            Top = webhookName.Bottom + 50,
            Parent = this._webhookPanel,
            HeightSizingMode = SizingMode.Fill,
            WidthSizingMode = SizingMode.Fill,
            FlowDirection = ControlFlowDirection.SingleTopToBottom,
            CanScroll = true
        };

        settingsPanel.DoUpdate(GameService.Overlay.CurrentGameTime); // Dirty trick to get actual height and width

        this.RenderEmptyLine(settingsPanel);

        this.RenderEnabledSettings(settingsPanel, webhook);

        this.RenderEmptyLine(settingsPanel);

        this.RenderBehaviourSettings(settingsPanel, webhook);

        this.RenderEmptyLine(settingsPanel);

        var lastAdded = settingsPanel.Children.Last();

        StandardButton removeButton = this.RenderButtonAsync(this._webhookPanel, this.TranslationState.GetTranslation("webhookSettingsView-remove-btn", "Remove"), async () =>
        {
            var dialog = new ConfirmDialog(
                    $"Delete Webhook \"{webhook.Configuration.Name}\"", $"Your are in the process of deleting the webhook \"{webhook.Configuration.Name}\".\nThis action will delete all settings.\n\nContinue?",
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

            this.RemoveWebhook?.Invoke(this, webhook);
            Menu menu = menuItem.Parent as Menu;
            menu.RemoveChild(menuItem);
            this._menuItems.Remove(webhook.Configuration.Name);
            this.ClearWebhookPanel();
            this.LoadConfigurations();
        });

        removeButton.Top = webhookName.Top;
        removeButton.Right = panelBounds.Right;

        webhookName.Left = triggerButton.Right;
        webhookName.Width = removeButton.Left - webhookName.Left;
    }

    private void RenderEnabledSettings(FlowPanel settingsPanel, Webhook webhook)
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

        this.RenderBoolSetting(groupPanel, webhook.Configuration.Enabled);
        this.RenderEmptyLine(groupPanel, 20); // Fake bottom padding
    }

    private void RenderBehaviourSettings(FlowPanel settingsPanel, Webhook webhook)
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

        this.RenderEnumSetting(groupPanel, webhook.Configuration.Mode);
        this.RenderTextSetting(groupPanel, webhook.Configuration.Interval);
        this.RenderEnumSetting(groupPanel, webhook.Configuration.IntervalUnit);
        this.RenderBoolSetting(groupPanel, webhook.Configuration.OnlyOnUrlOrDataChange);

        this.RenderEmptyLine(groupPanel);

        var urlTextBox = this.RenderTextSetting(groupPanel, webhook.Configuration.Url).textBox;
        urlTextBox.Width = groupPanel.ContentRegion.Width - urlTextBox.Left - 20 * 2;

        this.RenderTextSetting(groupPanel, webhook.Configuration.ContentType);
        this.RenderEnumSetting(groupPanel, webhook.Configuration.HTTPMethod);

        this.RenderEmptyLine(groupPanel);

        this.RenderButtonAsync(groupPanel, "Edit Content", async () =>
        {
            var tempFile = FileUtil.CreateTempFile("handlebars");
            await FileUtil.WriteStringAsync(tempFile, webhook.Configuration.Content.Value);

            await VSCodeHelper.EditAsync(tempFile);

            webhook.Configuration.Content.Value = await FileUtil.ReadStringAsync(tempFile);
            File.Delete(tempFile);
        });

        this.RenderEmptyLine(groupPanel);

        this.RenderBoolSetting(groupPanel, webhook.Configuration.CollectProtocols);

        this.RenderEmptyLine(groupPanel, 20); // Fake bottom padding
    }

    private void ShowProtocols(Webhook webhook)
    {
        if (this._protocolWindow == null)
        {
            AsyncTexture2D windowBackground = this.IconState.GetIcon("textures/setting_window_background.png");

            Rectangle settingsWindowSize = new Rectangle(35, 26, 1100, 714);
            int contentRegionPaddingY = settingsWindowSize.Y - 15;
            int contentRegionPaddingX = settingsWindowSize.X;
            Rectangle contentRegion = new Rectangle(contentRegionPaddingX, contentRegionPaddingY, settingsWindowSize.Width - 6, settingsWindowSize.Height - contentRegionPaddingY);

            this._protocolWindow = new StandardWindow(windowBackground, settingsWindowSize, contentRegion)
            {
                Parent = GameService.Graphics.SpriteScreen,
                Title = "Webhook Protocols",
                SavesPosition = true,
                Id = $"{this.GetType().Name}_53ff05a1-a99b-4960-8971-4f9dc262e7cd"
            };
        }

        if (_protocolWindow.CurrentView != null)
        {
            var manageEventView = _protocolWindow.CurrentView as WebhookProtocolView;
        }

        var view = new WebhookProtocolView(webhook, this.APIManager, this.IconState, this.TranslationState);
        _protocolWindow.Show(view);
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
        this._webhooks = null;
        this._menuItems?.Clear();

        this._protocolWindow?.Dispose();
        this._protocolWindow = null;
    }
}
