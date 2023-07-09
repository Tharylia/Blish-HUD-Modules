namespace Estreya.BlishHUD.Shared.Utils;

using System;
using System.Diagnostics;
using System.Linq;

public static class StreamerUtils
{
    public static bool IsStreaming()
    {
        return IsOBSOpen();
    }

    public static bool IsOBSOpen()
    {
        try
        {
            return Process.GetProcessesByName("obs64").Any();
        }
        catch (Exception)
        {
            return false;
        }
    }
}