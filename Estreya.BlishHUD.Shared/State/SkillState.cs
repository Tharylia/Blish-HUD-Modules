namespace Estreya.BlishHUD.Shared.State;

using Blish_HUD.Modules.Managers;
using Estreya.BlishHUD.Shared.Extensions;
using Estreya.BlishHUD.Shared.Models.GW2API.Skills;
using Estreya.BlishHUD.Shared.Modules;
using Estreya.BlishHUD.Shared.Settings;
using Estreya.BlishHUD.Shared.Threading;
using Estreya.BlishHUD.Shared.Utils;
using Flurl.Http;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

public class SkillState : APIState<Skill>
{
    private const string BASE_FOLDER_STRUCTURE = "skills";
    private const string FILE_NAME = "skills.json";

    private const string SKILL_FOLDER_NAME = "skills";
    private const string MISSING_SKILLS_FILE_NAME = "missing_skills.json";
    private const string REMAPPED_SKILLS_FILE_NAME = "remapped_skills.json";

    private const string LOCAL_MISSING_SKILL_FILE_NAME = "missingSkills.json";
    private const string LAST_UPDATED_FILE_NAME = "last_updated.txt";

    private const string DATE_TIME_FORMAT = "yyyy-MM-ddTHH:mm:ss";

    private IconState _iconState;
    private readonly string _baseFolderPath;
    private IFlurlClient _flurlClient;
    private readonly string _fileRootUrl;
    private AsyncRef<double> _lastSaveMissingSkill = new AsyncRef<double>(0);

    private string DirectoryPath => Path.Combine(this._baseFolderPath, BASE_FOLDER_STRUCTURE);

    public static Skill UnknownSkill { get; } = new Skill()
    {
        Id = int.MaxValue,
        Name = "Unknown",
        Icon = "62248.png",
        Category = SkillCategory.Skill,
    };

    private static readonly Dictionary<int, int> _remappedSkillIds = new Dictionary<int, int>()
    {
        { 43485, 42297 }, // Manifest Sand Shade
        { 46808, 42297 }, // Manifest Sand Shade
        { 46821, 42297 }, // Manifest Sand Shade
        { 46824, 42297 }, // Manifest Sand Shade
        { 59601, 1947 }, // Big Boomer
        { 49084, 1877 }, // Impact Savant
        { 54935, 29921 }, // Shredder Gyro
        { 30176, 29921 }, // Shredder Gyro
        { 1377, 1512 }, // Shoot
        { 42366, 2159 }, // Loremaster
        { 42984, 2101 }, // Liberator's Vow
        { 9113, 9118 }, // Virtue of Courage
        { 9114, 9115 }, // Virtue of Justice
        { 9119, 9120 }, // Virtue of Resolve
        { 17047, 9120 }, // Virtue of Resolve
        { 42639, 2063 }, // Weighty Terms
        { 1623, 2871 }, // Jab
        { 45895, 2089 }, // Purity of Word
        { 2672, 5683 }, // Unsteady Ground
        { 42133, 2643}, // Tail Spin, Raptor Skill 1
        { 13655, 582 }, // Valorous Protection
        { 30207, 1899 }, // Invigorated Bulwark
        { 30235, 58090 }, // Med Blaster
        { 13515, 1916 }, // Medical Dispersion Field
        { 5586, 5493 }, // Water Attunement
        { 5580, 5495}, // Earth Attunement
        { 5585, 5492  }, // Fire Attunement
        { 5575, 5494 }, // Air Attunement
        { 13133, 13132 }, // Basilisk Venom
        { 68121, 68079 }, // Rifle Burst Grenade
        { 59579, 59562 }, // Explosive Entrance
        { 63264, 63348}, // Jade Energy Shot
        { 48894, 1808}, // Expose Defenses
        { 10243, 10192 }, // Distortion
        { 62592, 62597 }, // Bladeturn Requiem?
    };

