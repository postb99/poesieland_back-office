namespace Toolbox;

public class MainMenuSettings
{
    public enum MenuChoices
    {
        GenerateSeasonIndexFile = 1
    }
    
    public List<MenuItem> MenuItems { get; set; }
}

public class MenuItem
{
    public int Key { get; set; }
    public string Label { get; set; }
}