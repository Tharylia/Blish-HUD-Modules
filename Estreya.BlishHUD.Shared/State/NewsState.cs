namespace Estreya.BlishHUD.Shared.State
{
    using Estreya.BlishHUD.Shared.Models;
    using Flurl.Http;
    using Microsoft.Xna.Framework;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;

    public class NewsState : ManagedState
    {
        private const string FILE_NAME = "news.json";
        private readonly IFlurlClient _flurlClient;
        private readonly string _baseFilePath;

        public List<News> News { get; private set; }

        public NewsState(StateConfiguration configuration, IFlurlClient flurlClient, string baseFilePath) : base(configuration)
        {
            this._flurlClient = flurlClient;
            this._baseFilePath = baseFilePath;
        }

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

        protected override void InternalUpdate(GameTime gameTime) { }

        protected override async Task Load()
        {
            try
            {
                var newsJson = await _flurlClient.Request(_baseFilePath, FILE_NAME).GetStringAsync();
                var newsList = JsonConvert.DeserializeObject<List<News>>(newsJson);

                this.News.AddRange(newsList);
            }
            catch (Exception ex)
            {
                this.Logger.Debug(ex, "Failed to load news:");
            }
        }
    }
}