    private static readonly Dictionary<int, (string Name, string RenderUrl)> _missingSkills = new Dictionary<int, (string Name, string RenderUrl)>()
    {
        { 717, ("Protection", "CD77D1FAB7B270223538A8F8ECDA1CFB044D65F4/102834" ) }, // Protection
	    { 718, ("Regeneration", "F69996772B9E18FD18AD0AABAB25D7E3FC42F261/102835" ) }, //Regeneration
        { 719, ("Swiftness", "20CFC14967E67F7A3FD4A4B8722B4CF5B8565E11/102836" ) }, // Swiftness
        { 720, ("Blinded", "102837.png") },  // Blinded
	    { 723, ("Poison", "559B0AF9FB5E1243D2649FAAE660CCB338AACC19/102840"  )}, //Poison
        { 725, ("Fury", "96D90DF84CAFE008233DD1C2606A12C1A0E68048/102842" ) }, // Fury
        { 726, ("Vigor", "58E92EBAF0DB4DA7C4AC04D9B22BCA5ECF0100DE/102843" ) }, // Vigor
        { 727, ("Immobile", "102844.png")}, // Immobile
        { 736, ("Bleeding", "79FF0046A5F9ADA3B4C4EC19ADB4CB124D5F0021/102848") }, //Bleeding
	    { 737, ("Burning", "B47BF5803FED2718D7474EAF9617629AD068EE10/102849"  )}, //Burning
        { 740, ("Might", "2FA9DF9D6BC17839BBEA14723F1C53D645DDB5E1/102852" ) }, // Might
        { 742, ("Weakness", "102853.png") }, // Weakness
        { 743, ("Aegis", "DFB4D1B50AE4D6A275B349E15B179261EE3EB0AF/102854" ) }, // Aegis
        { 762, ("Determined", "102763.png") }, // Determined
        { 770, ("Downed", "102763.png") }, // Downed
        { 791, ("Fear", "102869.png") }, // Fear
        { 833, ("Daze", "433474.png") }, // Daze
	    { 861, ("Confusion", "289AA0A4644F0E044DED3D3F39CED958E1DDFF53/102880"  )}, //Confusion
        { 872, ("Stun", "522727.png") }, // Stun
        { 873, ("Resolution", "D104A6B9344A2E2096424A3C300E46BC2926E4D7/2440718" ) }, // Resolution
        //{ 873, ("Retaliation", "27F233F7D4CE4E9EFE040E3D665B7B0643557B6E/102883"  )}, //Retaliation (old, replaced by Resolution)
        { 890, ("Revealed", "102887.png") }, // Revealed
        { 910, ("Poisoned", "102840.png") }, // Poisoned
        { 1066, ("Resurrect", "2261500.png" ) }, // Resurrect
        { 1122, ("Stability", "3D3A1C2D6D791C05179AB871902D28782C65C244/415959" ) }, // Stability
        { 1187, ("Quickness", "D4AB6401A6D6917C3D4F230764452BCCE1035B0D/1012835" ) }, // Quickness
        { 5974, ("Superspeed", "103458.png") }, // Superspeed
        { 13017, ("Stealth", "62777.png") }, // Stealth
        { 2643, ("Tail Spin", "1770527.png") }, // Raptor Skill 1
        //{000, ("Leap", "2278468.png") }, // Raptor Skill 5
        { 41993, ("Cannonball", "1770525.png") }, // Springer Skill 1
        //{000, ("Rocket Jump", "2278482.png") }, // Springer Skill 5
	    { 19426, ("Torment", "10BABF2708CA3575730AC662A2E72EC292565B08/598887")  }, //Torment
	    { 17495,("Regeneration", "F69996772B9E18FD18AD0AABAB25D7E3FC42F261/102835" ) }, //Regeneration
	    { 17674, ("Regeneration", "F69996772B9E18FD18AD0AABAB25D7E3FC42F261/102835" ) }, //Regeneration
        { 23276, ("Breakbar Change", "433474.png") }, // Breakbar Change
        { 26766, ("Slow","961397.png" ) }, //Slow
        { 26980, ("Resistance", "50BAC1B8E10CFAB9E749A5D910D4A9DCF29EBB7C/961398" ) }, // Resistance
        { 30328, ("Alacrity", "4FDAC2113B500104121753EF7E026E45C141E94D/1938787" ) }, // Alacrity
        { 40530, ("Tome of Justice", "2710AF269B38A4A365089BC7B3C9389B354DE59D/1770472") }, // Guardian Tome 1
        { 41258, ("Chapter 1: Searing Spell","1770473.png") }, // Guardian Tome 1, Skill 1
        { 40635, ("Chapter 2: Igniting Burst","1770474.png") }, // Guardian Tome 1, Skill 2
        { 42449, ("Chapter 3: Heated Rebuke", "1770475.png") }, // Guardian Tome 1, Skill 3
        { 40015, ("Chapter 4: Scorched Aftermath","1770476.png") }, // Guardian Tome 1, Skill 4
        { 41957, ("Epilogue: Ashes of the Just", "1770477.png") }, // Guardian Tome 1, Skill 5
        { 46298, ("Tome of Resolve", "E206770FD62BB63B71F56209F34BF99392BADF9E/1770478") }, // Guardian Tome 2
        { 40787, ("Chapter 1: Desert Bloom", "1770479.png") }, // Guardian Tome 2, Skill 1
        { 40679, ("Chapter 2: Radiant Recovery", "1770480.png") }, // Guardian Tome 2, Skill 2
        { 45128, ("Chapter 3: Azure Sun", "1770481.png") }, // Guardian Tome 2, Skill 3
        { 42008, ("Chapter 4: Shining River", "1770482.png") }, // Guardian Tome 2, Skill 4
        { 44871, ("Epilogue: Eternal Oasis", "1770483.png") }, // Guardian Tome 2, Skill 5
        { 43508, ("Tome of Courage", "49B3D2E829962602205B09619770E1650BF07108/1770466") }, // Guardian Tome 3
        //{ 999999903, ("Chapter 1: Unflinching Charge", "1770467.png") }, // Guardian Tome 3, Skill 1
        { 41968, ("Chapter 2: Daring Challenge","1770468.png") }, // Guardian Tome 3, Skill 2
        { 41836, ("Chapter 3: Valiant Bulwark", "1770469.png") }, // Guardian Tome 3, Skill 3
        //{ 999999909, ("Chapter 4: Stalwart Stand", "1770470.png") }, // Guardian Tome 3, Skill 4
        { 43194, ("Epilogue: Unbroken Lines", "1770471.png") }, // Guardian Tome 3, Skill 5
        { 53285, ("Rune of the Monk", "220738.png") }, // Rune of the Monk
        { 63348, ("Jade Energy Shot","103434.png") }, // Jade Energy Shot
        { 1656, ("Whirling Assault","102998.png") }, // Whirling Assault
        { 33611, ("Leader of the Pack III","66520.png") }, // Leader of the Pack III
        { 62554,("Cutter Burst","2479385.png") }, // Cutter Burst
    };

