namespace Estreya.BlishHUD.Shared.State;

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

public class IconState : ManagedState
{
    private static readonly Logger Logger = Logger.GetLogger<IconState>();

    public const string RENDER_API_URL = "https://render.guildwars2.com/file/";
    public const string WIKI_URL = "https://wiki.guildwars2.com/images/";

    private const string FOLDER_NAME = "images";
    private static TimeSpan _saveInterval = TimeSpan.FromMinutes(2);

    private static readonly WebClient _webclient = new WebClient();

    private static readonly Regex _regexRenderServiceSignatureFileIdPair = new Regex("(.{40})\\/(\\d+)(?>\\..*)?$", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex _regexWiki = new Regex("(\\d{1}\\/\\d{1}\\w{1}\\/.+\\.png)", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex _regexDat = new Regex("(\\d+)\\.png", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex _regexModuleRef = new Regex("(.+\\.png)", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex _regexCore = new Regex("(\\d+)", RegexOptions.Compiled | RegexOptions.Singleline);

    private readonly ContentsManager _contentsManager;

    private readonly AsyncLock _textureLock = new AsyncLock();
    private readonly Dictionary<string, AsyncTexture2D> _loadedTextures = new Dictionary<string, AsyncTexture2D>();

    private string _basePath;
    private string _path;

    private string Path
    {
        get
        {
            if (this._path == null)
            {
                this._path = System.IO.Path.Combine(this._basePath, FOLDER_NAME);
            }

            return this._path;
        }
    }

    public IconState(ContentsManager contentsManager, string basePath) : base(true, (int)_saveInterval.TotalMilliseconds)
    {
        this._contentsManager = contentsManager;
        this._basePath = basePath;
    }

    protected override async Task InternalReload()
    {
        await this.Clear();
        await this.Load();
    }

    protected override Task Initialize() => Task.CompletedTask;

    protected override void InternalUnload()
    {
        AsyncHelper.RunSync(this.Save);

        using (this._textureLock.Lock())
        {
            foreach (KeyValuePair<string, AsyncTexture2D> texture in this._loadedTextures)
            {
                texture.Value.Dispose();
            }

            this._loadedTextures.Clear();
        }
    }

    protected override void InternalUpdate(GameTime gameTime) { }

    protected override Task Load()
    {
        return Task.CompletedTask;
        //await this.LoadImages();
    }

    protected override Task Save()
    {
        return Task.CompletedTask;
        /*
        Logger.Debug("Save loaded textures to filesystem.");

        if (!Directory.Exists(this.Path))
        {
            _ = Directory.CreateDirectory(this.Path);
        }

        using (await this._textureLock.LockAsync())
        {
            string[] currentLoadedTexturesArr = new string[this._loadedTextures.Keys.Count];

            this._loadedTextures.Keys.CopyTo(currentLoadedTexturesArr, 0);

            List<string> currentLoadedTextures = new List<string>(currentLoadedTexturesArr);

            string[] filePaths = this.GetFiles();

            foreach (string filePath in filePaths)
            {
                string sanitizedFileName = FileUtil.SanitizeFileName(System.IO.Path.GetFileNameWithoutExtension(filePath));
                if (currentLoadedTextures.Contains(sanitizedFileName))
                {
                    _ = currentLoadedTextures.Remove(sanitizedFileName);
                }
            }

            foreach (string newTextureIdentifier in currentLoadedTextures)
            {
                try
                {
                    string fileName = System.IO.Path.ChangeExtension(System.IO.Path.Combine(this.Path, newTextureIdentifier), "png");
                    using FileStream fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write);
                    Texture2D newTexture = this._loadedTextures[newTextureIdentifier];
                    if (newTexture == ContentService.Textures.Error)
                    {
                        Logger.Warn("Texture \"{0}\" is errorneous. Skipping saving.", newTextureIdentifier);
                        continue;
                    }

                    newTexture.SaveAsPng(fileStream, newTexture.Width, newTexture.Height);
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "Failed saving texture \"{1}\": {0}", newTextureIdentifier);
                }
            }
        }
        */
    }

    private Task LoadImages()
    {
        Logger.Info("Load cached images from filesystem.");

        using (this._textureLock.Lock())
        {
            this._loadedTextures.Clear();

            if (!Directory.Exists(this.Path))
            {
                return Task.CompletedTask;
            }

            string[] filePaths = this.GetFiles();

            try
            {
                foreach (string filePath in filePaths)
                {
                    try
                    {
                        FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

                        if (fileStream.Length == 0)
                        {
                            Logger.Warn("Image is empty: {0}", filePath);
                            fileStream.Dispose();
                            continue;
                        }

                        AsyncTexture2D asyncTexture = new AsyncTexture2D();

                        Texture2D texture = TextureUtil.FromStreamPremultiplied(fileStream);
                        fileStream.Dispose();

                        string fileName = FileUtil.SanitizeFileName(System.IO.Path.GetFileNameWithoutExtension(filePath));
                        this._loadedTextures.Add(fileName, asyncTexture);

                        asyncTexture.SwapTexture(texture);
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(ex, "Failed preloading texture \"{0}\":", filePath);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed preloading textures:");
            }
        }

        return Task.CompletedTask;
    }

    private void HandleAsyncTextureSwap(AsyncTexture2D asyncTexture2D, string identifier)
    {
        asyncTexture2D.TextureSwapped += (s, e) =>
        {
            using (this._textureLock.Lock())
            {
                this._loadedTextures[identifier] = e.NewValue;
            }

            Logger.Debug("Async texture \"{0}\" was swapped in cache.", identifier);
        };
    }

    private string[] GetFiles()
    {
        string[] filePaths = Directory.GetFiles(this.Path, "*.png");
        return filePaths;
    }

    public bool HasIcon(string identifier)
    {
        string sanitizedIdentifier = FileUtil.SanitizeFileName(identifier);

        return this._loadedTextures.ContainsKey(sanitizedIdentifier);
    }

    public AsyncTexture2D GetIcon(string identifier)
    {
        var iconSources = this.DetermineIconSources(identifier);
        return this.GetIcon(identifier, iconSources);
    }

    private AsyncTexture2D GetIcon(string identifier, List<IconSource> iconSources)
    {
        if (iconSources == null || iconSources.Count == 0) { return ContentService.Textures.Error; }

        using (this._textureLock.Lock())
        {
            AsyncTexture2D icon = null;
            //string sanitizedIdentifier = null;
            foreach (IconSource source in iconSources)
            {
                var sourceIdentifier = this.ParseIdentifierBySource(identifier, source);
                if (string.IsNullOrWhiteSpace(sourceIdentifier))
                {
                    Logger.Warn($"Can't load texture by {identifier} with source {source}");
                    continue;
                }

                /*
                sanitizedIdentifier = FileUtil.SanitizeFileName(System.IO.Path.ChangeExtension(sourceIdentifier, null));

                if (this._loadedTextures.ContainsKey(sanitizedIdentifier))
                {
                    return this._loadedTextures[sanitizedIdentifier];
                }
                */

                try
                {
                    switch (source)
                    {
                        case IconSource.Core:
                            Texture2D coreTexture = GameService.Content.GetTexture(sourceIdentifier);
                            if (coreTexture != ContentService.Textures.Error) icon = coreTexture;
                            break;
                        case IconSource.Module:
                            Texture2D moduleTexture = this._contentsManager.GetTexture(sourceIdentifier);
                            if (moduleTexture != ContentService.Textures.Error) icon = moduleTexture;
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

            if (icon == null) return ContentService.Textures.Error;

            //this.HandleAsyncTextureSwap(icon, sanitizedIdentifier);

            //this._loadedTextures.Add(sanitizedIdentifier, icon);
            return icon;
        }
    }

    public Task<AsyncTexture2D> GetIconAsync(string identifier, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            return this.GetIcon(identifier);
        }, cancellationToken);
    }

    public override Task Clear() => Task.CompletedTask;

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
