namespace Estreya.BlishHUD.LookingForGroup.Models;

public class CategoryDefinition
{
    public string Key { get; set; }
    
    public string Name { get; set; }

    public string Description { get; set; }
    
    public string Icon { get; set; }
    
    public MapDefinition[] Maps { get; set; }
}