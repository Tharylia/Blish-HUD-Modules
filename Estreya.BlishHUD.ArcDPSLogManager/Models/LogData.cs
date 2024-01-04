namespace Estreya.BlishHUD.ArcDPSLogManager.Models;

using Estreya.BlishHUD.ArcDPSLogManager.Models.Enums;
using Estreya.BlishHUD.ArcDPSLogManager.Processing;
using GW2EIEvtcParser;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

public class LogData
{
    private const string UnknownMainTargetName = "Unknown";
    private DateTimeOffset? encounterStartTime;
    private DateTimeOffset? encounterEndTime;
    private FileInfo fileInfo;
    private string filePath;

    [JsonIgnore]
    internal WeakReference<ParsedEvtcLog> _parsedEvtcLog;

    /// <summary>
    /// The <see cref="FileInfo"/> of the corresponding log file.
    /// This will create a new <see cref="System.IO.FileInfo"/> if it doesn't exist and can throw every exception that <see cref="System.IO.FileInfo"/> can throw.
    /// </summary>
    [JsonIgnore]
    public FileInfo FileInfo
    {
        get
        {
            this.fileInfo ??= new FileInfo(this.FilePath);

            return this.fileInfo;
        }
    }

    /// <summary>
    /// The name of the corresponding log file.
    /// </summary>
    [JsonProperty("filePath")]
    public string FilePath
    {
        get => this.filePath;
        set
        {
            this.fileInfo = null;
            this.filePath = value;
        }
    }

    /// <summary>
    /// The players participating in the encounter recorded in this log.
    /// </summary>
    [JsonProperty("players")]
    public IEnumerable<LogPlayer> Players { get; set; }

    /// <summary>
    /// The result of the encounter recorded in this log.
    /// </summary>
    [JsonProperty("encounterResult")]
    public EncounterResult EncounterResult { get; set; } = EncounterResult.Unknown;

    /// <summary>
    /// The mode of the encounter recorded in this log.
    /// </summary>
    [JsonProperty("encounterMode")]
    public EncounterMode EncounterMode { get; set; } = EncounterMode.Unknown;

    /// <summary>
    /// The <see cref="Encounter"/> that is recorded in this log.
    /// </summary>
    //[JsonProperty]
    //public Encounter Encounter { get; set; } = Encounter.Other;

    /// <summary>
    /// The ID of the game map this log was recorded on.
    /// </summary>
    [JsonProperty("mapId")]
    public int? MapId { get; set; }

    /// <summary>
    /// The author of the log.
    /// </summary>
    [JsonProperty("pointOfView")]
    public PointOfView PointOfView { get; set; }

    /// <summary>
    /// The game version (build) used to generate this log.
    /// </summary>
    [JsonProperty("gameBuild")]
    public ulong GameBuild { get; set; }

    /// <summary>
    /// The version of arcdps used to generate this log.
    /// </summary>
    [JsonProperty("arcdpsVersion")]
    public string ArcDPSVersion { get; set; }

    /// <summary>
    /// The version of evtc used to generate this log.
    /// </summary>
    [JsonProperty("evtcVersion")]
    public long EvtcVersion { get; set; }

    /// <summary>
    /// The language of the game used to generate the log.
    /// </summary>
    [JsonProperty("gameLanguage")]
    public GameLanguage GameLanguage { get; set; }

    /// <summary>
    /// The name of the main target of the encounter.
    /// </summary>
    [JsonProperty("mainTargetName")]
    public string MainTargetName { get; set; } = UnknownMainTargetName;

    /// <summary>
    /// The health percentage of the target of the encounter. If there are multiple targets,
    /// the highest percentage is provided.
    /// </summary>
    [JsonProperty("healthPercentage")]
    public double HealthPercentage { get; set; }

    /// <summary>
    /// Time when the encounter started.
    /// Is only an estimate if <see cref="IsEncounterStartTimePrecise"/> is false.
    /// If it's an estimate it will create a <see cref="System.IO.FileInfo"/>.
    /// If the creation of <see cref="System.IO.FileInfo"/> fails it will return the default of <see cref="DateTimeOffset"/>.
    /// </summary>
    [JsonProperty("encounterStartTime")]
    public DateTimeOffset? EncounterStartTime
    {
        get
        {
            if (this.encounterStartTime == default && !this.IsEncounterStartTimePrecise)
            {
                try
                {
                    this.encounterStartTime = this.FileInfo.CreationTime;
                }
                catch
                {
                    this.encounterStartTime = default;
                }
            }

            return this.encounterStartTime;
        }

        set => this.encounterStartTime = value;
    }

