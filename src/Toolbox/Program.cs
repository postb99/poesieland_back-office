using Microsoft.Extensions.Configuration;
using Toolbox.Charts;
using Toolbox.Consistency;
using Toolbox.Domain;
using Toolbox.Generators;
using Toolbox.Importers;
using Toolbox.Persistence;
using Toolbox.Settings;

namespace Toolbox;

public class Program
{
    private static IConfiguration? _configuration;
    private static Engine? _engine;
    private static MainMenuSettings? _mainMenuSettings;
    private static DataManager? _dataManager;
    private static ContentFileGenerator _contentFileGenerator;
    private static PoemImporter _poemImporter;
    private static SeasonMetadataImporter _seasonMetadataImporter;
    private static CustomPageChecker _customPageChecker;
    private static YamlMetadataChecker _yamlMetadataChecker;
    private static ChartDataFileGenerator _chartDataFileGenerator;
    private static PoemMetadataChecker _poemMetadataChecker;

    public static void Main(string[] args)
    {
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        _configuration = configurationBuilder.Build();
        _mainMenuSettings = _configuration.GetSection(Constants.MAIN_MENU).Get<MainMenuSettings>();

        _dataManager = new DataManager(_configuration);
        _contentFileGenerator = new ContentFileGenerator(_configuration);
        _poemImporter = new PoemImporter(_configuration);
        _seasonMetadataImporter = new SeasonMetadataImporter(_configuration);
        _customPageChecker = new CustomPageChecker(_configuration);
        _chartDataFileGenerator = new ChartDataFileGenerator(_configuration);
        _poemMetadataChecker = new PoemMetadataChecker(_configuration, _poemImporter);

        _engine = new(_configuration, _dataManager);
        _engine.Load();

        _yamlMetadataChecker = new YamlMetadataChecker(_configuration, _engine.Data);

        var menuEntry = MainMenu();
        ValidateAndPerformMenuChoice(null, menuEntry);
    }

