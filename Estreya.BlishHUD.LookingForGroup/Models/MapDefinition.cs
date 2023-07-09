namespace Estreya.BlishHUD.LookingForGroup.Models;

using Estreya.BlishHUD.LookingForGroup.Controls;
using System;

public class MapDefinition
{
    public string Key { get; set; }
    
    public string Name { get; set; }

    public int MapId { get; set; } = -1;
    
    public string Icon { get; set; }

    public WeakReference<CategoryDefinition> Category { get; private set; }

    public void Load(CategoryDefinition category)
    {
        this.Category = new WeakReference<CategoryDefinition>(category);
    }
}