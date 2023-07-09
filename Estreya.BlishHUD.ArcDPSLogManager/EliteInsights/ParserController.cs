namespace Estreya.BlishHUD.ArcDPSLogManager.EliteInsights;

using System;

public class ParserController : GW2EIEvtcParser.ParserController
{
    private readonly IProgress<string> _progress;

    public ParserController(IProgress<string> progress)
    {
        this._progress = progress;
    }

    public override void UpdateProgress(string status)
    {
        base.UpdateProgress(status);

        this._progress?.Report(status);
    }
}