namespace Estreya.BlishHUD.Shared.State;

using Blish_HUD;
using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.Helpers;
using Estreya.BlishHUD.Shared.Models.GW2API.Skills;
using Estreya.BlishHUD.Shared.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class SkillState : APIState<Skill>
{
    private static readonly Logger Logger = Logger.GetLogger<SkillState>();
    private const string BASE_FOLDER_STRUCTURE = "skills";
    private const string FILE_NAME = "skills.txt";
    private const string LAST_UPDATED_FILE_NAME = "last_updated.txt";

    private const string DATE_TIME_FORMAT = "yyyy-MM-ddTHH:mm:ss";

    private IconState _iconState;
    private readonly string _baseFolderPath;

    private string FullPath => Path.Combine(this._baseFolderPath, BASE_FOLDER_STRUCTURE);

    public SkillState(Gw2ApiManager apiManager, IconState iconState, string baseFolderPath) : base(apiManager, updateInterval: Timeout.InfiniteTimeSpan, awaitLoad: false)
    {
        this._iconState = iconState;
        this._baseFolderPath = baseFolderPath;
    }

    protected override Task DoClear()
    {
        this._iconState = null;

        return Task.CompletedTask;
    }

    protected override void DoUnload() { }

    protected override async Task Load()
    {
        try
        {
            bool loadFromApi = false;

            if (Directory.Exists(this.FullPath))
            {
                bool continueLoadingFiles = true;

                string lastUpdatedFilePath = Path.Combine(this.FullPath, LAST_UPDATED_FILE_NAME);
                if (!System.IO.File.Exists(lastUpdatedFilePath))
                {
                    await this.CreateLastUpdatedFile();
                }

                string dateString = await FileUtil.ReadStringAsync(lastUpdatedFilePath);
                if (!DateTime.TryParseExact(dateString, DATE_TIME_FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime lastUpdated))
                {
                    Logger.Debug("Failed parsing last updated.");
                }
                else
                {
                    if (DateTime.UtcNow - new DateTime(lastUpdated.Ticks, DateTimeKind.Utc) > TimeSpan.FromDays(5))
                    {
                        continueLoadingFiles = false;
                        loadFromApi = true;
                    }
                }

                if (continueLoadingFiles)
                {
                    var filePath = Path.Combine(this.FullPath, FILE_NAME);
                    if (File.Exists(filePath))
                    {
                        var content = await FileUtil.ReadStringAsync(filePath);
                        var skills = JsonConvert.DeserializeObject<List<Skill>>(content);

                        using (await this._apiObjectListLock.LockAsync())
                        {
                            this.APIObjectList.AddRange(skills);
                        }

                        this._fetchTask = Task.CompletedTask;
                    }
                    else
                    {
                        loadFromApi = true;
                    }
                }
            }
            else
            {
                loadFromApi = true;
            }

            if (loadFromApi)
            {
                await base.Load();
                await this.Save();
            }

            Logger.Debug("Loaded {0} skills.", this.APIObjectList.Count);
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed loading skills:");
        }
    }

    protected override async Task<List<Skill>> Fetch(Gw2ApiManager apiManager)
    {
        var skillResponse = await apiManager.Gw2ApiClient.V2.Skills.AllAsync();

        var skills = skillResponse.Select(skill => Skill.FromAPISkill(skill)).ToList();

        var skillLoadTasks = skills.Select(skill => skill.LoadTexture(this._iconState));

        await Task.WhenAll(skillLoadTasks);

        return skills.ToList();
    }

    protected override async Task Save()
    {
        if (Directory.Exists(this.FullPath))
        {
            Directory.Delete(this.FullPath, true);
        }

        _ = Directory.CreateDirectory(this.FullPath);

        using (await this._apiObjectListLock.LockAsync())
        {
            await FileUtil.WriteStringAsync(Path.Combine(this.FullPath, FILE_NAME), JsonConvert.SerializeObject(this.APIObjectList));
        }

        await this.CreateLastUpdatedFile();
    }

    private async Task CreateLastUpdatedFile()
    {
        await FileUtil.WriteStringAsync(Path.Combine(this.FullPath, LAST_UPDATED_FILE_NAME), DateTime.UtcNow.ToString(DATE_TIME_FORMAT));
    }

    public Skill GetByName(string name)
    {
        using (this._apiObjectListLock.Lock())
        {
            IEnumerable<Skill> foundSkills = this.APIObjectList.Where(skill => skill.Name == name);

            return foundSkills.Any() ? foundSkills.First() : null;
        }
    }

    public Skill GetById(int id)
    {
        using (this._apiObjectListLock.Lock())
        {
            IEnumerable<Skill> foundSkills = this.APIObjectList.Where(skill => skill.Id == id);

            return foundSkills.Any() ? foundSkills.First() : null;
        }
    }
}
