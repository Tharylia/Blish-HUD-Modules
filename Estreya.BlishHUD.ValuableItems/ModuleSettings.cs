namespace Estreya.BlishHUD.ValuableItems;

using Blish_HUD.Input;
using Blish_HUD.Settings;
using Microsoft.Xna.Framework;
using Shared.Settings;

public class ModuleSettings : BaseModuleSettings
{
    public SettingEntry<Rectangle> OCRRegion { get; private set; }

    public ModuleSettings(SettingCollection settings, SemVer.Version moduleVersion) : base(settings, moduleVersion, new KeyBinding())
    {

    }

    protected override void DoInitializeGlobalSettings(SettingCollection globalSettingCollection)
    {
       this.OCRRegion = globalSettingCollection.DefineSetting(nameof(this.OCRRegion), Rectangle.Empty, () => "OCR Region", () => "Defines the OCR Region to scan.");
    }
}