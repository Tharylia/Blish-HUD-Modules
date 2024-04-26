namespace Estreya.BlishHUD.Shared.Services.GameIntegration
{
    using Blish_HUD;
    using Blish_HUD.Controls.Extern;
    using Blish_HUD.Controls.Intern;
    using Estreya.BlishHUD.Shared.Models.GameIntegration.Chat;
    using Estreya.BlishHUD.Shared.Models.GameIntegration.Guild;
    using Microsoft.Xna.Framework;
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using static System.Net.Mime.MediaTypeNames;
    using static System.Runtime.CompilerServices.RuntimeHelpers;

    public class ChatService : ManagedService
    {
        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        /// <summary>
        /// Inserts inputs into the input stream.
        /// </summary>
        /// <returns>How many inputs were successfully inserted into the input stream.</returns>
        /// <remarks>If blocked by UIPI neither the return value nor <seealso cref="GetLastError"/> will indicate that it was blocked.</remarks>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, [MarshalAs(UnmanagedType.LPArray), In] Windows.API.Input[] pInputs, int cbSize);

        private const uint MAPVK_VK_TO_VSC = 0x00;
        private const uint MAPVK_VSC_TO_VK = 0x01;
        private const uint MAPVK_VK_TO_CHAR = 0x02;
        private const uint MAPVK_VSC_TO_VK_EX = 0x03;
        private const uint MAPVK_VK_TO_VSC_EX = 0x04;

        public ChatService(ServiceConfiguration configuration) : base(configuration)
        {
        }

        public async Task ChangeChannel(ChatChannel channel, GuildNumber guildNumber = GuildNumber.Guild_1, string wispherRecipient = null)
        {
            if (await this.IsBusy()) throw new InvalidOperationException("The chat can't be used at the moment.");

            string channelText = channel switch
            {
                ChatChannel.Say => "/s",
                ChatChannel.Map => "/m",
                ChatChannel.Party => "/p",
                ChatChannel.Squad => "/d",
                ChatChannel.Team => "/t",
                ChatChannel.Private => "/w",
                ChatChannel.RepresentedGuild => "/g",
                ChatChannel.Guild1_5 => $"/g{(int)guildNumber}",
                _ => throw new ArgumentException($"Invalid chat channel: {channel}"),
            };

            await this.Paste(channelText);
            await Task.Delay(100);
            await this.Type(VirtualKeyShort.SPACE);
            await Task.Delay(100);

            if (channel is ChatChannel.Private)
            {
                if (string.IsNullOrWhiteSpace(wispherRecipient)) throw new ArgumentNullException(nameof(wispherRecipient), "wispher recipient can't be null or empty.");

                await this.Paste(wispherRecipient);
                await Task.Delay(100);
                await this.Type(VirtualKeyShort.TAB);
                await Task.Delay(100);
            }
        }

        public async Task Type(VirtualKeyShort virtualKey)
        {
            if (await this.IsBusy()) throw new InvalidOperationException("The chat can't be used at the moment.");

            Keyboard.Stroke(virtualKey, true);
        }

        public async Task Send(string message)
        {
            if (await this.IsBusy()) throw new InvalidOperationException("The chat can't be used at the moment.");

            await this.Paste(message);
            Keyboard.Stroke(VirtualKeyShort.RETURN);
        }

        public async Task Paste(string text)
        {
            if (await this.IsBusy()) throw new InvalidOperationException("The chat can't be used at the moment.");
            if (!await this.IsTextValid(text)) throw new ArgumentException("The text is invalid.", nameof(text));

            byte[] prevClipboardContent = await ClipboardUtil.WindowsClipboardService.GetAsUnicodeBytesAsync();

            try
            {
                await ClipboardUtil.WindowsClipboardService.SetTextAsync(text);
                await this.Focus();
                Keyboard.Press(VirtualKeyShort.LCONTROL, sendToSystem: true);
                Keyboard.Stroke(VirtualKeyShort.KEY_V, sendToSystem: true);
                Thread.Sleep(50);
                Keyboard.Release(VirtualKeyShort.LCONTROL, sendToSystem: true);
            }
            catch (Exception ex)
            {
                this.Logger.Warn(ex, "Failed to paste {text}", text);
                throw;
            }

            if (prevClipboardContent != null)
            {
                await ClipboardUtil.WindowsClipboardService.SetUnicodeBytesAsync(prevClipboardContent);
            }
        }

        public async Task<string> GetInputText()
        {
            if (await this.IsBusy()) return string.Empty;

            byte[] prevClipboardContent = await ClipboardUtil.WindowsClipboardService.GetAsUnicodeBytesAsync();
            await this.Focus();
            Keyboard.Press(VirtualKeyShort.LCONTROL, sendToSystem: true);
            Keyboard.Stroke(VirtualKeyShort.KEY_A, sendToSystem: true);
            Keyboard.Stroke(VirtualKeyShort.KEY_C, sendToSystem: true);
            Thread.Sleep(50);
            Keyboard.Release(VirtualKeyShort.LCONTROL, sendToSystem: true);
            await this.Unfocus();

            string text = await ClipboardUtil.WindowsClipboardService.GetTextAsync();

            if (prevClipboardContent != null)
            {
                await ClipboardUtil.WindowsClipboardService.SetUnicodeBytesAsync(prevClipboardContent);
            }

            return text;
        }

        public new async Task Clear()
        {
            if (!await this.IsBusy())
            {
                await this.Focus();
                Keyboard.Press(VirtualKeyShort.LCONTROL, sendToSystem: true);
                Keyboard.Stroke(VirtualKeyShort.KEY_A, sendToSystem: true);
                Thread.Sleep(50);
                Keyboard.Release(VirtualKeyShort.LCONTROL, sendToSystem: true);
                Keyboard.Stroke(VirtualKeyShort.BACK);
                await this.Unfocus();
            }
        }

        private async Task Focus()
        {
            if (await this.IsFocused()) return;

            await this.Unfocus();
            Keyboard.Stroke(VirtualKeyShort.RETURN);
        }

        private async Task Unfocus()
        {
            if (!await this.IsFocused()) return;

            Mouse.Click(MouseButton.LEFT, GameService.Graphics.WindowWidth / 2, 0);
        }

        private Task<bool> IsTextValid(string text)
        {
            return Task.FromResult(text != null && text.Length < 200);
        }

        private Task<bool> IsFocused()
        {
            return Task.FromResult(GameService.Gw2Mumble.UI.IsTextInputFocused);
        }

        private Task<bool> IsBusy()
        {
            return Task.FromResult(!GameService.GameIntegration.Gw2Instance.Gw2IsRunning || !GameService.GameIntegration.Gw2Instance.Gw2HasFocus || !GameService.GameIntegration.Gw2Instance.IsInGame);
        }

        protected override Task Initialize()
        {
            return Task.CompletedTask;
        }

        protected override Task Load()
        {
            return Task.CompletedTask;
        }

        protected override void InternalUpdate(GameTime gameTime) { }

        protected override void InternalUnload() { }
    }
}