    /// <summary>
    /// Time when the encounter started.
    /// Is only an estimate if <see cref="IsEncounterEndTimePrecise"/> is false.
    /// If it's an estimate it will create a <see cref="System.IO.FileInfo"/>.
    /// If the creation of <see cref="System.IO.FileInfo"/> fails it will return the default of <see cref="DateTimeOffset"/>.
    /// </summary>
    [JsonProperty("encounterEndTime")]
    public DateTimeOffset? EncounterEndTime
    {
        get
        {
            if (this.encounterEndTime == default && !this.IsEncounterEndTimePrecise)
            {
                try
                {
                    this.encounterEndTime = this.FileInfo.CreationTime;
                }
                catch
                {
                    this.encounterEndTime = default;
                }
            }

            return this.encounterEndTime;
        }

        set => this.encounterEndTime = value;
    }

    /// <summary>
    /// The duration of the encounter.
    /// </summary>
    [JsonProperty("encounterDuration")]
    public TimeSpan EncounterDuration { get; set; }

    /// <summary>
    /// The upload status for uploads to dps.report, using Elite Insights on dps.report.
    /// </summary>
    //[JsonProperty]
    //public LogUpload DpsReportEIUpload { get; set; } = new LogUpload();

    /// <summary>
    /// The current status of the data processing.
    /// </summary>
    [JsonProperty("parsingStatus")]
    public ParsingStatus ParsingStatus { get; set; } = ParsingStatus.Unparsed;

    /// <summary>
    /// Contains the time of when parsing of the log was finished, will be default unless <see cref="ParsingStatus"/> is <see cref="Logs.ParsingStatus.Parsed"/>
    /// </summary>
    [JsonProperty("parseTime")]
    public DateTimeOffset ParseTime { get; set; }

    /// <summary>
    /// The amount of milliseconds the parsing of the log took or -1 if <see cref="ParsingStatus"/> is not <see cref="Logs.ParsingStatus.Parsed"/>
    /// </summary>
    [JsonProperty("parseMilliseconds")]
    public long ParseMilliseconds { get; set; } = -1;

    /// <summary>
    /// An exception if one was thrown during parsing. Will be null unless <see cref="ParsingStatus"/> is <see cref="Logs.ParsingStatus.Failed"/>.
    /// </summary>
    [JsonProperty("parsingException")]
    public Exception ParsingException { get; set; }

    /// <summary>
    /// The version of the program that was used to parse this log. Will be null unless <see cref="ParsingStatus"/>
    /// is <see cref="Logs.ParsingStatus.Parsed"/> or <see cref="Logs.ParsingStatus.Failed"/>.
    /// </summary>
    [JsonProperty("parsingVersion")]
    public Version ParsingVersion { get; set; }

    /// <summary>
    /// Indicates whether the start time of the log is precise, or if it's an approximation based on the file creation date.
    /// </summary>
    [JsonIgnore]
    public bool IsEncounterStartTimePrecise => this.ParsingStatus == ParsingStatus.Parsed && !this.MissingEncounterStart;

    /// <summary>
    /// Indicates whether the end time of the log is precise, or if it's an approximation based on the file creation date.
    /// </summary>
    [JsonIgnore]
    public bool IsEncounterEndTimePrecise => this.ParsingStatus == ParsingStatus.Parsed && !this.MissingEncounterEnd;

    /// <summary>
    /// The tags (and info about tags) applied to this log. Set of <see cref="TagInfo"/> rather than Set of string for extensibility's sake.
    /// </summary>
    [JsonProperty("tags")]
    public ISet<TagInfo> Tags { get; set; } = new HashSet<TagInfo>();

    /// <summary>
    /// Indicates whether a log is a favorite.
    /// </summary>
    [JsonProperty("isFavorite")]
    public bool IsFavorite { get; set; } = false;

    /// <summary>
    /// Extra data that is only relevant for some logs.
    /// </summary>
    //[JsonProperty]
    //public LogExtras LogExtras { get; set; } = null;

    /// <summary>
    /// Indicates whether a log is missing the encounter start time
    /// </summary>
    [JsonIgnore]
    private bool MissingEncounterStart => this.EncounterStartTime is null;
    /// <summary>
    /// Indicates whether a log is missing the encounter start time
    /// </summary>
    [JsonIgnore]
    private bool MissingEncounterEnd => this.EncounterEndTime is null;

    [JsonProperty("htmlFilePath")]
    public string HTMLFilePath { get; set; }

    [JsonConstructor]
    public LogData(string fileName)
    {
        this.FilePath = fileName;
    }
}
