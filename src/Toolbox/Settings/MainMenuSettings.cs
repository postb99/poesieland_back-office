namespace Toolbox.Settings;

public class MainMenuSettings
{
    public enum MenuChoices
    {
        GenerateSeasonIndexFile = 100,
        GenerateAskSeasonNumberForIndexFile = 110,
        GeneratePoemFiles = 200,
        GenerateSinglePoem = 210,
        GenerateInputPoemId = 211,
        GeneratePoemsOfASeason = 220,
        GenerateAskSeasonNumberForPoemFile = 221,
        GenerateAllPoems = 230,
        ReloadDataFile = 0,
        ImportPoemContent = 300,
        ImportSinglePoem = 310,
        ImportInputPoemId = 311,
        ImportPoemsOfASeason = 320,
        ImportAskSeasonNumberForPoemFile = 321,
        GenerateChartsDataFiles = 400,
        GeneratePoemsLengthBarChartDataFile = 410,
        ExitProgram = 99
    }
    
    public List<MenuItem> MenuItems { get; set; }
}

public class MenuItem
{
    public int Key { get; set; }
    public string Label { get; set; }
    public List<MenuItem> SubMenuItems { get; set; }
}