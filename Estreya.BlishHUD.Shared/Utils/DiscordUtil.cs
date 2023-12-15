namespace Estreya.BlishHUD.Shared.Utils;

using SharpDX.Direct3D9;
using System.Text.RegularExpressions;

public static class DiscordUtil
{
    public const string ESTREYA_DISCORD_NAME = "estreya";
    public const string ESTREYA_DISCORD_LINK = "discord://-/users/248146979773874177/";

    private static readonly Regex _usernameRegEx = new Regex("^(?!.*?\\.{2,})[a-z0-9_\\.]{2,32}$", RegexOptions.Singleline | RegexOptions.Compiled);

    public static bool IsValidUsername(string username)
    {
        return _usernameRegEx.IsMatch(username);
    }
}