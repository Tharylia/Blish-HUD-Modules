namespace Estreya.BlishHUD.UniversalSearch;

using Blish_HUD.Input;
using Blish_HUD.Settings;
using Estreya.BlishHUD.Shared.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ModuleSettings : BaseModuleSettings
{
    public SettingEntry<bool> NotifyOnCopy { get; private set; }
    public SettingEntry<bool> CloseWindowAfterCopy { get; private set; }

    public SettingEntry<bool> PasteInChatAfterCopy { get; private set; }

    public ModuleSettings(SettingCollection settings) : base(settings, new KeyBinding(Microsoft.Xna.Framework.Input.ModifierKeys.Alt, Microsoft.Xna.Framework.Input.Keys.U))
    {
    }

    protected override void DoInitializeGlobalSettings(SettingCollection globalSettingCollection)
    {
        this.NotifyOnCopy = globalSettingCollection.DefineSetting(nameof(this.NotifyOnCopy), true, () => "Notify on copying Result", () => "Whether a Screen Notification should be displayed after copying a result.");

        this.CloseWindowAfterCopy = globalSettingCollection.DefineSetting(nameof(this.CloseWindowAfterCopy), true, () => "Close Window after Copy", () => "Whether the search window should be closed after a successful copy.");

        this.PasteInChatAfterCopy = globalSettingCollection.DefineSetting(nameof(this.PasteInChatAfterCopy), true, () => "Paste in Chat after Copy", () => "Whether the copied information should be pasted in chat.");
    }
}
