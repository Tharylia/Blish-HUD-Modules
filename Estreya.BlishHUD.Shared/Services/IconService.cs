namespace Estreya.BlishHUD.Shared.Services;

using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.Helpers;
using Estreya.BlishHUD.Shared.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

public class IconService : ManagedService
{
    public const string RENDER_API_URL = "https://render.guildwars2.com/file/";
    public const string WIKI_URL = "https://wiki.guildwars2.com/images/";

    private static readonly WebClient _webclient = new WebClient();

    private static readonly Regex _regexRenderServiceSignatureFileIdPair = new Regex("(.{40})\\/(\\d+)(?>\\..*)?$", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex _regexWiki = new Regex("(\\d{1}\\/\\d{1}\\w{1}\\/.+\\.png)", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex _regexDat = new Regex("(\\d+)\\.png", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex _regexModuleRef = new Regex("(.+\\.png)", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex _regexCore = new Regex("(\\d+)", RegexOptions.Compiled | RegexOptions.Singleline);

    private readonly ContentsManager _contentsManager;

    public IconService(ServiceConfiguration configuration, ContentsManager contentsManager) : base(configuration)
    {
        this._contentsManager = contentsManager;
    }

    protected override Task Initialize() => Task.CompletedTask;

    protected override void InternalUnload()
    {
    }

    protected override void InternalUpdate(GameTime gameTime) { }

    protected override Task Load() => Task.CompletedTask;

    public AsyncTexture2D GetIcon(string identifier)
    {
        var iconSources = this.DetermineIconSources(identifier);
        return this.GetIcon(identifier, iconSources);
    }

    private AsyncTexture2D GetIcon(string identifier, List<IconSource> iconSources)
    {
        if (iconSources == null || iconSources.Count == 0) { return ContentService.Textures.Error; }

        AsyncTexture2D icon = new AsyncTexture2D();

        foreach (IconSource source in iconSources)
        {
            var sourceIdentifier = this.ParseIdentifierBySource(identifier, source);
            if (string.IsNullOrWhiteSpace(sourceIdentifier))
            {
                Logger.Warn($"Can't load texture by {identifier} with source {source}");
                continue;
            }

            try
            {
                switch (source)
                {
                    case IconSource.Core:
                        Texture2D coreTexture = GameService.Content.GetTexture(sourceIdentifier);
                        if (coreTexture != ContentService.Textures.Error) icon.SwapTexture(coreTexture);
                        break;
                    case IconSource.Module:
                        Texture2D moduleTexture = this._contentsManager.GetTexture(sourceIdentifier);
                        if (moduleTexture != ContentService.Textures.Error) icon.SwapTexture(moduleTexture);
                        break;
                    case IconSource.RenderAPI:
                        icon = GameService.Content.GetRenderServiceTexture(sourceIdentifier);
                        break;
                    case IconSource.DAT:
                        icon = AsyncTexture2D.FromAssetId(Convert.ToInt32(sourceIdentifier));
                        break;
                    case IconSource.Wiki:
                        var wikiUrl = !identifier.StartsWith(WIKI_URL) ? $"{WIKI_URL}{sourceIdentifier}" : sourceIdentifier;
                        var wikiTextureBytes = _webclient.DownloadData(wikiUrl);
                        icon = TextureUtil.FromStreamPremultiplied(new MemoryStream(wikiTextureBytes));
                        break;
                    case IconSource.Unknown:
                        // Don't have anything to fetch.
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Could not load icon {0}:", identifier);
            }

            if (icon != null) break;
        }

        return icon switch
        {
            null => ContentService.Textures.Error,
            _ => icon
        };
    }

    public Task<AsyncTexture2D> GetIconAsync(string identifier, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            return this.GetIcon(identifier);
        }, cancellationToken);
    }

    private string ParseIdentifierBySource(string identifier, IconSource source)
    {
        switch (source)
        {
            case IconSource.Core:
                return _regexCore.Match(identifier).Groups[1].Value;
            case IconSource.Module:
                return _regexModuleRef.Match(identifier).Groups[1].Value;
            case IconSource.RenderAPI:
                var renderApiMatch = _regexRenderServiceSignatureFileIdPair.Match(identifier);
                return $"{renderApiMatch.Groups[1].Value}/{renderApiMatch.Groups[2].Value}";
            case IconSource.DAT:
                return _regexDat.Match(identifier).Groups[1].Value;
            case IconSource.Wiki:
                return _regexWiki.Match(identifier).Groups[1].Value;
        }

        return null;
    }

    private List<IconSource> DetermineIconSources(string identifier)
    {
        List<IconSource> iconSources = new List<IconSource>();

        if (string.IsNullOrEmpty(identifier)) return iconSources;

        if (_regexRenderServiceSignatureFileIdPair.IsMatch(identifier))
        {
            iconSources.Add(IconSource.RenderAPI);
        }

        if (_regexWiki.IsMatch(identifier))
        {
            iconSources.Add(IconSource.Wiki);
        }

        if (_regexDat.IsMatch(identifier))
        {
            iconSources.Add(IconSource.DAT);
        }

        if (_regexModuleRef.IsMatch(identifier))
        {
            iconSources.Add(IconSource.Module);
        }

        if (_regexCore.IsMatch(identifier))
        {
            iconSources.Add(IconSource.Core);
        }

        if (iconSources.Count == 0)
        {
            iconSources.Add(IconSource.Unknown);
        }

        return iconSources;
    }

    public enum IconSource
    {
        Core,
        Module,
        RenderAPI,
        DAT,
        Wiki,
        Unknown
    }
}
