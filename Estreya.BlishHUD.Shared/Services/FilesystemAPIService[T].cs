namespace Estreya.BlishHUD.Shared.Services;

using Blish_HUD;
using Blish_HUD.Modules.Managers;
using Flurl.Http;
using Gw2Sharp.Json.Converters;
using Gw2Sharp;
using IO;
using Json.Converter;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Utils;
using System.Threading;

public abstract class FilesystemAPIService<T> : APIService<T>
{
    private const string LAST_UPDATED_FILE_NAME = "last_updated.txt";

    private const string DATE_TIME_FORMAT = "yyyy-MM-ddTHH:mm:ss";

    private readonly string _baseModulePath;
    protected JsonSerializerSettings _serializerSettings;
    protected JsonSerializerOptions _gw2SharpSerializerOptions;
    protected IFlurlClient _flurlClient;
    protected string _fileRootUrl;

    protected FilesystemAPIService(Gw2ApiManager apiManager, APIServiceConfiguration configuration, string baseModulePath, IFlurlClient flurlClient, string fileRootUrl) : base(apiManager, configuration)
    {
        this.CreateJsonSettings();
        this._baseModulePath = baseModulePath;
        this._flurlClient = flurlClient;
        this._fileRootUrl = fileRootUrl;
    }

    protected abstract string BASE_FOLDER_STRUCTURE { get; }
    protected abstract string FILE_NAME { get; }

    protected string DirectoryPath => Path.Combine(this._baseModulePath, this.BASE_FOLDER_STRUCTURE);

    protected virtual bool ForceAPI => false;

    private void CreateJsonSettings()
    {
        this._serializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            Converters = new JsonConverter[]
            {
                new Json.Converter.RenderUrlConverter(GameService.Gw2WebApi.AnonymousConnection.Connection),
                new NullableRenderUrlConverter(GameService.Gw2WebApi.AnonymousConnection.Connection),
                new StringEnumConverter()
            }
        };

