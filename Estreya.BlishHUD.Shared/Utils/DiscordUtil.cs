namespace Estreya.BlishHUD.Shared.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public static class DiscordUtil
{
    private static readonly Regex _usernameRegEx = new Regex("^.{3,32}#[0-9]{4}$", RegexOptions.Singleline | RegexOptions.Compiled);

    public static bool IsValidUsername(string username)
    {
        return _usernameRegEx.IsMatch(username);
    }
}
