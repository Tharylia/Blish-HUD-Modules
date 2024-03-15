namespace Estreya.BlishHUD.EventTable.Models.Reminders;

using Blish_HUD.Settings;
using Gw2Sharp.WebApi.V2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class EventReminderColors
{
    public SettingEntry<Color> TitleText;

    public SettingEntry<Color> MessageText;

    public SettingEntry<Color> Background;
}
