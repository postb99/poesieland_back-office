using Microsoft.Extensions.Configuration;
using Toolbox;
using Toolbox.Domain;
using Toolbox.Settings;

public class Program
{
    private static IConfiguration _configuration;
    private static Engine _engine;
    private static MainMenuSettings _mainMenuSettings;

    public static void Main(string[] args)
    {
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        _configuration = configurationBuilder.Build();
        _mainMenuSettings = _configuration.GetSection(Constants.MAIN_MENU).Get<MainMenuSettings>();

        _engine = new(_configuration);
        _engine.Load();

        var menuEntry = MainMenu();
        ValidateAndPerformMenuChoice(null, menuEntry);
    }

    private static string MainMenu()
    {
        return MenuChoice(_mainMenuSettings.MenuItems);
    }

    private static string MenuChoice(List<MenuItem> menuItems)
    {
        foreach (var menuItem in menuItems)
        {
            Console.WriteLine($"[{menuItem.Key}] {menuItem.Label}");
        }

        var defaultChoice = menuItems[0].Key.ToString();
        Console.WriteLine($"Choice [{defaultChoice}]:");
        var input = Console.ReadLine();
        return string.IsNullOrWhiteSpace(input) ? defaultChoice : input;
    }

    private static void ValidateAndPerformMenuChoice(MenuItem? parentMenuItem, string input)
    {
        MenuItem? menuChoice = ValidateMenuEntry(parentMenuItem, input);
        while (menuChoice is null)
        {
            Console.WriteLine("ERROR: No such choice");
            input = Console.ReadLine();
            menuChoice = ValidateMenuEntry(parentMenuItem, input);
        }

        if (!PerformAction(menuChoice)) return;
        Console.WriteLine();
        Console.WriteLine("Back to main menu");
        var menuEntry = MainMenu();
        ValidateAndPerformMenuChoice(null, menuEntry);
    }

    private static MenuItem? ValidateMenuEntry(MenuItem? parentMenuItem, string entry)
    {
        var ok = Enum.TryParse<MainMenuSettings.MenuChoices>(entry, true, out var menuChoice);
        if (ok)
        {
            var menuItem = parentMenuItem is not null
                ? parentMenuItem.SubMenuItems.FirstOrDefault(x => x.Key == (int)menuChoice)
                : _mainMenuSettings.MenuItems.FirstOrDefault(x => x.Key == (int)menuChoice);
            return menuItem;
        }

        return null;
    }

