namespace Estreya.BlishHUD.ArcDPSLogManager.Processing;

using Estreya.BlishHUD.ArcDPSLogManager.Models.Enums;
using Estreya.BlishHUD.ArcDPSLogManager.Models;
using GW2EIEvtcParser;
using GW2EIEvtcParser.EncounterLogic;
using GW2EIEvtcParser.ParserHelpers;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GW2EIEvtcParser.ParsedData;
using GW2EIBuilders;
using SemVer;

public class LogProcessor
{
    private readonly Func<EvtcParser> _getParser;
    private readonly ParserController _parserController;

    public LogProcessor(Func<EvtcParser> getParser, ParserController parserController)
    {
        this._getParser = getParser;
        this._parserController = parserController;
    }

    public void ReportProgress(string progress)
    {
        this._parserController.UpdateProgress(progress);
    }

    public ParsedEvtcLog ParseRaw(string filePath)
    {
        var parsedLog = this._getParser().ParseLog(this._parserController, new FileInfo(filePath), out ParsingFailureReason parsingFailureReason, true)
                ?? throw new InvalidOperationException(parsingFailureReason.Reason.ToString());

        this.ReportProgress("Finished parsing");

        return parsedLog;
    }

    public Models.LogData Parse(string filePath)
    {
        return this.Parse(null, filePath);
    }

    public Models.LogData Parse(ParsedEvtcLog parsedLog, string filePath)
    {
        var logData = new Models.LogData(filePath);

        try
        {
            var stopwatch = Stopwatch.StartNew();
            logData.ParsingStatus = ParsingStatus.Parsing;

            parsedLog ??= this.ParseRaw(filePath);

            logData._parsedEvtcLog = new WeakReference<ParsedEvtcLog>(parsedLog);

            logData.GameLanguage = this.GetLanguageFromId(parsedLog.LogData.LanguageID);
            logData.GameBuild = parsedLog.LogData.GW2Build;
            logData.EvtcVersion = parsedLog.LogData.EvtcVersion;
            logData.ArcDPSVersion = parsedLog.LogData.ArcVersion;
            var pov = parsedLog.PlayerAgents.First(a => a.UniqueID == parsedLog.LogData.PoV.UniqueID);
            var povIdentity = this.SplitAgentName(pov.Name);
            logData.PointOfView = new PointOfView
            {
                AccountName = povIdentity.Account ?? "Unknown",
                CharacterName = povIdentity.Character ?? "Unknown"
            };
            //Encounter = log.EncounterData.Encounter;
            logData.MapId = parsedLog.CombatData.GetMapIDEvents().Last().MapID;
            var mainTarget = parsedLog.FightData.GetMainTargets(parsedLog).First().AgentItem;
            logData.MainTargetName = mainTarget.Name.TrimEnd('\0') ?? "Unknown";
            logData.EncounterResult = parsedLog.FightData.Success ? EncounterResult.Success : EncounterResult.Failure;
            logData.EncounterMode = EncounterMode.Unknown; // TODO: Get Mode
            logData.HealthPercentage = parsedLog.CombatData.GetHealthUpdateEvents(mainTarget).Select(a => a.HPPercent).DefaultIfEmpty(1).Last();
            if (logData.EncounterResult == EncounterResult.Success)
            {
                logData.HealthPercentage = 0;
            }

            logData.Players = parsedLog.PlayerList.Where(x => !string.IsNullOrWhiteSpace(x.Character)).Select(p =>
                new LogPlayer(p.Character, p.Account, p.Group, null, EliteSpecialization.None, // TODO: Fix specs
                    parsedLog.CombatData.GetGuildEvents(p.AgentItem).Last().APIString)
                {
                    Tag = parsedLog.CombatData.GetTagEvents(p.AgentItem).Any() ? PlayerTag.Commander : PlayerTag.None
                }
            ).ToArray();

            if (parsedLog.LogData.LogStart != null)
            {
                logData.EncounterStartTime = DateTimeOffset.Parse(parsedLog.LogData.LogStart);
            }

            if (parsedLog.LogData.LogEnd != null)
            {
                logData.EncounterEndTime = DateTimeOffset.Parse(parsedLog.LogData.LogEnd);
            }

            //LogExtras = new LogExtras();

            //var mistlockInstabilities = logAnalytics.FractalInstabilityDetector.GetInstabilities(log).ToList();
            //if (Encounter.GetEncounterCategory() == EncounterCategory.Fractal || mistlockInstabilities.Count > 0 || log.FractalScale != null)
            //{
            //    LogExtras.FractalExtras = new FractalExtras
            //    {
            //        MistlockInstabilities = mistlockInstabilities,
            //        FractalScale = log.FractalScale,
            //    };
            //}

            logData.EncounterDuration = TimeSpan.FromMilliseconds(parsedLog.FightData.FightDuration);

            stopwatch.Stop();

            logData.ParseMilliseconds = stopwatch.ElapsedMilliseconds;
            logData.ParseTime = DateTimeOffset.Now;
            logData.ParsingStatus = ParsingStatus.Parsed;
        }
        catch (Exception e)
        {
            logData.ParsingStatus = ParsingStatus.Failed;
            logData.ParsingException = e;
        }
        finally
        {
            logData.ParsingVersion = this.GetType().Assembly.GetName().Version;
        }

        //GC.Collect();

        return logData;
    }

