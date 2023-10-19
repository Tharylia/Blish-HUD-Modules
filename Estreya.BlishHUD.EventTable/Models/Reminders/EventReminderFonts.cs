namespace Estreya.BlishHUD.EventTable.Models.Reminders;

using Blish_HUD.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Blish_HUD.ContentService;

public class EventReminderFonts
{
    public SettingEntry<FontSize> TitleSize { get; set; }
    public SettingEntry<FontSize> MessageSize { get; set; }
}
