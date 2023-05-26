namespace Estreya.BlishHUD.Shared.Services;

using Flurl.Http;
using Microsoft.Xna.Framework;
using Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class NewsService : ManagedService
{
    private const string FILE_NAME = "news.json";
    private readonly string _baseFilePath;
    private readonly IFlurlClient _flurlClient;

    public NewsService(ServiceConfiguration configuration, IFlurlClient flurlClient, string baseFilePath) : base(configuration)
    {
        this._flurlClient = flurlClient;
        this._baseFilePath = baseFilePath;
    }

    public List<News> News { get; private set; }

    protected override Task Initialize()
    {
        this.News = new List<News>();
        return Task.CompletedTask;
    }

    protected override void InternalUnload()
    {
        this.News?.Clear();
        this.News = null;
    }

    protected override Task Clear()
    {
        this.News?.Clear();
        return Task.CompletedTask;
    }

    protected override void InternalUpdate(GameTime gameTime) { }

    protected override async Task Load()
    {
        try
        {
            string newsJson = await this._flurlClient.Request(this._baseFilePath, FILE_NAME).GetStringAsync();
            List<News> newsList = JsonConvert.DeserializeObject<List<News>>(newsJson);

            this.News.AddRange(newsList);
        }
        catch (Exception ex)
        {
            this.Logger.Debug(ex, "Failed to load news:");
        }
    }
}