namespace Estreya.BlishHUD.Shared.Services;

using Blish_HUD.Modules.Managers;
using Extensions;
using Flurl.Http;
using Gw2Sharp.WebApi.V2;
using Gw2Sharp.WebApi.V2.Models;
using IO;
using Microsoft.Xna.Framework;
using Models.GW2API.Skills;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Threading;
using Utils;
using File = System.IO.File;
using Skill = Models.GW2API.Skills.Skill;

public class SkillService : FilesystemAPIService<Skill>
{
    private const string MISSING_SKILLS_FILE_NAME = "missing_skills.json";
    private const string REMAPPED_SKILLS_FILE_NAME = "remapped_skills.json";

    private const string LOCAL_MISSING_SKILL_FILE_NAME = "missingSkills.json";
    private readonly AsyncRef<double> _lastSaveMissingSkill = new AsyncRef<double>(0);
    private readonly string _webRootUrl;
    private IFlurlClient _flurlClient;

    private IconService _iconService;

    private SynchronizedCollection<MissingArcDPSSkill> _missingSkillsFromAPIReportedByArcDPS;

    public SkillService(APIServiceConfiguration configuration, Gw2ApiManager apiManager, IconService iconService, string baseFolderPath, IFlurlClient flurlClient, string webRootUrl) : base(apiManager, configuration, baseFolderPath)
    {
        this._iconService = iconService;
        this._flurlClient = flurlClient;
        this._webRootUrl = new Uri(new Uri(webRootUrl), "/gw2/api/v2/skills").ToString();
    }

    protected override string BASE_FOLDER_STRUCTURE => "skills";
    protected override string FILE_NAME => "skills.json";

    public List<Skill> Skills => this.APIObjectList;

    public static Skill UnknownSkill { get; } = new Skill
    {
        Id = int.MaxValue,
        Name = "Unknown",
        Icon = "62248.png",
        Category = SkillCategory.Skill
    };

    protected override bool ForceAPI => true;

    protected override Task DoInitialize()
    {
        this._missingSkillsFromAPIReportedByArcDPS = new SynchronizedCollection<MissingArcDPSSkill>();
        return Task.CompletedTask;
    }

    protected override void DoUnload()
    {
        this._missingSkillsFromAPIReportedByArcDPS?.Clear();
        this._missingSkillsFromAPIReportedByArcDPS = null;
        this._iconService = null;
        this._flurlClient = null;
    }

    protected override async Task Load()
    {
        await this.LoadMissingSkills();
        await base.Load();
    }

    protected override async Task OnAfterFilesystemLoad(List<Skill> loadedEntitesFromFile)
    {
        this.ReportProgress("Adding missing skills...");
        await this.AddMissingSkills(loadedEntitesFromFile);

        this.ReportProgress("Remap skills...");
        await this.RemapSkillIds(loadedEntitesFromFile);

        this.ReportProgress("Loading skill icons...");
        this.LoadSkillIcons(loadedEntitesFromFile);
    }

    protected override async Task OnAfterLoadFromAPIAfterSave()
    {
        this.ReportProgress("Adding missing skills...");
        await this.AddMissingSkills(this.APIObjectList);

        this.ReportProgress("Remap skills...");
        await this.RemapSkillIds(this.APIObjectList);

        this.ReportProgress("Loading skill icons...");
        this.LoadSkillIcons(this.APIObjectList);

        this.SignalUpdated();
    }