    public string BuildHTML(string logFilePath, string outputDirectory, System.Version version)
    {
        return this.BuildHTML(null, logFilePath, outputDirectory, version);
    }

    public string BuildHTML(ParsedEvtcLog parsedEvtcLog, string logFilePath, string outputDirectory, System.Version version)
    {
        parsedEvtcLog ??= this.ParseRaw(logFilePath);

        if (!Directory.Exists(outputDirectory))
        {
            _ = Directory.CreateDirectory(outputDirectory);
        }

        string filePath = Path.Combine(outputDirectory, Path.GetFileName(Path.ChangeExtension(logFilePath, "html")));

        using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        using var sw = new StreamWriter(fs);

        var builder = new HTMLBuilder(parsedEvtcLog,
            new HTMLSettings(false, false, null, null, false),
            new HTMLAssets(), new System.Version(version.ToString()), new UploadResults());
        builder.CreateHTML(sw, filePath);

        return filePath;
    }

    private GameLanguage GetLanguageFromId(LanguageEvent.LanguageEnum id)
    {
        return id switch
        {
            LanguageEvent.LanguageEnum.English => GameLanguage.English,
            LanguageEvent.LanguageEnum.French => GameLanguage.French,
            LanguageEvent.LanguageEnum.German => GameLanguage.German,
            LanguageEvent.LanguageEnum.Spanish => GameLanguage.Spanish,
            LanguageEvent.LanguageEnum.Chinese => GameLanguage.Chinese,
            _ => GameLanguage.Other,
        };
    }

    private (string Account, string Character, byte[] guildBytes) SplitAgentName(string agentName)
    {
        var nameParts = agentName.Split('\0');
        var character = nameParts[0];
        var account = nameParts[1].TrimStart(':');
        var guildBytes = new byte[0];

        return (account, character, guildBytes);
    }

    private string GetGuildGuid(byte[] guidBytes)
    {
        string GetPart(byte[] bytes, int from, int to)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = from; i < to; i++)
            {
                builder.Append($"{bytes[i]:x2}");
            }

            return builder.ToString();
        }

        if (guidBytes == null) return null;
        if (guidBytes.Length != 16)
        {
            throw new ArgumentException("The GUID has to consist of 16 bytes", nameof(guidBytes));
        }

        return $"{GetPart(guidBytes, 0, 4)}-{GetPart(guidBytes, 4, 6)}-{GetPart(guidBytes, 6, 8)}" +
               $"-{GetPart(guidBytes, 8, 10)}-{GetPart(guidBytes, 10, 16)}";
    }
}
