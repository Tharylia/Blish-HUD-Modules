namespace Estreya.BlishHUD.Shared.Services
{
    using Blish_HUD;
    using Blish_HUD.Modules.Managers;
    using Estreya.BlishHUD.Shared.Extensions;
    using Estreya.BlishHUD.Shared.IO;
    using Estreya.BlishHUD.Shared.Json.Converter;
    using Estreya.BlishHUD.Shared.Utils;
    using Gw2Sharp.WebApi.V2.Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class AchievementService : FilesystemAPIService<Achievement>
    {
        protected override string BASE_FOLDER_STRUCTURE => "achievements";

        protected override string FILE_NAME => "achievements.json";

        public List<Achievement> Achievements => this.APIObjectList;

        public AchievementService(Gw2ApiManager apiManager, APIServiceConfiguration configuration, string baseFolderPath) : base(apiManager, configuration, baseFolderPath) { }

        protected override async Task<List<Achievement>> Fetch(Gw2ApiManager apiManager, IProgress<string> progress, CancellationToken cancellationToken)
        {
            progress.Report("Loading achievement ids...");
            var ids = await apiManager.Gw2ApiClient.V2.Achievements.IdsAsync(cancellationToken);

            progress.Report($"Loading {ids.Count} achievements...");

            var idChunks = ids.ChunkBy(200);
            var loadedCount = 0;
            var tasks = new List<Task<IReadOnlyList<Achievement>>>();

            foreach (var chunk in idChunks)
            {
                tasks.Add(apiManager.Gw2ApiClient.V2.Achievements.ManyAsync(chunk, cancellationToken).ContinueWith(t =>
                {
                    var newCount = Interlocked.Add(ref loadedCount, chunk.Count());

                    progress.Report($"Loading achievements... {newCount}/{ids.Count}");
                    if (t.IsFaulted)
                    {
                        this.Logger.Warn(t.Exception, $"Failed to load achievement chunk {chunk.First()} - {chunk.Last()}");
                        return new List<Achievement>();
                    }

                    return t.Result;
                }));
            }

            var achievementLists = await Task.WhenAll(tasks);
            var achievements = achievementLists.SelectMany(a => a);

            progress.Report($"Finished");

            return achievements.ToList();
        }
    }
}