    private static bool PerformAction(MenuItem menuChoice)
    {
        switch ((MainMenuSettings.MenuChoices)menuChoice.Key)
        {
            case MainMenuSettings.MenuChoices.GenerateSeasonIndexFile:
                GenerateSeasonIndexFiles(menuChoice);
                break;
            case MainMenuSettings.MenuChoices.GeneratePoemFiles:
            case MainMenuSettings.MenuChoices.Import:
            case MainMenuSettings.MenuChoices.GenerateChartsDataFiles:
                ValidateAndPerformMenuChoice(menuChoice, MenuChoice(menuChoice.SubMenuItems));
                return false;
            case MainMenuSettings.MenuChoices.GenerateSinglePoem:
                GeneratePoemContentFile(menuChoice);
                break;
            case MainMenuSettings.MenuChoices.ImportSinglePoem:
                ImportPoemContentFile(menuChoice);
                break;
            case MainMenuSettings.MenuChoices.GenerateAllPoems:
                GenerateAllPoemsContentFiles();
                break;
            case MainMenuSettings.MenuChoices.GeneratePoemsOfASeason:
                GenerateSeasonPoemContentFiles(menuChoice);
                break;
            case MainMenuSettings.MenuChoices.ImportPoemsOfASeason:
                ImportSeasonPoemContentFiles(menuChoice);
                break;
            case MainMenuSettings.MenuChoices.ImportSeasonMetadata:
                ImportSeasonMetadata(menuChoice);
                break;
            case MainMenuSettings.MenuChoices.GeneratePoemsLengthBarChartDataFile:
                GeneratePoemsLengthPieChartDataFile();
                break;
            case MainMenuSettings.MenuChoices.GeneratePoemVersesLengthBarChartDataFile:
                GeneratePoemMetricPieChartDataFile();
                break;
            case MainMenuSettings.MenuChoices.GenerateSeasonCategoriesPieChartDataFile:
                GenerateSeasonCategoriesPieChart(menuChoice);
                break;
            case MainMenuSettings.MenuChoices.GeneratePoemsRadarChartDataFile:
                GeneratePoemsRadarChartDataFile(menuChoice);
                break;
            case MainMenuSettings.MenuChoices.GenerateBubbleChartDataFile:
                GenerateBubbleChartDataFile();
                break;
            case MainMenuSettings.MenuChoices.GenerateLineChartDataFile:
                GenerateOverSeasonsVerseLengthLineChartDataFile();
                break;
            case MainMenuSettings.MenuChoices.GenerateCategoriesBubbleChartDataFile:
                _engine.GenerateCategoriesBubbleChartDataFile();
                _engine.GenerateCategoryMetricBubbleChartDataFile();
                break;
            case MainMenuSettings.MenuChoices.ReloadDataFile:
                _engine.Load();
                break;
            case MainMenuSettings.MenuChoices.CheckContentMetadataQuality:
                _engine.CheckPoemsWithoutVerseLength();
                _engine.CheckPoemsWithVariableMetric();
                _engine.VerifySeasonHaveCorrectPoemCount();
                _engine.VerifySeasonHaveCorrectWeightInPoemFile(null);
                _engine.VerifyPoemWithLesMoisExtraTagIsListedOnCustomPage(null);
                Console.WriteLine(
                    $"Metric last season computed values sum: {_engine.FillMetricDataDict(out var _).Values.Sum(x => x.Last())}");
                Console.WriteLine("Content metadata quality OK");
                break;
            case MainMenuSettings.MenuChoices.GenerateAllSeasonsPoemIntervalBarChartDataFile:
                GenerateAllSeasonsPoemIntervalBarChartDataFile();
                break;
            case MainMenuSettings.MenuChoices.ImportEnPoems:
                ImportEnPoemsContentFiles();
                break;
            case MainMenuSettings.MenuChoices.OutputSeasonsDuration:
                _engine.OutputSeasonsDuration();
                break;
            case MainMenuSettings.MenuChoices.OutputReusedTitles:
                foreach (var reusedTitle in _engine.GetReusedTitles())
                {
                    Console.WriteLine(reusedTitle);
                }

                break;
            case MainMenuSettings.MenuChoices.ExitProgram:
                Console.WriteLine("Closing program...");
                Environment.Exit(0);
                break;
        }

        return true;
    }

    private static void GenerateSeasonPoemContentFiles(MenuItem menuChoice)
    {
        Console.WriteLine(menuChoice.SubMenuItems.First().Label, _engine.Data.Seasons.Count);
        var choice = Console.ReadLine();
        if (choice == "0")
        {
            GenerateAllPoemsContentFiles();
            return;
        }

        if (int.TryParse(choice, out var intChoice) &&
            _engine.Data.Seasons.FirstOrDefault(x => x.Id == intChoice) is not null)
        {
            _engine.GenerateSeasonAllPoemFiles(intChoice);
            Console.WriteLine("Poem content files OK");
        }
        else
        {
            Console.WriteLine($"ERROR: No matching season for input: {choice}");
        }
    }

    private static void ImportSeasonPoemContentFiles(MenuItem menuChoice)
    {
        Console.WriteLine(menuChoice.SubMenuItems.First().Label, _engine.Data.Seasons.Count);
        var choice = Console.ReadLine();

        if (int.TryParse(choice, out var seasonId) &&
            _engine.Data.Seasons.FirstOrDefault(x => x.Id == seasonId) is not null)
        {
            _engine.ImportSeason(seasonId);
            Console.WriteLine("Season import OK");
            GenerateDependantChartDataFiles(seasonId, null);
        }
        else
        {
            Console.WriteLine($"ERROR: No matching season for input: {choice}");
        }
    }

    private static void ImportSeasonMetadata(MenuItem menuChoice)
    {
        Console.WriteLine(menuChoice.SubMenuItems.First().Label, _engine.Data.Seasons.Count);
        var choice = Console.ReadLine();

        if (int.TryParse(choice, out var seasonId) &&
            _engine.Data.Seasons.FirstOrDefault(x => x.Id == seasonId) is not null)
        {
            _engine.ImportSeasonMetadata(seasonId);
            Console.WriteLine("Season metadata import OK");
        }
        else
        {
            Console.WriteLine($"ERROR: No matching season for input: {choice}");
        }
    }