    private ConcurrentDictionary<int, string> _missingSkillsFromAPIReportedByArcDPS;

    public SkillState(APIStateConfiguration configuration, Gw2ApiManager apiManager, IconState iconState, string baseFolderPath, IFlurlClient flurlClient, string fileRootUrl) : base(apiManager, configuration)
    {
        this._iconState = iconState;
        this._baseFolderPath = baseFolderPath;
        this._flurlClient = flurlClient;
        this._fileRootUrl = fileRootUrl;
    }

    protected override Task DoInitialize()
    {
        this._missingSkillsFromAPIReportedByArcDPS = new ConcurrentDictionary<int, string>();
        return Task.CompletedTask;
    }

    protected override void DoUnload()
    {
        this._missingSkillsFromAPIReportedByArcDPS?.Clear();
        this._missingSkillsFromAPIReportedByArcDPS = null;
        this._iconState = null;
        this._flurlClient = null;
    }

    protected override async Task Load()
    {
        try
        {
            // TEST
            await this.LoadMissingSkills();
            // TEST

            //List<MissingSkill> ms = new List<MissingSkill>();
                 
            //foreach (var missingSkill in _missingSkills)
            //{
            //    ms.Add(new MissingSkill()
            //    {
            //        ID = missingSkill.Key,
            //        Name = missingSkill.Value.Name,
            //        Icon = missingSkill.Value.RenderUrl
            //    });
            //}

            //await FileUtil.WriteStringAsync(@"C:\temp\missingskills.json", JsonConvert.SerializeObject(ms, Formatting.Indented));

            //List<RemappedSkillID> rs = new List<RemappedSkillID>();

            //foreach (var remappedSkill in _remappedSkillIds)
            //{
            //    rs.Add(new RemappedSkillID()
            //    {
            //        OriginalID = remappedSkill.Key,
            //        DestinationID = remappedSkill.Value
            //    });
            //}

            //await FileUtil.WriteStringAsync(@"C:\temp\remapped_skills.json", JsonConvert.SerializeObject(rs, Formatting.Indented));

            await UnknownSkill.LoadTexture(this._iconState, this._cancellationTokenSource.Token);

            bool shouldLoadFiles = await this.ShouldLoadFiles();

            if (!shouldLoadFiles)
            {
                await base.Load();
                await this.Save();

                await this.AddMissingSkills(this.APIObjectList);
                await this.RemapSkillIds(this.APIObjectList);

                Logger.Debug("Loading skill icons..");

                await this.LoadSkillIcons(this.APIObjectList);

                Logger.Debug("Loaded skill icons..");
            }
            else
            {
                try
                {
                    this.Loading = true;

                    string filePath = Path.Combine(this.DirectoryPath, FILE_NAME);
                    string content = await FileUtil.ReadStringAsync(filePath);
                    List<Skill> skills = JsonConvert.DeserializeObject<List<Skill>>(content);

                    await this.AddMissingSkills(skills);

                    await this.RemapSkillIds(skills);

                    await this.LoadSkillIcons(skills);

                    using (await this._apiObjectListLock.LockAsync())
                    {
                        this.APIObjectList.AddRange(skills);
                    }
                }
                finally
                {
                    this.Loading = false;
                    this.SignalCompletion();
                }
            }

            this.Logger.Debug("Loaded {0} skills.", this.APIObjectList.Count);
        }
        catch (Exception ex)
        {
            this.Logger.Warn(ex, "Failed loading skills:");
        }
    }