    protected override async Task<List<Skill>> Fetch(Gw2ApiManager apiManager, IProgress<string> progress, CancellationToken cancellationToken)
    {
        progress.Report("Loading normal skills..");
        this.Logger.Debug("Loading normal skills..");

        IApiV2ObjectList<Gw2Sharp.WebApi.V2.Models.Skill> skillResponse = await apiManager.Gw2ApiClient.V2.Skills.AllAsync(cancellationToken);
        List<Skill> skills = skillResponse.Select(skill => Skill.FromAPISkill(skill)).ToList();

        this.Logger.Debug("Loaded normal skills..");

        progress.Report("Loading traits..");
        this.Logger.Debug("Loading traits..");

        IApiV2ObjectList<Trait> traitResponse = await apiManager.Gw2ApiClient.V2.Traits.AllAsync(cancellationToken);
        skills = skills.Concat(traitResponse.Select(trait => Skill.FromAPITrait(trait))).ToList();

        this.Logger.Debug("Loaded traits..");

        progress.Report("Loading trait skills..");
        this.Logger.Debug("Loading trait skills..");

        IEnumerable<TraitSkill> traitSkills = traitResponse.Where(trait => trait.Skills != null).SelectMany(trait => trait.Skills);
        skills = skills.Concat(traitSkills.Select(traitSkill => Skill.FromAPITraitSkill(traitSkill))).ToList();

        this.Logger.Debug("Loaded trait skills..");

        /*
        Logger.Debug("Loading item ids..");

        var itemIds = await apiManager.Gw2ApiClient.V2.Items.IdsAsync(this.CancellationTokenSource.Token);

        Logger.Debug($"Loaded item ids.. {itemIds.Count}");

        var itemIdChunks = itemIds.ChunkBy(200);

        Logger.Debug("Loading items..");

        var itemsMany = new List<IReadOnlyList<Gw2Sharp.WebApi.V2.Models.Item>>();

        
        foreach (var itemIdChunk in itemIdChunks)
        {
            var itemIdChunkList = itemIdChunk.ToList();
            if (itemIdChunkList.Count == 0) continue;

            try
            {
                itemsMany.Add(await apiManager.Gw2ApiClient.V2.Items.ManyAsync(itemIdChunkList, this.CancellationTokenSource.Token));

                Logger.Debug($"Loaded items {itemIdChunkList[0]} - {itemIdChunkList[itemIdChunkList.Count - 1]}");
            }
            catch (Exception ex)
            {
                Logger.Warn(ex,$"Could not load item ids {itemIdChunkList[0]} - {itemIdChunkList[itemIdChunkList.Count - 1]}:");
            }
        }

        Logger.Debug("Loaded items..");

        var items = itemsMany.SelectMany(item => item).ToList();
        var upgradeComponents = items.Where(item => item.Type == Gw2Sharp.WebApi.V2.Models.ItemType.UpgradeComponent).Select(item => item as Gw2Sharp.WebApi.V2.Models.ItemUpgradeComponent);

        var sigils = upgradeComponents.Where(upgradeComponent => upgradeComponent.Details?.Type == Gw2Sharp.WebApi.V2.Models.ItemUpgradeComponentType.Sigil);
        skills = skills.Concat(sigils.Select(sigil => Skill.FromAPIUpgradeComponent(sigil)).Where(skill => skill != null)).ToList();

        var runes = upgradeComponents.Where(upgradeComponent => upgradeComponent.Details?.Type == Gw2Sharp.WebApi.V2.Models.ItemUpgradeComponentType.Rune);
        //skills = skills.Concat(runes.Select(rune => Skill.FromAPIUpgradeComponent(rune)).Where(skill => skill != null)).ToList();
        //Sigils have ids but not resolvable against / skills
        // Runes have no ids to begin with..Mount skills are not resolveable against v2/ skills
        var mountResponse = await apiManager.Gw2ApiClient.V2.Mounts.Types.AllAsync();
        var mountSkills = mountResponse.SelectMany(mount => mount.Skills);
        skills = skills.Concat(mountSkills).ToList();
        */

        return skills.ToList();
    }

    private async Task RemapSkillIds(List<Skill> skills)
    {
        using Stream remappedSkillsStream = await this._flurlClient.Request(this._webRootUrl, REMAPPED_SKILLS_FILE_NAME).GetStreamAsync();
        using ReadProgressStream progressStream = new ReadProgressStream(remappedSkillsStream);
        progressStream.ProgressChanged += (s, e) => this.ReportProgress($"Reading remapped skills... {Math.Round(e.Progress, 0)}%");
        JsonSerializer serializer = JsonSerializer.CreateDefault(this._serializerSettings);
        using StreamReader sr = new StreamReader(progressStream);
        using JsonReader reader = new JsonTextReader(sr);
        List<RemappedSkillID> remappedSkills = serializer.Deserialize<List<RemappedSkillID>>(reader);

        foreach (RemappedSkillID remappedSkill in remappedSkills)
        {
            this.ReportProgress($"Remapping skill from {remappedSkill.OriginalID} to {remappedSkill.DestinationID} ({remappedSkill.Comment})");
            List<Skill> skillsToRemap = skills.Where(skill => skill.Id == remappedSkill.OriginalID).ToList();

            Skill skillToInsert = skills.FirstOrDefault(skill => skill.Id == remappedSkill.DestinationID);
            if (skillToInsert == null)
            {
                continue;
            }

            skillToInsert = skillToInsert.CopyWithJson(this._serializerSettings);

            //skillToInsert = skillToInsert.Copy(); // Serious performance problem
            skillToInsert.Id = remappedSkill.OriginalID;

            skillsToRemap.ForEach(skillToRemap => skills.Remove(skillToRemap));

            skills.Add(skillToInsert);

            this.Logger.Debug($"Remapped skill from {remappedSkill.OriginalID} to {remappedSkill.DestinationID} ({remappedSkill.Comment})");
        }
    }

