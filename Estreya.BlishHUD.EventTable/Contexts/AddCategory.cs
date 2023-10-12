namespace Estreya.BlishHUD.EventTable.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public struct AddCategory
{
    public AddCategory()
    {
    }

    public string Key { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
    public bool ShowCombined { get; set; } = false;
}
