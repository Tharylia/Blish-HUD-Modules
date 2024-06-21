namespace Estreya.BlishHUD.Shared.Controls
{
    using Blish_HUD;
    using Blish_HUD.Controls;
    using Blish_HUD.Modules;
    using Blish_HUD.Modules.Managers;
    using Estreya.BlishHUD.Shared.Services;
    using Estreya.BlishHUD.Shared.Settings;
    using Estreya.BlishHUD.Shared.UI.Views;
    using Estreya.BlishHUD.Shared.Utils;
    using Microsoft.Xna.Framework;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class MessageContainer : IDisposable
    {
        private const string WINDOW_TITLE = "Estreya Messages";
        private readonly Gw2ApiManager _apiManager;
        private readonly TranslationService _translationService;
        private readonly IconService _iconService;
        private Window _window;
        private ContainerView _containerView;
        private FlowPanel _messagePanel;

        private ConcurrentBag<Guid> _openedGuids;

        private AsyncLock _lock = new AsyncLock();

        public MessageContainer(Gw2ApiManager apiManager, BaseModuleSettings settings, TranslationService translationService, IconService iconService, string title = WINDOW_TITLE)
        {
            this._window = WindowUtil.CreateStandardWindow(settings, title, this.GetType(), Guid.Parse("89dc6f18-9c3b-4e1b-b16f-7db71682129a"), iconService);
            this._window.Width = 500;
            this._window.Height = 500;
            this._window.Hide();
            this._apiManager = apiManager;
            this._translationService = translationService;
            this._iconService = iconService;

            this._openedGuids = new ConcurrentBag<Guid>();
        }

        private async Task CreateContainerView()
        {
            using (await this._lock.LockAsync())
            {
                if (this._containerView == null)
                {
                    this._containerView = new ContainerView(_apiManager, _iconService, _translationService);
                    await this._window.SetView(this._containerView);

                    this._messagePanel = new FlowPanel()
                    {
                        FlowDirection = ControlFlowDirection.SingleTopToBottom,
                        WidthSizingMode = SizingMode.Fill,
                        HeightSizingMode = SizingMode.Fill,
                        CanScroll = true,
                        ControlPadding = new Vector2(0, 10)
                    };
                    this._containerView.Add(this._messagePanel);
                    this._messagePanel.RecalculateLayout();
                    this._messagePanel.Update(GameService.Overlay.CurrentGameTime);
                }
            }
        }

        private string GetTimestamp()
        {
            return DateTimeOffset.Now.ToString("T");
        }

        private Color GetMessageTypeColor(MessageType type)
        {
            return type switch
            {
                MessageType.Info => Color.Green,
                MessageType.Warning => Color.Yellow,
                MessageType.Error => Color.Red,
                MessageType.Fatal => Color.MediumVioletRed,
                MessageType.Debug => Color.LightBlue,
                _ => Color.White,
            };
        }

        private bool ShouldShowItself(MessageType type)
        {
            return type switch
            {
                MessageType.Error or MessageType.Fatal => true,
                _ => false,
            };
        }

        public Task Add(Module module, MessageType messageType, string message)
        {
            return this.Add(module, Guid.NewGuid(), messageType, message);
        }

        /// <summary>
        /// Adds messages with an identification guid. If that guid already forcefully opened the container, it won't do it again.
        /// </summary>
        /// <param name="module"></param>
        /// <param name="identification"></param>
        /// <param name="messageType"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task Add(Module module, Guid identification, MessageType messageType, string message)
        {
            await this.CreateContainerView();

            using (await this._lock.LockAsync())
            {
                var panel = new Panel()
                {
                    Width = this._messagePanel.ContentRegion.Width,
                    HeightSizingMode = SizingMode.AutoSize,
                    Parent = this._messagePanel,
                    ShowBorder = true
                };

                var messageTypeColor = this.GetMessageTypeColor(messageType);
                var timestamp = this.GetTimestamp();

                var label = new FormattedLabelBuilder().SetWidth(panel.ContentRegion.Width).AutoSizeHeight().Wrap()
                    .CreatePart($"[", b => { })
                    .CreatePart($"{timestamp}", b => { b.SetTextColor(Color.Gray); })
                    .CreatePart($" - ", b => { })
                    .CreatePart($"{module.Name}", b => { b.SetTextColor(Color.DarkGray); })
                    .CreatePart($" - ", b => { })
                    .CreatePart($"{messageType}", b => { b.SetTextColor(messageTypeColor); })
                    .CreatePart($"] ", b => { })
                    //.CreatePart($"\n", b => { })
                    .CreatePart($"{message}", b => { })
                    .Build();

                label.Parent = panel;

                var containsIdentification = this._openedGuids?.ToArray().Contains(identification) ?? false;

                if (!containsIdentification && !this._window.Visible && this.ShouldShowItself(messageType))
                {
                    this._openedGuids?.Add(identification);
                    this._window.Show();
                }
            }
        }

        public void Show()
        {
            this._window?.Show();
        }

        public void Dispose()
        {
            using (this._lock.Lock())
            {
                this._window?.Dispose();
                this._containerView?.DoUnload();
                this._messagePanel?.Dispose();

                this._window = null;
                this._containerView = null;
                this._messagePanel = null;

                this._openedGuids = null;

            }
        }

        public enum MessageType
        {
            Info,
            Warning,
            Error,
            Fatal,
            Debug,
        }
    }
}
