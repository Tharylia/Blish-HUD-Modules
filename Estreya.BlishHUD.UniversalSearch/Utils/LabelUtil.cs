namespace Estreya.BlishHUD.UniversalSearch.Utils;

using Blish_HUD.Controls;
using System;

public static class LabelUtil
{
    // Poor mans max width implementation
    public static void HandleMaxWidth(Label control, int maxWidth, int offset = 0, Action afterRecalculate = null)
    {
        if (control.Width > maxWidth - offset)
        {
            control.AutoSizeWidth = false;
            control.Width = maxWidth - offset;
            control.WrapText = true;
            control.RecalculateLayout();
            afterRecalculate?.Invoke();
        }
    }
}