namespace Toolbox.Settings;

public class MainMenuSettings
{
    public enum MenuChoices
    {
        GenerateSeasonIndexFile = 100,
        GenerateSeasonIndexFileAskSeasonNumber = 110,
        GeneratePoemFiles = 200,
        GenerateSinglePoem = 210,
        GenerateSinglePoemAskPoemId = 211,
        GeneratePoemsOfASeason = 220,
        GeneratePoemsOfASeasonAskPoemId = 221,
        ReloadDataFile = 0,
        Import = 300,
        ImportSinglePoem = 310,
        ImportSinglePoemAskPoemId = 311,
        ImportPoemsOfASeason = 320,
        ImportPoemsOfASeasonAskSeasonId = 321,
        ImportSeasonMetadata = 330,
        ImportSeasonMetadataAskSeasonId = 331,
        GenerateChartsDataFiles = 400,
        GeneratePoemsLengthBarChartDataFile = 410,
        GenerateSeasonCategoriesPieChartDataFile = 420,
        GenerateSeasonCategoriesPieChartAskSeasonId = 421,
        GeneratePoemsRadarChartDataFile = 430,
        GeneratePoemsRadarChartAskCategory = 431,
        GeneratePoemVersesLengthBarChartDataFile = 440,
        GenerateAllSeasonsPoemIntervalBarChartDataFile = 450,
        GenerateBubbleChartDataFile = 460,
        GenerateLineChartDataFile = 470,
        GenerateCategoriesBubbleChartDataFile = 480,
        CheckContentMetadataQuality = 500,
        ImportEnPoems = 600,
        OutputSeasonsDuration = 700,
        OutputReusedTitles = 800,
        ExitProgram = 99
    }

    public List<MenuItem> MenuItems { get; set; } = [];
}

public class MenuItem
{
    public int Key { get; set; }
    public string Label { get; set; } = string.Empty;
    public List<MenuItem> SubMenuItems { get; set; } = [];
}