    private async Task AddMissingSkills(List<Skill> skills)
    {
        Stream missingSkillsStream = await this._flurlClient.Request(this._webRootUrl, MISSING_SKILLS_FILE_NAME).GetStreamAsync();
        using ReadProgressStream progressStream = new ReadProgressStream(missingSkillsStream);
        progressStream.ProgressChanged += (s, e) => this.ReportProgress($"Reading missing skills... {Math.Round(e.Progress, 0)}%");
        JsonSerializer serializer = JsonSerializer.CreateDefault(this._serializerSettings);
        using StreamReader sr = new StreamReader(progressStream);
        using JsonReader reader = new JsonTextReader(sr);
        List<MissingSkill> missingSkills = serializer.Deserialize<List<MissingSkill>>(reader);

        foreach (MissingSkill missingSkill in missingSkills)
        {
            this.ReportProgress($"Adding missing skill {missingSkill.ID} ({missingSkill.Name}) with {missingSkill.NameAliases?.Length ?? 0} aliases.");

            skills.Add(new Skill
            {
                Id = missingSkill.ID,
                Name = missingSkill.Name,
                Icon = missingSkill.Icon
            });

            this.Logger.Debug($"Added missing skill {missingSkill.ID} ({missingSkill.Name})");

            if (missingSkill.NameAliases != null)
            {
                foreach (string alias in missingSkill.NameAliases)
                {
                    skills.Add(new Skill
                    {
                        Id = missingSkill.ID,
                        Name = alias,
                        Icon = missingSkill.Icon
                    });

                    this.Logger.Debug($"Added missing skill alias {missingSkill.ID} ({alias})");
                }
            }
        }
    }

    private void LoadSkillIcons(List<Skill> skills)
    {
        skills.ForEach(skill =>
        {
            skill.LoadTexture(this._iconService);
        });
    }

    protected override void DoUpdate(GameTime gameTime)
    {
        _ = UpdateUtil.UpdateAsync(this.SaveMissingSkills, gameTime, 60000, this._lastSaveMissingSkill, false);
    }

    private async Task SaveMissingSkills()
    {
        string missingSkillPath = Path.Combine(this.DirectoryPath, LOCAL_MISSING_SKILL_FILE_NAME);

        await FileUtil.WriteStringAsync(missingSkillPath, JsonConvert.SerializeObject(this._missingSkillsFromAPIReportedByArcDPS.OrderBy(skill => skill.ID), Formatting.Indented));
    }

    private async Task LoadMissingSkills()
    {
        string missingSkillPath = Path.Combine(this.DirectoryPath, LOCAL_MISSING_SKILL_FILE_NAME);
        if (File.Exists(missingSkillPath))
        {
            try
            {
                this._missingSkillsFromAPIReportedByArcDPS = JsonConvert.DeserializeObject<SynchronizedCollection<MissingArcDPSSkill>>(await FileUtil.ReadStringAsync(missingSkillPath));
            }
            catch (Exception)
            {
                // We dont care. Maybe schema changed
            }
        }
        else
        {
            this._missingSkillsFromAPIReportedByArcDPS = new SynchronizedCollection<MissingArcDPSSkill>();
        }
    }

    public Skill GetByName(string name)
    {
        if (this.Loading)
        {
            return null;
        }

        using (this._apiObjectListLock.Lock())
        {
            foreach (Skill skill in this.APIObjectList)
            {
                if (skill.Name == name)
                {
                    return skill;
                }
            }

            return null;
        }
    }

    public Skill GetBy(Predicate<Skill> predicate)
    {
        if (this.Loading)
        {
            return null;
        }

        using (this._apiObjectListLock.Lock())
        {
            return this.APIObjectList.Find(predicate);
        }
    }

    public Skill GetById(int id)
    {
        if (this.Loading)
        {
            return null;
        }

        //var t = this.APIObjectList.GroupBy(c => c.Id).Where(g => g.Skip(1).Any()).SelectMany(c => c).Select(x => $"{x.Id}: {x.Name} - {x.Category.ToString()}").ToList();

        using (this._apiObjectListLock.Lock())
        {
            foreach (Skill skill in this.APIObjectList)
            {
                if (skill.Id == id)
                {
                    return skill;
                }
            }

            return null;
        }
    }

    public bool AddMissingSkill(int id, string name)
    {
        if (this._missingSkillsFromAPIReportedByArcDPS.Any(m => m.ID == id))
        {
            return false;
        }

        int hintId = -1;
        using (this._apiObjectListLock.Lock())
        {
            foreach (Skill skill in this.APIObjectList)
            {
                if (skill.Name == name)
                {
                    hintId = skill.Id;
                    break;
                }
            }
        }

        this._missingSkillsFromAPIReportedByArcDPS?.Add(new MissingArcDPSSkill
        {
            ID = id,
            Name = name,
            HintID = hintId
        });

        return true;
    }

    public void RemoveMissingSkill(int id)
    {
        List<MissingArcDPSSkill> items = this._missingSkillsFromAPIReportedByArcDPS.Where(m => m.ID == id).ToList();
        if (!items.Any())
        {
            return;
        }

        foreach (MissingArcDPSSkill item in items)
        {
            _ = this._missingSkillsFromAPIReportedByArcDPS?.Remove(item);
        }
    }

    private struct MissingArcDPSSkill
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int HintID { get; set; }
    }
}