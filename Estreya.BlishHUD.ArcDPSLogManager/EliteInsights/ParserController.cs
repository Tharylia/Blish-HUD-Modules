namespace Estreya.BlishHUD.ArcDPSLogManager.EliteInsights;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ParserController : GW2EIEvtcParser.ParserController
{
    private IProgress<string> _progress;

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