    private static string MainMenu()
    {
        return MenuChoice(_mainMenuSettings!.MenuItems);
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
                : _mainMenuSettings!.MenuItems.FirstOrDefault(x => x.Key == (int)menuChoice);
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
                PoemMetadataChecker.CheckPoemsWithoutVerseLength(_engine.Data);
                PoemMetadataChecker.CheckPoemsWithVariableMetric(_engine.Data);
                SeasonChecker.VerifySeasonHaveCorrectPoemCount(_engine.Data);
                _poemMetadataChecker.VerifySeasonHaveCorrectWeightInPoemFile(_engine.Data, null);
                var outputs = _yamlMetadataChecker.GetMissingTagsInYamlMetadata();
                foreach (var output in outputs)
                {
                    Console.WriteLine(output);
                }

                // Les mois
                outputs = _customPageChecker.GetPoemWithLesMoisExtraTagNotListedOnCustomPage(null, _engine.Data);
                foreach (var output in outputs)
                {
                    Console.WriteLine(output);
                }

                // Ciel
                outputs = _customPageChecker.GetPoemOfSkyCategoryStartingWithSpecificWordsNotListedOnCustomPage(null,
                    _engine.Data);
                foreach (var output in outputs)
                {
                    Console.WriteLine(output);
                }

                // Saisons
                outputs = _customPageChecker.GetPoemOfMoreThanOneSeasonNotListedOnCustomPage(null, _engine.Data);
                foreach (var output in outputs)
                {
                    Console.WriteLine(output);
                }

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
                var reusedTitles = new ReusedTitlesChecker(_engine.Data).GetReusedTitles();
                foreach (var reusedTitle in reusedTitles)
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
            _contentFileGenerator.GenerateSeasonAllPoemFiles(_engine.Data, intChoice);
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
            _poemImporter.ImportPoemsOfSeason(seasonId, _engine.Data);
            _dataManager.Save(_engine.Data);
            Console.WriteLine("Season import OK");
            GenerateDependantChartDataFilesAndCheckQuality(seasonId, null);
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
            _seasonMetadataImporter.ImportSeasonMetadata(seasonId, _engine.Data);
            _dataManager.Save(_engine.Data);
            Console.WriteLine("Season metadata import OK");
        }
        else
        {
            Console.WriteLine($"ERROR: No matching season for input: {choice}");
        }
    }

    private static void ImportEnPoemsContentFiles()
    {
        _poemImporter.ImportPoemsEn(_engine.DataEn);
        _dataManager.SaveEn(_engine.DataEn);
        Console.WriteLine("Poems import OK");

        _contentFileGenerator.GeneratePoemEnCountFile(_engine.DataEn);
        Console.WriteLine("Poems count OK");

        _chartDataFileGenerator.GeneratePoemsEnByDayRadarChartDataFile(_engine.DataEn);
        _chartDataFileGenerator.GeneratePoemsByDayRadarChartDataFile(_engine.Data, _engine.DataEn, null, null);
        Console.WriteLine("Charts for day radar OK");

        _chartDataFileGenerator.GeneratePoemIntensityPieChartDataFile(_engine.Data, _engine.DataEn);
        Console.WriteLine("Poem intensity chart OK");

        _chartDataFileGenerator.GenerateEnPoemByDayOfWeekPieChartDataFile(_engine.DataEn);
        _chartDataFileGenerator.GeneratePoemByDayOfWeekPieChartDataFile(_engine.Data, _engine.DataEn);
        Console.WriteLine("Chart for day of week OK");

        _chartDataFileGenerator.GeneratePoemIntervalBarChartDataFile(_engine.Data, _engine.DataEn, null);
        Console.WriteLine("Poem interval bar chart OK");
        GenerateAllSeasonsPoemIntervalBarChartDataFile();
    }

    private static void GeneratePoemContentFile(MenuItem menuChoice)
    {
        Console.WriteLine(menuChoice.SubMenuItems.First().Label);
        var poemId = Console.ReadLine();

        var poem = _engine.Data.Seasons.SelectMany(x => x.Poems).FirstOrDefault(x => x.Id == poemId);
        if (poem is not null)
        {
            _contentFileGenerator.GeneratePoemFile(_engine.Data, poem);
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
            var importedPoem = _poemImporter.ImportPoem(poemId, _engine.Data);
            _dataManager.Save(_engine.Data);
            Console.WriteLine("Poem import OK");
            var seasonId = int.Parse(poemId.Substring(poemId.LastIndexOf('_') + 1));
            GenerateDependantChartDataFilesAndCheckQuality(seasonId, importedPoem);
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
        _contentFileGenerator.GenerateAllPoemFiles(_engine.Data);
        Console.WriteLine("All poem content files OK");
    }

    private static void GenerateSeasonIndexFiles(MenuItem menuChoice)
    {
        var seasonCount = _engine.Data.Seasons.Count;
        Console.WriteLine(menuChoice.SubMenuItems.First().Label, seasonCount);
        var choice = Console.ReadLine();
        if (choice == "0")
        {
            for (var i = 1; i <= seasonCount; i++)
                _contentFileGenerator.GenerateSeasonIndexFile(_engine.Data, i);
            Console.WriteLine("Seasons index files OK");
            return;
        }

        if (int.TryParse(choice, out var intChoice) &&
            _engine.Data.Seasons.FirstOrDefault(x => x.Id == intChoice) is not null)
        {
            _contentFileGenerator.GenerateSeasonIndexFile(_engine.Data, intChoice);
            Console.WriteLine("Season index file OK");
        }
        else
        {
            Console.WriteLine($"ERROR: No matching season for input: {choice}");
        }
    }

    private static void GeneratePoemsLengthPieChartDataFile()
    {
        _chartDataFileGenerator.GeneratePoemsLengthBarAndPieChartDataFile(_engine.Data);
        Console.WriteLine("Poems length pie chart data file OK");
    }

    private static void GeneratePoemMetricPieChartDataFile()
    {
        _chartDataFileGenerator.GeneratePoemMetricBarAndPieChartDataFile(_engine.Data, null);
        Console.WriteLine("Poem verses length pie chart data file OK");
    }

    private static void GenerateSeasonCategoriesPieChart(MenuItem menuChoice)
    {
        Console.WriteLine(menuChoice.SubMenuItems.First().Label, _engine.Data.Seasons.Count);
        var choice = Console.ReadLine();

        if (choice == "0")
        {
            // Seasons categories' pie
            for (var i = 1; i < _engine.Data.Seasons.Count + 1; i++)
            {
                _chartDataFileGenerator.GenerateSeasonCategoriesPieChartDataFile(_engine.Data, i);
            }           
            
            // General categories' pie
            _chartDataFileGenerator.GenerateSeasonCategoriesPieChartDataFile(_engine.Data, null);
            
            // Year categories' pie
            var currentYear = DateTime.Now.Year;
            for (var y = 1994; y < currentYear + 1; y++)
            {
                _chartDataFileGenerator.GenerateYearCategoriesPieChartDataFile(_engine.Data, y);
            }

            // Categories' and tags' radar
            GeneratePoemsCategoriesAndTagsRadarChartDataFile();

            // Over seasons categories' and tags' bar
            GenerateOverSeasonsCategoriesAndTagsBarChartDataFile();

            Console.WriteLine("All seasons categories pie chart data file OK");
        }
        else if (int.TryParse(choice, out var seasonId) &&
                 _engine.Data.Seasons.FirstOrDefault(x => x.Id == seasonId) is not null)
        {
            // Season categories' pie
            _chartDataFileGenerator.GenerateSeasonCategoriesPieChartDataFile(_engine.Data, seasonId);
            
            // General categories' pie
            _chartDataFileGenerator.GenerateSeasonCategoriesPieChartDataFile(_engine.Data, null);

            Console.WriteLine($"Season {seasonId} categories pie chart data file OK");
        }
        else
        {
            Console.WriteLine($"ERROR: No matching season for input: {seasonId}");
        }
    }

    private static void GenerateDependantChartDataFilesAndCheckQuality(int seasonId, Poem? importedPoem)
    {
        // General poems length
        GeneratePoemsLengthPieChartDataFile();

        // General and season's metric
        GeneratePoemMetricPieChartDataFile();
        _chartDataFileGenerator.GeneratePoemMetricBarAndPieChartDataFile(_engine.Data, seasonId);

        // Season categories' pie
        _chartDataFileGenerator.GenerateSeasonCategoriesPieChartDataFile(_engine.Data, seasonId);
        
        // General categories' pie
        _chartDataFileGenerator.GenerateSeasonCategoriesPieChartDataFile(_engine.Data, null);
        
        // Year categories' pie
        _chartDataFileGenerator.GenerateYearCategoriesPieChartDataFile(_engine.Data, importedPoem.Date.Year);

        Console.WriteLine(seasonId == 0
            ? "All seasons categories pie chart data file OK"
            : $"Season {seasonId} categories pie chart data file OK");

        // Poem by day
        _chartDataFileGenerator.GeneratePoemsByDayRadarChartDataFile(_engine.Data, _engine.DataEn, null, null);
        _chartDataFileGenerator.GeneratePoemsByDayRadarChartDataFile(_engine.Data, _engine.DataEn, null, null,
            forLesMoisExtraTag: true);
        _chartDataFileGenerator.GeneratePoemsByDayRadarChartDataFile(_engine.Data, _engine.DataEn, null, null,
            forNoelExtraTag: true);
        _chartDataFileGenerator.GeneratePoemIntensityPieChartDataFile(_engine.Data, _engine.DataEn);
        _chartDataFileGenerator.GeneratePoemByDayOfWeekPieChartDataFile(_engine.Data, _engine.DataEn);
        Console.WriteLine(
            "Poems by day general and specific, poem intensity, poem by day of week, chart data files OK");

        // Poem interval
        _chartDataFileGenerator.GeneratePoemIntervalBarChartDataFile(_engine.Data, _engine.DataEn, null);
        _chartDataFileGenerator.GeneratePoemIntervalBarChartDataFile(_engine.Data, _engine.DataEn, seasonId);
        Console.WriteLine("Poems interval chart data file OK");

        // Categories' and tags' radar
        GeneratePoemsCategoriesAndTagsRadarChartDataFile();

        // Year tag's radar
        if (importedPoem is not null)
        {
            _chartDataFileGenerator.GeneratePoemsOfYearByDayRadarChartDataFile(_engine.Data, importedPoem.Date.Year);
            Console.WriteLine("Poem's year by day chart data file OK");
        }

        // Poem count
        _contentFileGenerator.GeneratePoemCountFile(_engine.Data);
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
        SeasonChecker.VerifySeasonHaveCorrectPoemCount(_engine.Data);
        _poemMetadataChecker.VerifySeasonHaveCorrectWeightInPoemFile(_engine.Data, seasonId);

        // Les mois
        var output = _customPageChecker.GetPoemWithLesMoisExtraTagNotListedOnCustomPage(importedPoem, _engine.Data);
        if (!string.IsNullOrEmpty(output.FirstOrDefault()))
            Console.WriteLine(output);

        // Ciel
        output = _customPageChecker.GetPoemOfSkyCategoryStartingWithSpecificWordsNotListedOnCustomPage(importedPoem,
            _engine.Data);
        if (!string.IsNullOrEmpty(output.FirstOrDefault()))
            Console.WriteLine(output);

        // Saisons
        output = _customPageChecker.GetPoemOfMoreThanOneSeasonNotListedOnCustomPage(importedPoem, _engine.Data);
        if (!string.IsNullOrEmpty(output.FirstOrDefault()))
            Console.WriteLine(output);

        Console.WriteLine(
            $"Content metadata quality OK. Info: metric last season computed values sum: {_engine.FillMetricDataDict(out var _).Values.Sum(x => x.Last())}");
    }

    private static void GeneratePoemsRadarChartDataFile(MenuItem menuChoice)
    {
        Console.WriteLine(menuChoice.SubMenuItems.First().Label);
        var choice = Console.ReadLine();

        if (string.IsNullOrEmpty(choice))
        {
            _chartDataFileGenerator.GeneratePoemsByDayRadarChartDataFile(_engine.Data, _engine.DataEn, null, null);
            _chartDataFileGenerator.GeneratePoemsByDayRadarChartDataFile(_engine.Data, _engine.DataEn, null, null,
                forLesMoisExtraTag: true);
            _chartDataFileGenerator.GeneratePoemsByDayRadarChartDataFile(_engine.Data, _engine.DataEn, null, null,
                forNoelExtraTag: true);
            _chartDataFileGenerator.GeneratePoemIntensityPieChartDataFile(_engine.Data, _engine.DataEn);
            _chartDataFileGenerator.GeneratePoemByDayOfWeekPieChartDataFile(_engine.Data, _engine.DataEn);
            Console.WriteLine(
                "Poems by day general and specific, poem intensity, poem by day of week, chart data files OK");
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
            _chartDataFileGenerator.GeneratePoemsByDayRadarChartDataFile(_engine.Data, _engine.DataEn, choice, null);
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
        var storageSettings = _configuration!.GetSection(Constants.STORAGE_SETTINGS).Get<StorageSettings>();

        foreach (var category in storageSettings!.Categories.SelectMany(x => x.Subcategories).Select(x => x.Name)
                     .Distinct())
        {
            _chartDataFileGenerator.GeneratePoemsByDayRadarChartDataFile(_engine.Data, _engine.DataEn, category, null);
        }

        Console.WriteLine("Poems by day for all categories chart data files OK");

        foreach (var category in storageSettings.Categories.Select(x => x.Name).Distinct())
        {
            _chartDataFileGenerator.GeneratePoemsByDayRadarChartDataFile(_engine.Data, _engine.DataEn, null, category);
        }

        Console.WriteLine("Poems by day for all tags chart data files OK");
    }

    private static void GenerateOverSeasonsCategoriesAndTagsBarChartDataFile()
    {
        var storageSettings = _configuration!.GetSection(Constants.STORAGE_SETTINGS).Get<StorageSettings>();

        foreach (var category in storageSettings!.SubcategorieNames)
        {
            _chartDataFileGenerator.GenerateOverSeasonsChartDataFile(_engine.Data, category, null);
        }

        Console.WriteLine("Poems over seasons for all categories chart data files OK");

        foreach (var category in storageSettings.Categories.Select(x => x.Name).Distinct())
        {
            _chartDataFileGenerator.GenerateOverSeasonsChartDataFile(_engine.Data, null, category);
        }

        Console.WriteLine("Poems over seasons for all tags chart data files OK");

        _chartDataFileGenerator.GenerateOverSeasonsChartDataFile(_engine.Data, null, null, forAcrostiche: true);
        _chartDataFileGenerator.GenerateOverSeasonsChartDataFile(_engine.Data, null, null, forSonnet: true);
        _chartDataFileGenerator.GenerateOverSeasonsChartDataFile(_engine.Data, null, null, forPantoun: true);
        _chartDataFileGenerator.GenerateOverSeasonsChartDataFile(_engine.Data, null, null, forVariableMetric: true);
        _chartDataFileGenerator.GenerateOverSeasonsChartDataFile(_engine.Data, null, null, forRefrain: true);
        _chartDataFileGenerator.GenerateOverSeasonsChartDataFile(_engine.Data, null, null, forLovecat: true);
        _chartDataFileGenerator.GenerateOverSeasonsChartDataFile(_engine.Data, null, null, forLesMois: true);
        _chartDataFileGenerator.GenerateOverSeasonsChartDataFile(_engine.Data, null, null, forMetric: 1);
        _chartDataFileGenerator.GenerateOverSeasonsChartDataFile(_engine.Data, null, null, forMetric: 2);
        _chartDataFileGenerator.GenerateOverSeasonsChartDataFile(_engine.Data, null, null, forMetric: 3);
        _chartDataFileGenerator.GenerateOverSeasonsChartDataFile(_engine.Data, null, null, forMetric: 4);
        _chartDataFileGenerator.GenerateOverSeasonsChartDataFile(_engine.Data, null, null, forMetric: 5);
        _chartDataFileGenerator.GenerateOverSeasonsChartDataFile(_engine.Data, null, null, forMetric: 6);
        _chartDataFileGenerator.GenerateOverSeasonsChartDataFile(_engine.Data, null, null, forMetric: 7);
        _chartDataFileGenerator.GenerateOverSeasonsChartDataFile(_engine.Data, null, null, forMetric: 8);
        _chartDataFileGenerator.GenerateOverSeasonsChartDataFile(_engine.Data, null, null, forMetric: 9);
        _chartDataFileGenerator.GenerateOverSeasonsChartDataFile(_engine.Data, null, null, forMetric: 10);
        _chartDataFileGenerator.GenerateOverSeasonsChartDataFile(_engine.Data, null, null, forMetric: 11);
        _chartDataFileGenerator.GenerateOverSeasonsChartDataFile(_engine.Data, null, null, forMetric: 12);

        Console.WriteLine(
            "Poems over seasons for 'acrostiche', 'sonnet', 'pantoun', 'métrique variable', 'refrain', 'lovecat', 'les mois', 1-12 metrics chart data files OK");
    }

    private static void GenerateAllSeasonsPoemIntervalBarChartDataFile()
    {
        _chartDataFileGenerator.GeneratePoemIntervalBarChartDataFile(_engine.Data, _engine.DataEn, null);
        for (var i = 1; i < _engine.Data.Seasons.Count + 1; i++)
            _chartDataFileGenerator.GeneratePoemIntervalBarChartDataFile(_engine.Data, _engine.DataEn, i);
        Console.WriteLine("All seasons poems interval chart data files OK");
    }
}