    private async Task<bool> ShouldLoadFiles()
    {
        var baseDirectoryExists = Directory.Exists(this.DirectoryPath);

        if (!baseDirectoryExists) return false;

        var skillFileExists = File.Exists(Path.Combine(this.DirectoryPath, FILE_NAME));

        if (!skillFileExists) return false;

        string lastUpdatedFilePath = Path.Combine(this.DirectoryPath, LAST_UPDATED_FILE_NAME);
        if (System.IO.File.Exists(lastUpdatedFilePath))
        {
            string dateString = await FileUtil.ReadStringAsync(lastUpdatedFilePath);
            if (!DateTime.TryParseExact(dateString, DATE_TIME_FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime lastUpdated))
            {
                this.Logger.Debug("Failed parsing last updated.");
                return false;
            }
            else
            {
                return DateTime.UtcNow - new DateTime(lastUpdated.Ticks, DateTimeKind.Utc) <= TimeSpan.FromDays(5);
            }
        }

        return false;
    }

    protected override async Task<List<Skill>> Fetch(Gw2ApiManager apiManager, IProgress<string> progress)
    {
        progress.Report("Loading normal skills..");
        Logger.Debug("Loading normal skills..");

        Gw2Sharp.WebApi.V2.IApiV2ObjectList<Gw2Sharp.WebApi.V2.Models.Skill> skillResponse = await apiManager.Gw2ApiClient.V2.Skills.AllAsync(this._cancellationTokenSource.Token);
        List<Skill> skills = skillResponse.Select(skill => Skill.FromAPISkill(skill)).ToList();

        Logger.Debug("Loaded normal skills..");

        progress.Report("Loading traits..");
        Logger.Debug("Loading traits..");

        Gw2Sharp.WebApi.V2.IApiV2ObjectList<Gw2Sharp.WebApi.V2.Models.Trait> traitResponse = await apiManager.Gw2ApiClient.V2.Traits.AllAsync(this._cancellationTokenSource.Token);
        skills = skills.Concat(traitResponse.Select(trait => Skill.FromAPITrait(trait))).ToList();

        Logger.Debug("Loaded traits..");

        progress.Report("Loading trait skills..");
        Logger.Debug("Loading trait skills..");

        IEnumerable<Gw2Sharp.WebApi.V2.Models.TraitSkill> traitSkills = traitResponse.Where(trait => trait.Skills != null).SelectMany(trait => trait.Skills);
        skills = skills.Concat(traitSkills.Select(traitSkill => Skill.FromAPITraitSkill(traitSkill))).ToList();

        Logger.Debug("Loaded trait skills..");

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
        skills = skills.Concat(runes.Select(rune => Skill.FromAPIUpgradeComponent(rune)).Where(skill => skill != null)).ToList();
        */
        // Sigils have ids but not resolvable against /skills
        // Runes have no ids to begin with..

        // Mount skills are not resolveable against v2/skills
        //var mountResponse = await apiManager.Gw2ApiClient.V2.Mounts.Types.AllAsync();
        //var mountSkills = mountResponse.SelectMany(mount => mount.Skills);
        //skills = skills.Concat(mountSkills).ToList();

        //progress.Report("Adding missing skills..");
        //Logger.Debug("Adding missing skills..");

        //this.AddMissingSkills(skills);

        //Logger.Debug("Added missing skills..");

        //progress.Report("Remapping skills..");
        //Logger.Debug("Remapping skills..");

        //this.RemapSkillIds(skills);

        //Logger.Debug("Remapped skills..");

        //progress.Report("Loading skill icons..");
        //Logger.Debug("Loading skill icons..");

        //await this.LoadSkillIcons(skills);

        //Logger.Debug("Loaded skill icons..");

        return skills.ToList();
    }

