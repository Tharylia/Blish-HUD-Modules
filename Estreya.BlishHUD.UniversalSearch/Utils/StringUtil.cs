﻿namespace Estreya.BlishHUD.UniversalSearch.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class StringUtil
{
    public static int ComputeLevenshteinDistance(string s, string t)
    {
        int n = s.Length;
        int m = t.Length;
        var d = new int[n + 1, m + 1];

        if (n == 0)
        {
            return m;
        }

        if (m == 0)
        {
            return n;
        }

        for (int i = 0; i <= n; d[i, 0] = i++)
        {
        }

        for (int j = 0; j <= m; d[0, j] = j++)
        {
        }

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }
        return d[n, m];
    }

    public static string SanitizeTraitDescription(string description)
    {
        var indexOfClosingBracket = description.IndexOf('>');

        while (indexOfClosingBracket != -1)
        {
            description = description.Remove(description.IndexOf('<'), indexOfClosingBracket - description.IndexOf('<') + 1);
            indexOfClosingBracket = description.IndexOf('>');
        }

        return description;
    }
}
