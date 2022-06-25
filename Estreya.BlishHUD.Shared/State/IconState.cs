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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public class IconState : ManagedState
{
    private static readonly Logger Logger = Logger.GetLogger<IconState>();
    private const string FOLDER_NAME = "images";
    private static TimeSpan _saveInterval = TimeSpan.FromMinutes(2);

    private static readonly Regex _regexRenderServiceSignatureFileIdPair = new Regex("(.{40})\\/(\\d+)(?>\\..*)?$", RegexOptions.Compiled | RegexOptions.Singleline);

    private readonly ContentsManager _contentsManager;

    private readonly AsyncLock _textureLock = new AsyncLock();
    private readonly Dictionary<string, Texture2D> _loadedTextures = new Dictionary<string, Texture2D>();

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
            foreach (KeyValuePair<string, Texture2D> texture in this._loadedTextures)
            {
                texture.Value.Dispose();
            }

            this._loadedTextures.Clear();
        }
    }

    protected override void InternalUpdate(GameTime gameTime) { }

    protected override async Task Load()
    {
        await this.LoadImages();
    }

    protected override async Task Save()
    {
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
    }

    private async Task LoadImages()
    {
        Logger.Info("Load cached images from filesystem.");

        using (await this._textureLock.LockAsync())
        {
            this._loadedTextures.Clear();

            if (!Directory.Exists(this.Path))
            {
                return;
            }

            string[] filePaths = this.GetFiles();

            try
            {
                var loadTasks = filePaths.ToList().Select(filePath => Task.Run(() =>
                {
                    try
                    {
                        FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

                        if (fileStream.Length == 0)
                        {
                            Logger.Warn("Image is empty: {0}", filePath);
                            return;
                        }

                        AsyncTexture2D asyncTexture = new AsyncTexture2D(ContentService.Textures.Pixel);

                        GameService.Graphics.QueueMainThreadRender(device =>
                        {
                            Texture2D texture = TextureUtil.FromStreamPremultiplied(device, fileStream);
                            fileStream.Dispose();
                            asyncTexture.SwapTexture(texture);
                        });

                        string fileName = FileUtil.SanitizeFileName(System.IO.Path.GetFileNameWithoutExtension(filePath));
                        this.HandleAsyncTextureSwap(asyncTexture, fileName);
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(ex, "Failed preloading texture \"{0}\":", filePath);
                    }
                }));

                await Task.WhenAll(loadTasks);
            }catch(Exception ex)
            {
                Logger.Warn(ex, "Failed preloading textures:");
            }

            /*foreach (string filePath in filePaths)
            {
                try
                {
                    FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

                    if (fileStream.Length == 0)
                    {
                        Logger.Warn("Image is empty: {0}", filePath);
                        continue;
                    }

                    AsyncTexture2D asyncTexture = new AsyncTexture2D(ContentService.Textures.Pixel);

                    GameService.Graphics.QueueMainThreadRender(device =>
                    {
                        Texture2D texture = TextureUtil.FromStreamPremultiplied(device, fileStream);
                        fileStream.Dispose();
                        asyncTexture.SwapTexture(texture);
                    });

                    string fileName = FileUtil.SanitizeFileName(System.IO.Path.GetFileNameWithoutExtension(filePath));
                    this.HandleAsyncTextureSwap(asyncTexture, fileName);
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "Failed preloading texture \"{1}\": {0}", filePath);
                }
            }*/
        }

        return;
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

    public AsyncTexture2D GetIcon(string identifier, bool checkRenderAPI = true)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return null;
        }

        if (checkRenderAPI)
        {
            Match match = _regexRenderServiceSignatureFileIdPair.Match(identifier);
            if (match.Success)
            {
                string signature = match.Groups[1].Value;
                string fileId = match.Groups[2].Value;

                identifier = $"{signature}/{fileId}";
            }
        }

        string sanitizedIdentifier = FileUtil.SanitizeFileName(System.IO.Path.ChangeExtension(identifier, null));

        using (this._textureLock.Lock())
        {
            if (this._loadedTextures.ContainsKey(sanitizedIdentifier))
            {
                return this._loadedTextures[sanitizedIdentifier];
            }

            AsyncTexture2D icon = ContentService.Textures.Error;
            if (!string.IsNullOrWhiteSpace(identifier))
            {
                if (checkRenderAPI && identifier.Contains("/"))
                {
                    try
                    {
                        AsyncTexture2D asyncTexture = GameService.Content.GetRenderServiceTexture(identifier);
                        this.HandleAsyncTextureSwap(asyncTexture, sanitizedIdentifier);

                        icon = asyncTexture;
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(ex, "Could not load icon from render api:");
                    }
                }
                else
                {
                    try
                    {
                        // Load from module ref folder.
                        Texture2D texture = this._contentsManager.GetTexture(identifier);
                        if (texture == ContentService.Textures.Error)
                        {
                            // Load from base ref folder.
                            texture = GameService.Content.GetTexture(identifier);
                        }

                        icon = texture;
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(ex, "Could not load icon from ref folders:");
                    }
                }
            }

            this._loadedTextures.Add(sanitizedIdentifier, icon);

            return icon;
        }
    }

    public Task<AsyncTexture2D> GetIconAsync(string identifier, bool checkRenderAPI = true)
    {
        return Task.Run(() =>
        {
            return this.GetIcon(identifier, checkRenderAPI);
        });
    }

    public override Task Clear() => Task.CompletedTask;
}