    private static void ImportEnPoemsContentFiles()
    {
        _engine.ImportPoemsEn();
        Console.WriteLine("Poems import OK");

        _engine.GeneratePoemEnCountFile();
        Console.WriteLine("Poems count OK");

        _engine.GeneratePoemsEnByDayRadarChartDataFile();
        Console.WriteLine("Chart for day radar OK");

        _engine.GenerateEnPoemByDayOfWeekPieChartDataFile();
        Console.WriteLine("Chart for day of week OK");
    }

    private static void GeneratePoemContentFile(MenuItem menuChoice)
    {
        Console.WriteLine(menuChoice.SubMenuItems.First().Label);
        var poemId = Console.ReadLine();

        var poem = _engine.Data.Seasons.SelectMany(x => x.Poems).FirstOrDefault(x => x.Id == poemId);
        if (poem is not null)
        {
            _engine.GeneratePoemFile(poem);
            Console.WriteLine("Poem content file OK");
        }
        else
        {
            Console.WriteLine($"ERROR: No matching poem for input: {poemId}");
        }
    }

    private static void ImportPoemContentFile(MenuItem menuChoice)
    {
        Console.WriteLine(menuChoice.SubMenuItems.First().Label);
        var poemId = Console.ReadLine();

        try
        {
            var importedPoem = _engine.ImportPoem(poemId);
            Console.WriteLine("Poem import OK");
            var seasonId = int.Parse(poemId.Substring(poemId.LastIndexOf('_') + 1));
            GenerateDependantChartDataFiles(seasonId, importedPoem);
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"ERROR: No matching file to import for: {poemId}, {ex.Message}");
        }
    }

    private static void GenerateAllPoemsContentFiles()
    {
        _engine.GenerateAllPoemFiles();
        Console.WriteLine("All poem content files OK");
    }

    private static void GenerateSeasonIndexFiles(MenuItem menuChoice)
    {
        Console.WriteLine(menuChoice.SubMenuItems.First().Label, _engine.Data.Seasons.Count);
        var choice = Console.ReadLine();
        if (choice == "0")
        {
            _engine.GenerateAllSeasonsIndexFile();
            Console.WriteLine("Seasons index files OK");
            return;
        }

        if (int.TryParse(choice, out var intChoice) &&
            _engine.Data.Seasons.FirstOrDefault(x => x.Id == intChoice) is not null)
        {
            _engine.GenerateSeasonIndexFile(intChoice);
            Console.WriteLine("Season index files OK");
        }
        else
        {
            Console.WriteLine($"ERROR: No matching season for input: {choice}");
        }
    }

    private static void GeneratePoemsLengthPieChartDataFile()
    {
        _engine.GeneratePoemsLengthBarAndPieChartDataFile(null);
        Console.WriteLine("Poems length pie chart data file OK");
    }

    private static void GeneratePoemMetricPieChartDataFile()
    {
        _engine.GeneratePoemMetricBarAndPieChartDataFile(null, true);
        Console.WriteLine("Poem verses length pie chart data file OK");
    }

    private static void GenerateSeasonCategoriesPieChart(MenuItem menuChoice)
    {
        Console.WriteLine(menuChoice.SubMenuItems.First().Label, _engine.Data.Seasons.Count);
        var choice = Console.ReadLine();

        if (choice == "0")
        {
            for (var i = 1; i < _engine.Data.Seasons.Count + 1; i++)
            {
                _engine.GenerateSeasonCategoriesPieChartDataFile(i);
            }

            // General chart
            _engine.GenerateSeasonCategoriesPieChartDataFile(null);

            // Categories' and tags' radar
            GeneratePoemsCategoriesAndTagsRadarChartDataFile();

            // Over seasons categories' and tags' bar
            GenerateOverSeasonsCategoriesAndTagsBarChartDataFile();

            Console.WriteLine("All seasons categories pie chart data file OK");
        }
        else if (int.TryParse(choice, out var seasonId) &&
                 _engine.Data.Seasons.FirstOrDefault(x => x.Id == seasonId) is not null)
        {
            _engine.GenerateSeasonCategoriesPieChartDataFile(seasonId);
            // General chart
            _engine.GenerateSeasonCategoriesPieChartDataFile(null);

            Console.WriteLine($"Season {seasonId} categories pie chart data file OK");
        }
        else
        {
            Console.WriteLine($"ERROR: No matching season for input: {seasonId}");
        }
    }

    private static void GenerateDependantChartDataFiles(int seasonId, Poem? importedPoem)
    {
        // General and season's poems length
        GeneratePoemsLengthPieChartDataFile();
        _engine.GeneratePoemsLengthBarAndPieChartDataFile(seasonId);

        // General and season's metric
        GeneratePoemMetricPieChartDataFile();
        _engine.GeneratePoemMetricBarAndPieChartDataFile(seasonId, false);

        // Season's pie
        _engine.GenerateSeasonCategoriesPieChartDataFile(seasonId);
        // General chart
        _engine.GenerateSeasonCategoriesPieChartDataFile(null);

        Console.WriteLine(seasonId == 0
            ? "All seasons categories pie chart data file OK"
            : $"Season {seasonId} categories pie chart data file OK");

        // Poem by day
        _engine.GeneratePoemsByDayRadarChartDataFile(null, null);
        _engine.GeneratePoemsByDayRadarChartDataFile(null, null, forLesMoisExtraTag: true);
        _engine.GeneratePoemIntensityPieChartDataFile();
        _engine.GeneratePoemByDayOfWeekPieChartDataFile();
        Console.WriteLine("Poems by day chart data file OK");

        // Poem interval
        _engine.GeneratePoemIntervalBarChartDataFile(null);
        _engine.GeneratePoemIntervalBarChartDataFile(seasonId);
        Console.WriteLine("Poems interval chart data file OK");

        // Categories' and tags' radar
        GeneratePoemsCategoriesAndTagsRadarChartDataFile();
        
        // Year tag's radar
        if (importedPoem is not null)
        {
            _engine.GeneratePoemsOfYearByDayRadarChartDataFile(importedPoem.Date.Year);
            Console.WriteLine("Poem's year by day chart data file OK");
        }

        // Poem count
        _engine.GeneratePoemCountFile();
        Console.WriteLine("Poem count file OK");

        // Poem length by metric and vice versa
        _engine.GeneratePoemLengthByVerseLengthBubbleChartDataFile();
        Console.WriteLine("Poems bubble chart data files OK");

        // Over seasons categories', tags' bar, verse length's line
        GenerateOverSeasonsCategoriesAndTagsBarChartDataFile();
        GenerateOverSeasonsVerseLengthLineChartDataFile();

        // Categories bubble chart
        _engine.GenerateCategoriesBubbleChartDataFile();
        // Category metric bubble chart
        _engine.GenerateCategoryMetricBubbleChartDataFile();

        // And check data quality
        _engine.VerifySeasonHaveCorrectPoemCount();
        _engine.VerifySeasonHaveCorrectWeightInPoemFile(seasonId);
        _engine.VerifyPoemWithLesMoisExtraTagIsListedOnCustomPage(importedPoem);
        Console.WriteLine(
            $"Content metadata quality OK. Info: metric last season computed values sum: {_engine.FillMetricDataDict(out var _).Values.Sum(x => x.Last())}");
    }

    private static void GeneratePoemsRadarChartDataFile(MenuItem menuChoice)
    {
        Console.WriteLine(menuChoice.SubMenuItems.First().Label);
        var choice = Console.ReadLine();

        if (string.IsNullOrEmpty(choice))
        {
            _engine.GeneratePoemsByDayRadarChartDataFile(null, null);
            _engine.GeneratePoemsByDayRadarChartDataFile(null, null, forLesMoisExtraTag: true);
            _engine.GeneratePoemIntensityPieChartDataFile();
            _engine.GeneratePoemByDayOfWeekPieChartDataFile();
            Console.WriteLine("Poems by day and cie chart data file OK");
            GeneratePoemsCategoriesAndTagsRadarChartDataFile();
            Console.WriteLine("Categories and tags radar chart data file OK");
            GenerateOverSeasonsCategoriesAndTagsBarChartDataFile();
            Console.WriteLine("Categories and tags bar chart data file OK");
            return;
        }

        var colorSettings = _configuration.GetSection(Constants.STORAGE_SETTINGS).Get<StorageSettings>();
        var color = colorSettings!.Categories.SelectMany(x => x.Subcategories).FirstOrDefault(x => x.Name == choice);

        if (color == null)
        {
            Console.WriteLine($"ERROR: No matching category for input (case sensitive) : {choice}");
        }
        else
        {
            _engine.GeneratePoemsByDayRadarChartDataFile(choice, null);
            Console.WriteLine($"Poems by day for '{choice}' chart data file OK");
        }
    }

    private static void GenerateBubbleChartDataFile()
    {
        _engine.GeneratePoemLengthByVerseLengthBubbleChartDataFile();
        Console.WriteLine("Bubble chart data file OK");
    }

    private static void GenerateOverSeasonsVerseLengthLineChartDataFile()
    {
        _engine.GenerateOverSeasonsMetricLineChartDataFile();
        Console.WriteLine("Line chart data file OK");
    }

    private static void GeneratePoemsCategoriesAndTagsRadarChartDataFile()
    {
        var storageSettings = _configuration.GetSection(Constants.STORAGE_SETTINGS).Get<StorageSettings>();

        foreach (var category in storageSettings!.Categories.SelectMany(x => x.Subcategories).Select(x => x.Name)
                     .Distinct())
        {
            _engine.GeneratePoemsByDayRadarChartDataFile(category, null);
        }

        Console.WriteLine("Poems by day for all categories chart data files OK");


        foreach (var category in storageSettings.Categories.Select(x => x.Name).Distinct())
        {
            _engine.GeneratePoemsByDayRadarChartDataFile(null, category);
        }

        Console.WriteLine("Poems by day for all tags chart data files OK");
    }

    private static void GenerateOverSeasonsCategoriesAndTagsBarChartDataFile()
    {
        var storageSettings = _configuration.GetSection(Constants.STORAGE_SETTINGS).Get<StorageSettings>();

        foreach (var category in storageSettings!.SubcategorieNames)
        {
            _engine.GenerateOverSeasonsChartDataFile(category, null);
        }

        Console.WriteLine("Poems over seasons for all categories chart data files OK");

        foreach (var category in storageSettings.Categories.Select(x => x.Name).Distinct())
        {
            _engine.GenerateOverSeasonsChartDataFile(null, category);
        }

        Console.WriteLine("Poems over seasons for all tags chart data files OK");

        _engine.GenerateOverSeasonsChartDataFile(null, null, forAcrostiche: true);
        _engine.GenerateOverSeasonsChartDataFile(null, null, forSonnet: true);
        _engine.GenerateOverSeasonsChartDataFile(null, null, forPantoun: true);
        _engine.GenerateOverSeasonsChartDataFile(null, null, forVariableMetric: true);
        _engine.GenerateOverSeasonsChartDataFile(null, null, forRefrain: true);
        _engine.GenerateOverSeasonsChartDataFile(null, null, forLovecat: true);
        _engine.GenerateOverSeasonsChartDataFile(null, null, forLesMois: true);
        _engine.GenerateOverSeasonsChartDataFile(null, null, forMetric: 1);
        _engine.GenerateOverSeasonsChartDataFile(null, null, forMetric: 2);
        _engine.GenerateOverSeasonsChartDataFile(null, null, forMetric: 3);
        _engine.GenerateOverSeasonsChartDataFile(null, null, forMetric: 4);
        _engine.GenerateOverSeasonsChartDataFile(null, null, forMetric: 5);
        _engine.GenerateOverSeasonsChartDataFile(null, null, forMetric: 6);
        _engine.GenerateOverSeasonsChartDataFile(null, null, forMetric: 7);
        _engine.GenerateOverSeasonsChartDataFile(null, null, forMetric: 8);
        _engine.GenerateOverSeasonsChartDataFile(null, null, forMetric: 9);
        _engine.GenerateOverSeasonsChartDataFile(null, null, forMetric: 10);
        _engine.GenerateOverSeasonsChartDataFile(null, null, forMetric: 11);
        _engine.GenerateOverSeasonsChartDataFile(null, null, forMetric: 12);

        Console.WriteLine("Poems over seasons for 'acrostiche', 'sonnet', 'pantoun', 'métrique variable', 'refrain', 'lovecat', 'les mois', 1-12 metrics chart data files OK");
    }

    private static void GenerateAllSeasonsPoemIntervalBarChartDataFile()
    {
        _engine.GeneratePoemIntervalBarChartDataFile(null);
        for (var i = 1; i < _engine.Data.Seasons.Count + 1; i++)
            _engine.GeneratePoemIntervalBarChartDataFile(i);
        Console.WriteLine("Poems interval chart data file OK");
    }
}