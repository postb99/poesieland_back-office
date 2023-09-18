namespace Toolbox;

public class MainMenuSettings
{
    public enum MenuChoices
    {
        GenerateSeasonIndexFile = 10,
        AskSeasonNumberForIndexFile = 11,
        GeneratePoemFiles = 200,
        SinglePoem = 210,
        InputPoemId = 211,
        PoemsOfASeason = 220,
        AskSeasonNumberForPoemFile = 221
        }
    
    public List<MenuItem> MenuItems { get; set; }
}

public class MenuItem
{
    public int Key { get; set; }
    public string Label { get; set; }
    public List<MenuItem> SubMenuItems { get; set; }
}