        this._gw2SharpSerializerOptions = new System.Text.Json.JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = Json.Converter.GW2Sharp.SnakeCaseNamingPolicy.SnakeCase
        };
        this._gw2SharpSerializerOptions.Converters.Add(new ApiEnumConverter());
        this._gw2SharpSerializerOptions.Converters.Add(new ApiFlagsConverter());
        this._gw2SharpSerializerOptions.Converters.Add(new ApiObjectConverter());
        this._gw2SharpSerializerOptions.Converters.Add(new ApiObjectListConverter());
        this._gw2SharpSerializerOptions.Converters.Add(new CastableTypeConverter());
        this._gw2SharpSerializerOptions.Converters.Add(new DictionaryIntKeyConverter());
        this._gw2SharpSerializerOptions.Converters.Add(new Gw2Sharp.Json.Converters.RenderUrlConverter(GameService.Gw2WebApi.AnonymousConnection.Connection, new Gw2Client(GameService.Gw2WebApi.AnonymousConnection.Connection)));
        this._gw2SharpSerializerOptions.Converters.Add(new TimeSpanConverter());
    }

    protected override async Task Load()
    {
        try
        {
            bool forceAPI = this.ForceAPI;

            if (forceAPI)
            {
                this.Logger.Debug("Force API is active.");
            }

            bool loadedFromStatic = false;

            if (!forceAPI)
            {
                try
                {
                    this.Loading = true;

                    this.ReportProgress("Loading static file content...");

                    IProgress<string> progress = new Progress<string>(this.ReportProgress);
                    var entities = await this.FetchFromStaticFile(progress, this.CancellationToken);

                    if (entities != null)
                    {
                        using (await this._apiObjectListLock.LockAsync())
                        {
                            this.APIObjectList.Clear();
                            this.APIObjectList.AddRange(entities);
                        }

                        this.SignalUpdated();

                        loadedFromStatic = true;
                    }
                }
                catch (Exception ex)
                {
                    this.Logger.Warn(ex, "Could not load from static file. Fallback to filesystem cache.");
                }
                finally
                {
                    this.Loading = false;
                    this.SignalCompletion(); // This clears the progress text as well
                }
            }

            bool canLoadFiles = !loadedFromStatic && !forceAPI && this.CanLoadFiles();
            bool shouldLoadFiles = !loadedFromStatic && !forceAPI && await this.ShouldLoadFiles();

            if (!loadedFromStatic && !forceAPI && canLoadFiles)
            {
                try
                {
                    this.Loading = true;

                    string filePath = Path.Combine(this.DirectoryPath, this.FILE_NAME);

                    bool handled = await this.OnBeforeFilesystemLoad(filePath);
                    if (handled)
                    {
                        return;
                    }

                    this.ReportProgress("Loading file content...");

                    using FileStream stream = FileUtil.ReadStream(filePath);

                    using ReadProgressStream progressStream = new ReadProgressStream(stream);
                    progressStream.ProgressChanged += (s, e) => this.ReportProgress($"Parsing json... {Math.Round(e.Progress, 0)}%");

                    Newtonsoft.Json.JsonSerializer serializer = Newtonsoft.Json.JsonSerializer.CreateDefault(this._serializerSettings);

                    using StreamReader sr = new StreamReader(progressStream);
                    using JsonReader reader = new JsonTextReader(sr);

                    List<T> entities = serializer.Deserialize<List<T>>(reader);

                    await this.OnAfterFilesystemLoad(entities);

                    using (await this._apiObjectListLock.LockAsync())
                    {
                        this.APIObjectList.Clear();
                        this.APIObjectList.AddRange(entities);
                    }

                    this.SignalUpdated();
                }
                catch (Exception ex)
                {
                    this.Logger.Warn(ex, "Could not load from filesystem. Fallback to API.");
                    forceAPI = true;
                }
                finally
                {
                    this.Loading = false;
                    this.SignalCompletion(); // This clears the progress text as well
                }
            }

            // Refresh files after we loaded the prior saved
            if (!loadedFromStatic && (forceAPI || !shouldLoadFiles))
            {
                var result = await this.LoadFromAPI(!canLoadFiles); // Only reset completion if we could not load anything at start
                if (!this.CancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        this.Loading = true;
                        this.ReportProgress("Saving...");

                        result |= await this.OnAfterLoadFromAPIBeforeSave();

                        if (result)
                        {
                            await this.Save();
                            await this.OnAfterLoadFromAPIAfterSave();
                        }
                    }
                    finally
                    {
                        this.Loading = false;
                    }
                }
            }

            this.Logger.Debug("Loaded {0} entities.", this.APIObjectList.Count);
        }
        catch (Exception ex)
        {
            this.Logger.Warn(ex, "Failed loading entites:");
        }
    }

    protected virtual Task<List<T>> FetchFromStaticFile(IProgress<string> progress, CancellationToken cancellationToken) => Task.FromResult<List<T>>(default);

    /// <summary>
    ///     Called before the filesystem load started.
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns>True, if the method handled the loading. Otherwise false.</returns>
    protected virtual Task<bool> OnBeforeFilesystemLoad(string filePath)
    {
        return Task.FromResult(false);
    }

    /// <summary>
    ///     Called after the filesystem load finished and before added to api object list.
    /// </summary>
    /// <param name="loadedEntites"></param>
    protected virtual Task OnAfterFilesystemLoad(List<T> loadedEntites)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Called after the load from api finished and before calling save.
    /// </summary>
    /// <returns>True, if the method handled additional loading before saving. Otherwise false.</returns>
    protected virtual Task<bool> OnAfterLoadFromAPIBeforeSave()
    {
        return Task.FromResult(false);
    }

    /// <summary>
    ///     Called after the load from api finished and after calling save.
    /// </summary>
    protected virtual Task OnAfterLoadFromAPIAfterSave()
    {
        return Task.CompletedTask;
    }

    private bool CanLoadFiles()
    {
        bool baseDirectoryExists = Directory.Exists(this.DirectoryPath);

        if (!baseDirectoryExists)
        {
            return false;
        }

        bool savedFileExists = File.Exists(Path.Combine(this.DirectoryPath, this.FILE_NAME));

        if (!savedFileExists)
        {
            return false;
        }

        return true;
    }

    private async Task<bool> ShouldLoadFiles()
    {
        if (!this.CanLoadFiles())
        {
            return false;
        }

        string lastUpdatedFilePath = Path.Combine(this.DirectoryPath, LAST_UPDATED_FILE_NAME);
        if (File.Exists(lastUpdatedFilePath))
        {
            string dateString = await FileUtil.ReadStringAsync(lastUpdatedFilePath);
            if (!DateTime.TryParseExact(dateString, DATE_TIME_FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime lastUpdated))
            {
                this.Logger.Debug("Failed parsing last updated.");
                return false;
            }

            return true;
        }

        return false;
    }

    protected override async Task Save()
    {
        _ = Directory.CreateDirectory(this.DirectoryPath);

        using (await this._apiObjectListLock.LockAsync())
        {
            string itemJson = JsonConvert.SerializeObject(this.APIObjectList, Formatting.Indented, this._serializerSettings);
            await FileUtil.WriteStringAsync(Path.Combine(this.DirectoryPath, this.FILE_NAME), itemJson);
        }

        await this.CreateLastUpdatedFile();
    }

    private async Task CreateLastUpdatedFile()
    {
        await FileUtil.WriteStringAsync(Path.Combine(this.DirectoryPath, LAST_UPDATED_FILE_NAME), DateTime.UtcNow.ToString(DATE_TIME_FORMAT));
    }

    protected override Task DoClear()
    {
        if (!Directory.Exists(this.DirectoryPath))
        {
            return Task.CompletedTask;
        }

        Directory.Delete(this.DirectoryPath, true);

        return Task.CompletedTask;
    }
}