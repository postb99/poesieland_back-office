using Microsoft.Extensions.Configuration;
using Toolbox.Charts;
using Toolbox.Consistency;
using Toolbox.Domain;
using Toolbox.Generators;
using Toolbox.Importers;
using Toolbox.Information;
using Toolbox.Persistence;
using Toolbox.Settings;

namespace Toolbox;

public class Program
{
    private static IConfiguration? _configuration;
    private static MainMenuSettings? _mainMenuSettings;
    private static DataManager? _dataManager;
    private static ContentFileGenerator _contentFileGenerator;
    private static PoemImporter _poemImporter;
    private static SeasonMetadataImporter _seasonMetadataImporter;
    private static CustomPageChecker _customPageChecker;
    private static YamlMetadataChecker _yamlMetadataChecker;
    private static ChartDataFileGenerator _chartDataFileGenerator;
    private static PoemMetadataChecker _poemMetadataChecker;
    
    private static Root _data;
    private static Root _dataEn;

    public static async Task Main(string[] args)
    {
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        _configuration = configurationBuilder.Build();
        _mainMenuSettings = _configuration.GetSection(Constants.MAIN_MENU).Get<MainMenuSettings>();
        
        _dataManager = new DataManager(_configuration);
        _dataManager.Load(out _data, out _dataEn);
        
        _contentFileGenerator = new ContentFileGenerator(_configuration);
        _poemImporter = new PoemImporter(_configuration);
        _seasonMetadataImporter = new SeasonMetadataImporter(_configuration);
        _customPageChecker = new CustomPageChecker(_configuration);
        _chartDataFileGenerator = new ChartDataFileGenerator(_configuration);
        _poemMetadataChecker = new PoemMetadataChecker(_configuration, _poemImporter);

        _yamlMetadataChecker = new YamlMetadataChecker(_configuration, _data);

        var menuEntry = MainMenu();
        await ValidateAndPerformMenuChoiceAsync(null, menuEntry);
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

    private static async Task ValidateAndPerformMenuChoiceAsync(MenuItem? parentMenuItem, string input)
    {
        MenuItem? menuChoice = ValidateMenuEntry(parentMenuItem, input);
        while (menuChoice is null)
        {
            Console.WriteLine("ERROR: No such choice");
            input = Console.ReadLine();
            menuChoice = ValidateMenuEntry(parentMenuItem, input);
        }

        if (!(await PerformActionAsync(menuChoice))) return;
        Console.WriteLine();
        Console.WriteLine("Back to main menu");
        var menuEntry = MainMenu();
        await ValidateAndPerformMenuChoiceAsync(null, menuEntry);
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

    private static async Task<bool> PerformActionAsync(MenuItem menuChoice)
    {
        switch ((MainMenuSettings.MenuChoices)menuChoice.Key)
        {
            case MainMenuSettings.MenuChoices.GenerateSeasonIndexFile:
                GenerateSeasonIndexFiles(menuChoice);
                break;
            case MainMenuSettings.MenuChoices.GeneratePoemFiles:
            case MainMenuSettings.MenuChoices.Import:
            case MainMenuSettings.MenuChoices.GenerateChartsDataFiles:
                await ValidateAndPerformMenuChoiceAsync(menuChoice, MenuChoice(menuChoice.SubMenuItems));
                return false;
            case MainMenuSettings.MenuChoices.GenerateSinglePoem:
                GeneratePoemContentFile(menuChoice);
                break;
            case MainMenuSettings.MenuChoices.ImportSinglePoem:
                await ImportPoemContentFileAsync(menuChoice);
                break;
            case MainMenuSettings.MenuChoices.GeneratePoemsOfASeason:
                GenerateSeasonPoemContentFiles(menuChoice);
                break;
            case MainMenuSettings.MenuChoices.ImportPoemsOfASeason:
                await ImportSeasonPoemContentFilesAsync(menuChoice);
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
                _chartDataFileGenerator.GenerateCategoriesBubbleChartDataFile(_data);
                _chartDataFileGenerator.GenerateCategoryMetricBubbleChartDataFile(_data);
                break;
            case MainMenuSettings.MenuChoices.ReloadDataFile:
                _dataManager.Load(out _data, out _dataEn);
                break;
            case MainMenuSettings.MenuChoices.CheckContentMetadataQuality:
                PoemMetadataChecker.CheckPoemsWithoutMetricSpecified(_data);
                Console.WriteLine("Poems without metric specified checked");
                PoemMetadataChecker.CheckPoemsWithVariableMetricNotPresentInInfo(_data);
                Console.WriteLine("Poems with variable metric not present in info checked");
                SeasonChecker.VerifySeasonHaveCorrectPoemCount(_data);
                Console.WriteLine("Seasons with incorrect poem count checked");
                _poemMetadataChecker.VerifySeasonHaveCorrectWeightInPoemFile(_data, null);
                var outputs = await _yamlMetadataChecker.GetYamlMetadataAnomaliesAcrossSeasonsAsync().ToListAsync();
                foreach (var output in outputs)
                {
                    Console.WriteLine(output);
                }
                Console.WriteLine("YAML metadata checked for all poems since season 21");
                // Custom pages
                // Les mois
                // TODO put back output
                _customPageChecker.GetPoemWithLesMoisExtraTagNotListedOnCustomPage(null, _data);
                // outputs = _customPageChecker.GetPoemWithLesMoisExtraTagNotListedOnCustomPage(null, _data);
                // foreach (var output in outputs)
                // {
                //     Console.WriteLine(output);
                // }

                // Ciel
                // TODO put back output
                _customPageChecker.GetPoemOfSkyCategoryStartingWithSpecificWordsNotListedOnCustomPage(null, _data);
                // outputs = _customPageChecker.GetPoemOfSkyCategoryStartingWithSpecificWordsNotListedOnCustomPage(null,
                //     _data);
                // foreach (var output in outputs)
                // {
                //     Console.WriteLine(output);
                // }

                // Saisons
                // TODO put back output
                _customPageChecker.GetPoemOfMoreThanOneSeasonNotListedOnCustomPage(null, _data);
                // outputs = _customPageChecker.GetPoemOfMoreThanOneSeasonNotListedOnCustomPage(null, _data);
                // foreach (var output in outputs)
                // {
                //     Console.WriteLine(output);
                // }

                Console.WriteLine(
                    $"Metric last season computed values sum: {ChartDataFileHelper.FillMetricDataDict(_data, out var _).Values.Sum(x => x.Last())}");

                Console.WriteLine("Content metadata quality OK");
                break;
            case MainMenuSettings.MenuChoices.GenerateAllSeasonsPoemIntervalBarChartDataFile:
                GenerateAllSeasonsPoemIntervalBarChartDataFile();
                break;
            case MainMenuSettings.MenuChoices.ImportEnPoems:
                ImportEnPoemsContentFiles();
                break;
            case MainMenuSettings.MenuChoices.OutputSeasonsDuration:
                SeasonDurationOutputHelper.OutputSeasonsDuration(_data);
                break;
            case MainMenuSettings.MenuChoices.OutputReusedTitles:
                var reusedTitles = new ReusedTitlesChecker(_data).GetReusedTitles();
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
        Console.WriteLine(menuChoice.SubMenuItems.First().Label, _data.Seasons.Count);
        var choice = Console.ReadLine();
        if (choice == "0")
        {
            GenerateAllPoemsContentFiles();
            return;
        }

        if (int.TryParse(choice, out var intChoice) &&
            _data.Seasons.FirstOrDefault(x => x.Id == intChoice) is not null)
        {
            _contentFileGenerator.GenerateSeasonAllPoemFiles(_data, intChoice);
            Console.WriteLine("Poem content files OK");
        }
        else
        {
            Console.WriteLine($"ERROR: No matching season for input: {choice}");
        }
    }

    private static async Task ImportSeasonPoemContentFilesAsync(MenuItem menuChoice)
    {
        Console.WriteLine(menuChoice.SubMenuItems.First().Label, _data.Seasons.Count);
        var choice = Console.ReadLine();
        if (choice == "0")
        {
            await ImportAllPoemsContentFilesAsync();
            return;
        }

        if (int.TryParse(choice, out var seasonId) &&
            _data.Seasons.FirstOrDefault(x => x.Id == seasonId) is not null)
        {
            await _poemImporter.ImportPoemsOfSeasonAsync(seasonId, _data);
            _dataManager!.Save(_data);
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
        Console.WriteLine(menuChoice.SubMenuItems.First().Label, _data.Seasons.Count);
        var choice = Console.ReadLine();

        if (int.TryParse(choice, out var seasonId) &&
            _data.Seasons.FirstOrDefault(x => x.Id == seasonId) is not null)
        {
            _seasonMetadataImporter.ImportSeasonMetadata(seasonId, _data);
            _dataManager.Save(_data);
            Console.WriteLine("Season metadata import OK");
        }
        else
        {
            Console.WriteLine($"ERROR: No matching season for input: {choice}");
        }
    }

    private static void ImportEnPoemsContentFiles()
    {
        _poemImporter.ImportPoemsEn(_dataEn);
        _dataManager.SaveEn(_dataEn);
        Console.WriteLine("Poems import OK");

        _contentFileGenerator.GeneratePoemEnCountFile(_dataEn);
        Console.WriteLine("Poems count OK");

        _chartDataFileGenerator.GeneratePoemsEnByDayRadarChartDataFile(_dataEn);
        _chartDataFileGenerator.GeneratePoemsByDayRadarChartDataFile(_data, _dataEn, null, null);
        Console.WriteLine("Charts for day radar OK");

        _chartDataFileGenerator.GeneratePoemIntensityPieChartDataFile(_data, _dataEn);
        Console.WriteLine("Poem intensity chart OK");

        _chartDataFileGenerator.GenerateEnPoemByDayOfWeekPieChartDataFile(_dataEn);
        _chartDataFileGenerator.GeneratePoemByDayOfWeekPieChartDataFile(_data, _dataEn);
        Console.WriteLine("Chart for day of week OK");

        _chartDataFileGenerator.GeneratePoemIntervalBarChartDataFile(_data, _dataEn, null);
        Console.WriteLine("Poem interval bar chart OK");
        GenerateAllSeasonsPoemIntervalBarChartDataFile();
    }

    private static void GeneratePoemContentFile(MenuItem menuChoice)
    {
        Console.WriteLine(menuChoice.SubMenuItems.First().Label);
        var poemId = Console.ReadLine();

        var poem = _data.Seasons.SelectMany(x => x.Poems).FirstOrDefault(x => x.Id == poemId);
        if (poem is not null)
        {
            _contentFileGenerator.GeneratePoemFile(_data, poem);
            Console.WriteLine("Poem content file OK");
        }
        else
        {
            Console.WriteLine($"ERROR: No matching poem for input: {poemId}");
        }
    }

    private static async Task ImportPoemContentFileAsync(MenuItem menuChoice)
    {
        Console.WriteLine(menuChoice.SubMenuItems.First().Label);
        var poemId = Console.ReadLine();

        try
        {
            var importedPoem = await _poemImporter.ImportPoemAsync(poemId, _data);
            _dataManager!.Save(_data);
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

    private static async Task ImportAllPoemsContentFilesAsync()
    {
        var seasonCount = _data.Seasons.Count;
        for (var i = 1; i <= seasonCount; i++)
        {
            await _poemImporter.ImportPoemsOfSeasonAsync(i, _data);
            GenerateDependantChartDataFilesAndCheckQuality(i, null);
        }

        _dataManager!.Save(_data);
        Console.WriteLine("All poems import OK");
    }

    private static void GenerateAllPoemsContentFiles()
    {
        _contentFileGenerator.GenerateAllPoemFiles(_data);
        Console.WriteLine("All poem content files OK");
    }

    private static void GenerateSeasonIndexFiles(MenuItem menuChoice)
    {
        var seasonCount = _data.Seasons.Count;
        Console.WriteLine(menuChoice.SubMenuItems.First().Label, seasonCount);
        var choice = Console.ReadLine();
        if (choice == "0")
        {
            for (var i = 1; i <= seasonCount; i++)
                _contentFileGenerator.GenerateSeasonIndexFile(_data, i);
            Console.WriteLine("Seasons index files OK");
            return;
        }

        if (int.TryParse(choice, out var intChoice) &&
            _data.Seasons.FirstOrDefault(x => x.Id == intChoice) is not null)
        {
            _contentFileGenerator.GenerateSeasonIndexFile(_data, intChoice);
            Console.WriteLine("Season index file OK");
        }
        else
        {
            Console.WriteLine($"ERROR: No matching season for input: {choice}");
        }
    }

    private static void GeneratePoemsLengthPieChartDataFile()
    {
        _chartDataFileGenerator.GeneratePoemsLengthBarAndPieChartDataFile(_data);
        Console.WriteLine("Poems length pie chart data file OK");
    }

    private static void GeneratePoemMetricPieChartDataFile()
    {
        _chartDataFileGenerator.GeneratePoemMetricBarAndPieChartDataFile(_data, null);
        Console.WriteLine("Poem verses length pie chart data file OK");
    }

    private static void GenerateSeasonCategoriesPieChart(MenuItem menuChoice)
    {
        Console.WriteLine(menuChoice.SubMenuItems.First().Label, _data.Seasons.Count);
        var choice = Console.ReadLine();

        if (choice == "0")
        {
            // Seasons categories' pie
            for (var i = 1; i < _data.Seasons.Count + 1; i++)
            {
                _chartDataFileGenerator.GenerateSeasonCategoriesPieChartDataFile(_data, i);
            }

            // General categories' pie
            _chartDataFileGenerator.GenerateSeasonCategoriesPieChartDataFile(_data, null);

            // Year categories' pie
            var currentYear = DateTime.Now.Year;
            for (var y = 1994; y < currentYear + 1; y++)
            {
                _chartDataFileGenerator.GenerateYearCategoriesPieChartDataFile(_data, y);
            }

            // Categories' and tags' radar
            GeneratePoemsCategoriesAndTagsRadarChartDataFile();

            // Over seasons categories' and tags' bar
            GenerateOverSeasonsCategoriesAndTagsBarChartDataFile();

            Console.WriteLine("All seasons categories pie chart data file OK");
        }
        else if (int.TryParse(choice, out var seasonId) &&
                 _data.Seasons.FirstOrDefault(x => x.Id == seasonId) is not null)
        {
            // Season categories' pie
            _chartDataFileGenerator.GenerateSeasonCategoriesPieChartDataFile(_data, seasonId);

            // General categories' pie
            _chartDataFileGenerator.GenerateSeasonCategoriesPieChartDataFile(_data, null);

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
        _chartDataFileGenerator.GeneratePoemMetricBarAndPieChartDataFile(_data, seasonId);

        // Season categories' pie
        _chartDataFileGenerator.GenerateSeasonCategoriesPieChartDataFile(_data, seasonId);

        // General categories' pie
        _chartDataFileGenerator.GenerateSeasonCategoriesPieChartDataFile(_data, null);

        // Year categories' pie
        if (importedPoem is not null)
            _chartDataFileGenerator.GenerateYearCategoriesPieChartDataFile(_data, importedPoem.Date.Year);

        Console.WriteLine(seasonId == 0
            ? "All seasons categories pie chart data file OK"
            : $"Season {seasonId} categories pie chart data file OK");

        // Poem by day
        _chartDataFileGenerator.GeneratePoemsByDayRadarChartDataFile(_data, _dataEn, null, null);
        _chartDataFileGenerator.GeneratePoemsByDayRadarChartDataFile(_data, _dataEn, null, null,
            forLesMoisExtraTag: true);
        _chartDataFileGenerator.GeneratePoemsByDayRadarChartDataFile(_data, _dataEn, null, null,
            forNoelExtraTag: true);
        _chartDataFileGenerator.GeneratePoemsByDayRadarChartDataFile(_data, _dataEn, null, null,
            forLaMortExtraTag: true);
        _chartDataFileGenerator.GeneratePoemIntensityPieChartDataFile(_data, _dataEn);
        _chartDataFileGenerator.GeneratePoemByDayOfWeekPieChartDataFile(_data, _dataEn);
        Console.WriteLine(
            "Poems by day general and specific, poem intensity, poem by day of week, chart data files OK");

        // Poem interval
        _chartDataFileGenerator.GeneratePoemIntervalBarChartDataFile(_data, _dataEn, null);
        _chartDataFileGenerator.GeneratePoemIntervalBarChartDataFile(_data, _dataEn, seasonId);
        Console.WriteLine("Poems interval chart data file OK");

        // Categories' and tags' radar
        GeneratePoemsCategoriesAndTagsRadarChartDataFile();

        // Year tag's radar
        if (importedPoem is not null)
        {
            _chartDataFileGenerator.GeneratePoemsOfYearByDayRadarChartDataFile(_data, importedPoem.Date.Year);
            Console.WriteLine("Poem's year by day chart data file OK");
        }

        // Poem count
        _contentFileGenerator.GeneratePoemCountFile(_data);
        Console.WriteLine("Poem count file OK");

        // Poem length by metric and vice versa
        _chartDataFileGenerator.GeneratePoemLengthByVerseLengthBubbleChartDataFile(_data);
        Console.WriteLine("Poems bubble chart data files OK");

        // Over seasons categories', tags' bar, verse length's line
        GenerateOverSeasonsCategoriesAndTagsBarChartDataFile();
        GenerateOverSeasonsVerseLengthLineChartDataFile();

        // Categories bubble chart
        _chartDataFileGenerator.GenerateCategoriesBubbleChartDataFile(_data);
        // Category metric bubble chart
        _chartDataFileGenerator.GenerateCategoryMetricBubbleChartDataFile(_data);

        // And check data quality
        SeasonChecker.VerifySeasonHaveCorrectPoemCount(_data);
        _poemMetadataChecker.VerifySeasonHaveCorrectWeightInPoemFile(_data, seasonId);

        if (importedPoem is not null)
        {
            // Check custom pages
            // Les mois
            var output = _customPageChecker.GetPoemWithLesMoisExtraTagNotListedOnCustomPage(importedPoem, _data);
            if (!string.IsNullOrEmpty(output.FirstOrDefault()))
                Console.WriteLine(output);

            // Ciel
            output = _customPageChecker.GetPoemOfSkyCategoryStartingWithSpecificWordsNotListedOnCustomPage(importedPoem,
                _data);
            if (!string.IsNullOrEmpty(output.FirstOrDefault()))
                Console.WriteLine(output);

            // Saisons
            output = _customPageChecker.GetPoemOfMoreThanOneSeasonNotListedOnCustomPage(importedPoem, _data);
            if (!string.IsNullOrEmpty(output.FirstOrDefault()))
                Console.WriteLine(output);
        }

        Console.WriteLine(
            $"Content metadata quality OK. Info: metric last season computed values sum: {ChartDataFileHelper.FillMetricDataDict(_data, out _).Values.Sum(x => x.Last())}");
    }

    private static void GeneratePoemsRadarChartDataFile(MenuItem menuChoice)
    {
        Console.WriteLine(menuChoice.SubMenuItems.First().Label);
        var choice = Console.ReadLine();

        if (string.IsNullOrEmpty(choice))
        {
            _chartDataFileGenerator.GeneratePoemsByDayRadarChartDataFile(_data, _dataEn, null, null);
            _chartDataFileGenerator.GeneratePoemsByDayRadarChartDataFile(_data, _dataEn, null, null,
                forLesMoisExtraTag: true);
            _chartDataFileGenerator.GeneratePoemsByDayRadarChartDataFile(_data, _dataEn, null, null,
                forNoelExtraTag: true);
            _chartDataFileGenerator.GeneratePoemsByDayRadarChartDataFile(_data, _dataEn, null, null,
                forLaMortExtraTag: true);
            _chartDataFileGenerator.GeneratePoemIntensityPieChartDataFile(_data, _dataEn);
            _chartDataFileGenerator.GeneratePoemByDayOfWeekPieChartDataFile(_data, _dataEn);
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
            _chartDataFileGenerator.GeneratePoemsByDayRadarChartDataFile(_data, _dataEn, choice, null);
            Console.WriteLine($"Poems by day for '{choice}' chart data file OK");
        }
    }

    private static void GenerateBubbleChartDataFile()
    {
        _chartDataFileGenerator.GeneratePoemLengthByVerseLengthBubbleChartDataFile(_data);
        Console.WriteLine("Bubble chart data file OK");
    }

    private static void GenerateOverSeasonsVerseLengthLineChartDataFile()
    {
        _chartDataFileGenerator.GenerateOverSeasonsMetricLineChartDataFile(_data);
        Console.WriteLine("Line chart data file OK");
    }

    private static void GeneratePoemsCategoriesAndTagsRadarChartDataFile()
    {
        var storageSettings = _configuration!.GetSection(Constants.STORAGE_SETTINGS).Get<StorageSettings>();

        foreach (var category in storageSettings!.Categories.SelectMany(x => x.Subcategories).Select(x => x.Name)
                     .Distinct())
        {
            _chartDataFileGenerator.GeneratePoemsByDayRadarChartDataFile(_data, _dataEn, category, null);
        }

        Console.WriteLine("Poems by day for all categories chart data files OK");

        foreach (var category in storageSettings.Categories.Select(x => x.Name).Distinct())
        {
            _chartDataFileGenerator.GeneratePoemsByDayRadarChartDataFile(_data, _dataEn, null, category);
        }

        Console.WriteLine("Poems by day for all tags chart data files OK");
    }

    private static void GenerateOverSeasonsCategoriesAndTagsBarChartDataFile()
    {
        var storageSettings = _configuration!.GetSection(Constants.STORAGE_SETTINGS).Get<StorageSettings>();

        foreach (var category in storageSettings!.SubcategorieNames)
        {
            _chartDataFileGenerator.GenerateOverSeasonsChartDataFile(_data, category, null);
        }

        Console.WriteLine("Poems over seasons for all categories chart data files OK");

        foreach (var category in storageSettings.Categories.Select(x => x.Name).Distinct())
        {
            _chartDataFileGenerator.GenerateOverSeasonsChartDataFile(_data, null, category);
        }

        Console.WriteLine("Poems over seasons for all tags chart data files OK");

        _chartDataFileGenerator.GenerateOverSeasonsChartDataFile(_data, null, null, forAcrostiche: true);
        _chartDataFileGenerator.GenerateOverSeasonsChartDataFile(_data, null, null, forSonnet: true);
        _chartDataFileGenerator.GenerateOverSeasonsChartDataFile(_data, null, null, forPantoun: true);
        _chartDataFileGenerator.GenerateOverSeasonsChartDataFile(_data, null, null, forVariableMetric: true);
        _chartDataFileGenerator.GenerateOverSeasonsChartDataFile(_data, null, null, forRefrain: true);
        _chartDataFileGenerator.GenerateOverSeasonsChartDataFile(_data, null, null, forLovecat: true);
        _chartDataFileGenerator.GenerateOverSeasonsChartDataFile(_data, null, null, forLesMois: true);
        _chartDataFileGenerator.GenerateOverSeasonsChartDataFile(_data, null, null, forLaMort: true);
        _chartDataFileGenerator.GenerateOverSeasonsChartDataFile(_data, null, null, forMetric: 1);
        _chartDataFileGenerator.GenerateOverSeasonsChartDataFile(_data, null, null, forMetric: 2);
        _chartDataFileGenerator.GenerateOverSeasonsChartDataFile(_data, null, null, forMetric: 3);
        _chartDataFileGenerator.GenerateOverSeasonsChartDataFile(_data, null, null, forMetric: 4);
        _chartDataFileGenerator.GenerateOverSeasonsChartDataFile(_data, null, null, forMetric: 5);
        _chartDataFileGenerator.GenerateOverSeasonsChartDataFile(_data, null, null, forMetric: 6);
        _chartDataFileGenerator.GenerateOverSeasonsChartDataFile(_data, null, null, forMetric: 7);
        _chartDataFileGenerator.GenerateOverSeasonsChartDataFile(_data, null, null, forMetric: 8);
        _chartDataFileGenerator.GenerateOverSeasonsChartDataFile(_data, null, null, forMetric: 9);
        _chartDataFileGenerator.GenerateOverSeasonsChartDataFile(_data, null, null, forMetric: 10);
        _chartDataFileGenerator.GenerateOverSeasonsChartDataFile(_data, null, null, forMetric: 11);
        _chartDataFileGenerator.GenerateOverSeasonsChartDataFile(_data, null, null, forMetric: 12);

        Console.WriteLine(
            "Poems over seasons for 'acrostiche', 'sonnet', 'pantoun', 'métrique variable', 'refrain', 'lovecat', 'les mois', 'la mort', 1-12 metrics chart data files OK");
    }

    private static void GenerateAllSeasonsPoemIntervalBarChartDataFile()
    {
        _chartDataFileGenerator.GeneratePoemIntervalBarChartDataFile(_data, _dataEn, null);
        for (var i = 1; i < _data.Seasons.Count + 1; i++)
            _chartDataFileGenerator.GeneratePoemIntervalBarChartDataFile(_data, _dataEn, i);
        Console.WriteLine("All seasons poems interval chart data files OK");
    }
}
