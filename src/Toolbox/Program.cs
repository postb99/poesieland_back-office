using Microsoft.Extensions.Configuration;
using Toolbox;
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

        _engine = new Engine(_configuration);
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
        MenuItem? menuChoice = null;
        do
        {
            menuChoice = ValidateMenuEntry(parentMenuItem, input);
        } while (menuChoice == null);

        if (PerformAction(menuChoice))
        {
            Console.WriteLine();
            Console.WriteLine("Back to main menu");
            var menuEntry = MainMenu();
            ValidateAndPerformMenuChoice(null, menuEntry);
        }
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
                return true;
            case MainMenuSettings.MenuChoices.GeneratePoemFiles:
            case MainMenuSettings.MenuChoices.ImportPoemContent:
            case MainMenuSettings.MenuChoices.GenerateChartsDataFiles:
                ValidateAndPerformMenuChoice(menuChoice, MenuChoice(menuChoice.SubMenuItems));
                return false;
            case MainMenuSettings.MenuChoices.GenerateSinglePoem:
                GeneratePoemContentFile(menuChoice);
                return true;
            case MainMenuSettings.MenuChoices.ImportSinglePoem:
                ImportPoemContentFile(menuChoice);
                return true;
            case MainMenuSettings.MenuChoices.GenerateAllPoems:
                GenerateAllPoemsContentFiles();
                return true;
            case MainMenuSettings.MenuChoices.GeneratePoemsOfASeason:
                GenerateSeasonPoemContentFiles(menuChoice);
                return true;
            case MainMenuSettings.MenuChoices.ImportPoemsOfASeason:
                ImportSeasonPoemContentFiles(menuChoice);
                return true;
            case MainMenuSettings.MenuChoices.GeneratePoemsLengthBarChartDataFile:
                GeneratePoemsLengthBarChartDataFile();
                return true;
            case MainMenuSettings.MenuChoices.GeneratePoemVersesLengthBarChartDataFile:
                GeneratePoemVersesLengthBarChartDataFile();
                return true;
            case MainMenuSettings.MenuChoices.GenerateSeasonCategoriesPieChartDataFile:
                GenerateSeasonCategoriesPieChart(menuChoice);
                return true;
            case MainMenuSettings.MenuChoices.GeneratePoemsRadarChartDataFile:
                GeneratePoemsRadarChartDataFile(menuChoice);
                return true;
            case MainMenuSettings.MenuChoices.GenerateBubbleChartDataFile:
                GenerateBubbleChartDataFile();
                return true;
            case MainMenuSettings.MenuChoices.GenerateLineChartDataFile:
                GenerateOverSeasonsVerseLengthLineChartDataFile();
                return true;
            case MainMenuSettings.MenuChoices.GenerateCategoriesBubbleChartDataFile:
                _engine.GenerateCategoriesBubbleChartDataFile();
                _engine.GenerateCategoryMetricBubbleChartDataFile();
                return true;
            case MainMenuSettings.MenuChoices.ReloadDataFile:
                _engine.Load();
                return true;
            case MainMenuSettings.MenuChoices.CheckContentMetadataQuality:
                _engine.CheckPoemsWithoutVerseLength();
                _engine.CheckPoemsWithVariableMetric();
                _engine.VerifySeasonHaveCorrectPoemCount();
                _engine.VerifySeasonHaveCorrectWeightInPoemFile(null);
                Console.WriteLine(
                    $"Metric last season computed values sum: {_engine.FillMetricDataDict(out var _).Values.Sum(x => x.Last())}");
                Console.WriteLine("Content metadata quality OK");
                return true;
            case MainMenuSettings.MenuChoices.GenerateAllSeasonsPoemIntervalBarChartDataFile:
                GenerateAllSeasonsPoemIntervalBarChartDataFile();
                return true;
            case MainMenuSettings.MenuChoices.ImportEnPoems:
                ImportEnPoemsContentFiles(menuChoice);
                return true;
            case MainMenuSettings.MenuChoices.OutputSeasonsDuration:
                _engine.OutputSeasonsDuration();
                return true;
            case MainMenuSettings.MenuChoices.ExitProgram:
                Console.WriteLine("Closing program...");
                Environment.Exit(0);
                return true;
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

        if (int.TryParse(choice, out var intChoice))
        {
            _engine.GenerateSeasonAllPoemFiles(intChoice);
            Console.WriteLine("Poem content files OK");
        }
        else
        {
            Console.WriteLine("No matching season for input");
        }
    }

    private static void ImportSeasonPoemContentFiles(MenuItem menuChoice)
    {
        Console.WriteLine(menuChoice.SubMenuItems.First().Label, _engine.Data.Seasons.Count);
        var choice = Console.ReadLine();

        if (int.TryParse(choice, out var seasonId))
        {
            _engine.ImportSeason(seasonId);
            Console.WriteLine("Season import OK");
            GenerateDependantChartDataFiles(seasonId, null);
        }
        else
        {
            Console.WriteLine("No matching season for input");
        }
    }

    private static void ImportEnPoemsContentFiles(MenuItem menuChoice)
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
            Console.WriteLine("No matching poem for input");
        }
    }

    private static void ImportPoemContentFile(MenuItem menuChoice)
    {
        Console.WriteLine(menuChoice.SubMenuItems.First().Label);
        var poemId = Console.ReadLine();

        var importedPoem = _engine.ImportPoem(poemId);
        if (importedPoem is not null)
        {
            Console.WriteLine("Poem import OK");
            var seasonId = int.Parse(poemId.Substring(poemId.LastIndexOf('_') + 1));
            GenerateDependantChartDataFiles(seasonId, importedPoem.Date.Year);
        }
        else
        {
            Console.WriteLine("No matching file for import");
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

        if (int.TryParse(choice, out var intChoice))
        {
            _engine.GenerateSeasonIndexFile(intChoice);
            Console.WriteLine("Season index files OK");
        }
        else
        {
            Console.WriteLine("No matching season for input");
        }
    }

    private static void GeneratePoemsLengthBarChartDataFile()
    {
        _engine.GeneratePoemsLengthBarChartDataFile(null);
        Console.WriteLine("Poems length bar chart data file OK");
    }

    private static void GeneratePoemVersesLengthBarChartDataFile()
    {
        _engine.GeneratePoemMetricBarAndPieChartDataFile(null);
        Console.WriteLine("Poem verses length bar chart data file OK");
    }

    private static void GenerateSeasonCategoriesPieChart(MenuItem menuChoice)
    {
        Console.WriteLine(menuChoice.SubMenuItems.First().Label, _engine.Data.Seasons.Count);
        var choice = Console.ReadLine();

        if (int.TryParse(choice, out var seasonId))
        {
            if (seasonId == 0)
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
            }
            else
            {
                _engine.GenerateSeasonCategoriesPieChartDataFile(seasonId);
                // General chart
                _engine.GenerateSeasonCategoriesPieChartDataFile(null);
            }

            Console.WriteLine(seasonId == 0
                ? "All seasons categories pie chart data file OK"
                : $"Season {seasonId} categories pie chart data file OK");
        }
        else
        {
            Console.WriteLine("No matching season for input");
        }
    }

    private static void GenerateDependantChartDataFiles(int seasonId, int? poemYear)
    {
        // Poem's and season's poems length
        GeneratePoemsLengthBarChartDataFile();
        _engine.GeneratePoemsLengthBarChartDataFile(seasonId);

        // Poem's and season's metric
        GeneratePoemVersesLengthBarChartDataFile();
        _engine.GeneratePoemMetricBarAndPieChartDataFile(seasonId);

        // Season's pie
        _engine.GenerateSeasonCategoriesPieChartDataFile(seasonId);
        // General chart
        _engine.GenerateSeasonCategoriesPieChartDataFile(null);

        Console.WriteLine(seasonId == 0
            ? "All seasons categories pie chart data file OK"
            : $"Season {seasonId} categories pie chart data file OK");

        // Poem by day
        _engine.GeneratePoemsByDayRadarChartDataFile(null, null);
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
        if (poemYear is not null)
        {
            _engine.GeneratePoemsOfYearByDayRadarChartDataFile(poemYear.Value);
            Console.WriteLine("Poem's year by day chart data file OK");
        }

        // Poem count
        _engine.GeneratePoemCountFile();
        Console.WriteLine("Poem count file OK");

        // Poem length by metric and vice versa
        Console.WriteLine("Poems bubble chart data files: starting...");
        _engine.GeneratePoemLengthByVerseLengthBubbleChartDataFile();
        Console.WriteLine("Poems bubble chart data files OK");

        // Over seasons categories', tags' bar, vers length's line
        GenerateOverSeasonsCategoriesAndTagsBarChartDataFile();
        GenerateOverSeasonsVerseLengthLineChartDataFile();

        // Categories bubble chart
        _engine.GenerateCategoriesBubbleChartDataFile();
        // Category metric bubble chart
        _engine.GenerateCategoryMetricBubbleChartDataFile();

        // And check data quality
        _engine.VerifySeasonHaveCorrectPoemCount();
        _engine.VerifySeasonHaveCorrectWeightInPoemFile(seasonId);
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
            Console.WriteLine("No matching category for input (case sensitive)");
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
        _engine.GenerateOverSeasonsVerseLengthLineChartDataFile();
        Console.WriteLine("Line chart data file OK");
    }

    private static void GeneratePoemsCategoriesAndTagsRadarChartDataFile()
    {
        var storageSettings = _configuration.GetSection(Constants.STORAGE_SETTINGS).Get<StorageSettings>();

        foreach (var category in storageSettings!.Categories.SelectMany(x => x.Subcategories).Select(x => x.Name)
                     .Distinct())
        {
            _engine.GeneratePoemsByDayRadarChartDataFile(category, null);
            Console.WriteLine($"Poems by day for '{category}' chart data file OK");
        }

        foreach (var category in storageSettings.Categories.Select(x => x.Name).Distinct())
        {
            _engine.GeneratePoemsByDayRadarChartDataFile(null, category);
            Console.WriteLine($"Poems by day for '{category}' chart data file OK");
        }
    }

    private static void GenerateOverSeasonsCategoriesAndTagsBarChartDataFile()
    {
        var storageSettings = _configuration.GetSection(Constants.STORAGE_SETTINGS).Get<StorageSettings>();

        foreach (var category in storageSettings!.SubcategorieNames)
        {
            _engine.GenerateOverSeasonsChartDataFile(category, null);
            Console.WriteLine($"Poems over seasons for '{category}' chart data file OK");
        }

        foreach (var category in storageSettings.Categories.Select(x => x.Name).Distinct())
        {
            _engine.GenerateOverSeasonsChartDataFile(null, category);
            Console.WriteLine($"Poems over seasons for '{category}' chart data file OK");
        }

        _engine.GenerateOverSeasonsChartDataFile(null, null, forAcrostiche: true);
        Console.WriteLine($"Poems over seasons for 'acrostiche' chart data file OK");

        _engine.GenerateOverSeasonsChartDataFile(null, null, forSonnet: true);
        Console.WriteLine($"Poems over seasons for 'sonnet' chart data file OK");

        _engine.GenerateOverSeasonsChartDataFile(null, null, forPantoun: true);
        Console.WriteLine($"Poems over seasons for 'pantoun' chart data file OK");

        _engine.GenerateOverSeasonsChartDataFile(null, null, forVariableMetric: true);
        Console.WriteLine($"Poems over seasons for 'métrique variable' chart data file OK");
    }

    private static void GenerateAllSeasonsPoemIntervalBarChartDataFile()
    {
        _engine.GeneratePoemIntervalBarChartDataFile(null);
        for (var i = 1; i < _engine.Data.Seasons.Count + 1; i++)
            _engine.GeneratePoemIntervalBarChartDataFile(i);
        Console.WriteLine("Poems interval chart data file OK");
    }
}