    private async Task RemapSkillIds(List<Skill> skills)
    {
        var remappedSkillsJson = await this._flurlClient.Request(_fileRootUrl, SKILL_FOLDER_NAME, REMAPPED_SKILLS_FILE_NAME).GetStringAsync();
        var remappedSkills = JsonConvert.DeserializeObject<List<RemappedSkillID>>(remappedSkillsJson);

        foreach (var remappedSkill in remappedSkills)
        {
            List<Skill> skillsToRemap = skills.Where(skill => skill.Id == remappedSkill.OriginalID).ToList();

            Skill skillToInsert = skills.FirstOrDefault(skill => skill.Id == remappedSkill.DestinationID);
            if (skillToInsert == null)
            {
                continue;
            }

            skillToInsert = skillToInsert.Copy();
            skillToInsert.Id = remappedSkill.OriginalID;

            skillsToRemap.ForEach(skillToRemap => skills.Remove(skillToRemap));

            skills.Add(skillToInsert);

            Logger.Debug($"Remapped skill from {remappedSkill.OriginalID} to {remappedSkill.DestinationID}");
        }
    }

    private async Task AddMissingSkills(List<Skill> skills)
    {
        var missingSkillsJson = await this._flurlClient.Request(this._fileRootUrl, SKILL_FOLDER_NAME, MISSING_SKILLS_FILE_NAME).GetStringAsync();
        var missingSkills = JsonConvert.DeserializeObject<List<MissingSkill>>(missingSkillsJson);

        foreach (var missingSkill in missingSkills)
        {
            if (!skills.Exists(skill => skill.Id == missingSkill.ID))
            {
                skills.Add(new Skill()
                {
                    Id = missingSkill.ID,
                    Name = missingSkill.Name,
                    Icon = missingSkill.Icon
                });
            }
        }
    }

    private async Task LoadSkillIcons(List<Skill> skills)
    {
        IEnumerable<Task> skillLoadTasks = skills.Select(skill =>
        {
            return skill.LoadTexture(this._iconState, this._cancellationTokenSource.Token);
        });

        await Task.WhenAll(skillLoadTasks);
    }

    protected override async Task Save()
    {
        if (Directory.Exists(this.DirectoryPath))
        {
            Directory.Delete(this.DirectoryPath, true);
        }

        _ = Directory.CreateDirectory(this.DirectoryPath);

        using (await this._apiObjectListLock.LockAsync())
        {
            await FileUtil.WriteStringAsync(Path.Combine(this.DirectoryPath, FILE_NAME), JsonConvert.SerializeObject(this.APIObjectList));
        }

        await this.CreateLastUpdatedFile();
    }

    protected override void DoUpdate(GameTime gameTime)
    {
        _ = UpdateUtil.UpdateAsync(this.SaveMissingSkills, gameTime, 60000, this._lastSaveMissingSkill);
    }

    private async Task SaveMissingSkills()
    {
        string missingSkillPath = Path.Combine(this.DirectoryPath, LOCAL_MISSING_SKILL_FILE_NAME);

        await FileUtil.WriteStringAsync(missingSkillPath, JsonConvert.SerializeObject(this._missingSkillsFromAPIReportedByArcDPS.OrderBy(skill => skill.Value).ToDictionary(skill => skill.Key, skill => skill.Value), Formatting.Indented));
    }

    private async Task LoadMissingSkills()
    {
        string missingSkillPath = Path.Combine(this.DirectoryPath, LOCAL_MISSING_SKILL_FILE_NAME);
        if (File.Exists(missingSkillPath))
        {
            this._missingSkillsFromAPIReportedByArcDPS = JsonConvert.DeserializeObject<ConcurrentDictionary<int, string>>(await FileUtil.ReadStringAsync(missingSkillPath));
        }
        else
        {
            this._missingSkillsFromAPIReportedByArcDPS = new ConcurrentDictionary<int, string>();
        }
    }

    private async Task CreateLastUpdatedFile()
    {
        await FileUtil.WriteStringAsync(Path.Combine(this.DirectoryPath, LAST_UPDATED_FILE_NAME), DateTime.UtcNow.ToString(DATE_TIME_FORMAT));
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

    public Skill GetById(int id)
    {
        if (this.Loading)
        {
            return null;
        }

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
        return this._missingSkillsFromAPIReportedByArcDPS?.TryAdd(id, name) ?? false;
    }

    public void RemoveMissingSkill(int id)
    {
        _ = this._missingSkillsFromAPIReportedByArcDPS?.TryRemove(id, out string _);
    }
}
