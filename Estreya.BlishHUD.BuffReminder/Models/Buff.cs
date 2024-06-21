namespace Estreya.BlishHUD.BuffReminder.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Buff
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Icon { get; set; }
    public List<uint> Ids { get; set; }
}
