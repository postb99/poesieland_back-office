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
            Console.WriteLine(Environment.NewLine);
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
            var menuItem = parentMenuItem != null
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
            case MainMenuSettings.MenuChoices.ReloadDataFile:
                _engine.Load();
                return true;
            case MainMenuSettings.MenuChoices.CheckPoemsWithoutVerseLength:
                _engine.CheckPoemsWithoutVerseLength();
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

    private static void GeneratePoemContentFile(MenuItem menuChoice)
    {
        Console.WriteLine(menuChoice.SubMenuItems.First().Label);
        var poemId = Console.ReadLine();

        var poem = _engine.Data.Seasons.SelectMany(x => x.Poems).FirstOrDefault(x => x.Id == poemId);
        if (poem != null)
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
        if (importedPoem != null)
        {
            Console.WriteLine("Poem import OK");
            var seasonId = int.Parse(poemId.Substring(poemId.LastIndexOf('_') + 1));
            GenerateDependantChartDataFiles(seasonId, importedPoem.Date.Year);

            var missingYearInTags = _engine.CheckMissingYearTagInYamlMetadata();
            if (missingYearInTags.Any())
            {
                Console.WriteLine($"Missing year in tags for poems: {string.Join(',', missingYearInTags)}");
            }
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
        
        // Once
        // for (var i = 1; i < _engine.Data.Seasons.Count + 1; i++)
        // {
        //     _engine.GeneratePoemsLengthBarChartDataFile(i);
        // }
    }

    private static void GeneratePoemVersesLengthBarChartDataFile()
    {
        _engine.GeneratePoemVersesLengthBarChartDataFile(null);
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
            }
            else
            {
                _engine.GenerateSeasonCategoriesPieChartDataFile(seasonId);
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

        // Poem's and season's verse length
        GeneratePoemVersesLengthBarChartDataFile();
        _engine.GeneratePoemVersesLengthBarChartDataFile(seasonId);

        // Season's pie
        if (seasonId == 0)
        {
            for (var i = 1; i < _engine.Data.Seasons.Count + 1; i++)
            {
                _engine.GenerateSeasonCategoriesPieChartDataFile(i);
            }
        }
        else
        {
            _engine.GenerateSeasonCategoriesPieChartDataFile(seasonId);
        }

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
        if (poemYear != null)
        {
            _engine.GeneratePoemsOfYearByDayRadarChartDataFile(poemYear.Value);
            Console.WriteLine("Poem's year by day chart data file OK");
        }

        // Acrostiche - not anymore useful
        // _engine.GenerateAcrosticheBarChartDataFile();
        // Console.WriteLine("Acrostiche chart data file OK");
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
            return;
        }

        var colorSettings = _configuration.GetSection(Constants.STORAGE_SETTINGS).Get<StorageSettings>();
        var color = colorSettings.Categories.SelectMany(x => x.Subcategories).FirstOrDefault(x => x.Name == choice);

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

    private static void GeneratePoemsCategoriesAndTagsRadarChartDataFile()
    {
        var storageSettings = _configuration.GetSection(Constants.STORAGE_SETTINGS).Get<StorageSettings>();

        foreach (var category in storageSettings.Categories.SelectMany(x => x.Subcategories).Select(x => x.Name)
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
}