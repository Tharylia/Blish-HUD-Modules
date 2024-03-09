namespace Estreya.BlishHUD.Shared.Services;

using Blish_HUD.Modules.Managers;
using Extensions;
using Flurl.Http;
using Gw2Sharp.WebApi.V2;
using Gw2Sharp.WebApi.V2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class AchievementService : FilesystemAPIService<Achievement>
{
    public AchievementService(Gw2ApiManager apiManager, APIServiceConfiguration configuration, string baseFolderPath, IFlurlClient flurlClient, string fileRootUrl) : base(apiManager, configuration, baseFolderPath, flurlClient, fileRootUrl) { }
    protected override string BASE_FOLDER_STRUCTURE => "achievements";

    protected override string FILE_NAME => "achievements.json";

    public List<Achievement> Achievements => this.APIObjectList;

    protected override async Task<List<Achievement>> Fetch(Gw2ApiManager apiManager, IProgress<string> progress, CancellationToken cancellationToken)
    {
        progress.Report("Loading achievement ids...");
        IApiV2ObjectList<int> ids = await apiManager.Gw2ApiClient.V2.Achievements.IdsAsync(cancellationToken);

        progress.Report($"Loading {ids.Count} achievements...");

        IEnumerable<IEnumerable<int>> idChunks = ids.ChunkBy(200);
        int loadedCount = 0;
        List<Task<IReadOnlyList<Achievement>>> tasks = new List<Task<IReadOnlyList<Achievement>>>();

        foreach (IEnumerable<int> chunk in idChunks)
        {
            tasks.Add(apiManager.Gw2ApiClient.V2.Achievements.ManyAsync(chunk, cancellationToken).ContinueWith(t =>
            {
                int newCount = Interlocked.Add(ref loadedCount, chunk.Count());

                progress.Report($"Loading achievements... {newCount}/{ids.Count}");
                if (t.IsFaulted)
                {
                    this.Logger.Warn(t.Exception, $"Failed to load achievement chunk {chunk.First()} - {chunk.Last()}");
                    return new List<Achievement>();
                }

                return t.Result;
            }));
        }

        IReadOnlyList<Achievement>[] achievementLists = await Task.WhenAll(tasks);
        IEnumerable<Achievement> achievements = achievementLists.SelectMany(a => a);

        progress.Report("Finished");

        return achievements.ToList();
    }
}