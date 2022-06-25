namespace Estreya.BlishHUD.Shared.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class GW2Utils
{
    public static string FormatCoins(int coins)
    {
        var splitCoins = SplitCoins(coins);

        return splitCoins.Gold > 0 ? $"{splitCoins.Gold}g {splitCoins.Silver}s {splitCoins.Copper}c" : splitCoins.Silver > 0 ? $"{splitCoins.Silver}s {splitCoins.Copper}c" : $"{splitCoins.Copper}c";
    }

    public static (int Gold, int Silver, int Copper) SplitCoins(int coins)
    {
        var copper = coins % 100;
        coins = (coins - copper) / 100;
        var silver = coins % 100;
        var gold = (coins - silver) / 100;

        return (gold, silver, copper);
    }
    
    public static int ToCoins(int gold, int silver, int copper)
    {
        int coins = copper;
        coins += silver * 100;
        coins += gold * 10000;

        return coins;
    }
}
