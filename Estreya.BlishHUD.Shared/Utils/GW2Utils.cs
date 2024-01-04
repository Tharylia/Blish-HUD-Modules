namespace Estreya.BlishHUD.Shared.Utils;

using Microsoft.Win32;

public static class GW2Utils
{
    public static string FormatCoins(int coins)
    {
        (int Gold, int Silver, int Copper) splitCoins = SplitCoins(coins);

        return splitCoins.Gold > 0 ? $"{splitCoins.Gold}g {splitCoins.Silver}s {splitCoins.Copper}c" : splitCoins.Silver > 0 ? $"{splitCoins.Silver}s {splitCoins.Copper}c" : $"{splitCoins.Copper}c";
    }

    public static (int Gold, int Silver, int Copper) SplitCoins(int coins)
    {
        int copper = coins % 100;
        coins = (coins - copper) / 100;
        int silver = coins % 100;
        int gold = (coins - silver) / 100;

        return (gold, silver, copper);
    }

    public static int ToCoins(int gold, int silver, int copper)
    {
        int coins = copper;
        coins += silver * 100;
        coins += gold * 10000;

        return coins;
    }

    public static string GetInstallPath()
    {
        var regKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default).OpenSubKey("SOFTWARE\\ArenaNet\\Guild Wars 2");
        return (string)regKey.GetValue("Path");
    }
}