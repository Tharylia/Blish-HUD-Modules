namespace Estreya.BlishHUD.Shared.Utils;

using System.Text.RegularExpressions;

public static class DiscordUtil
{
    private static readonly Regex _usernameRegEx = new Regex("^.{3,32}#[0-9]{4}$", RegexOptions.Singleline | RegexOptions.Compiled);

    public static bool IsValidUsername(string username)
    {
        return _usernameRegEx.IsMatch(username);
    }
}