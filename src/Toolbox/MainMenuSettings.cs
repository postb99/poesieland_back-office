namespace Toolbox;

public class MainMenuSettings
{
    public enum MenuChoices
    {
        GenerateSeasonIndexFile = 10,
        AskSeasonNumber = 11
    }
    
    public List<MenuItem> MenuItems { get; set; }
}

public class MenuItem
{
    public int Key { get; set; }
    public string Label { get; set; }
    public List<MenuItem> SubMenuItems { get; set; }
}