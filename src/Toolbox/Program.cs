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
            case MainMenuSettings.MenuChoices.ReloadDataFile:
                _engine.Load();
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

        if (int.TryParse(choice, out var intChoice))
        {
            _engine.ImportSeason(intChoice);
            Console.WriteLine("Season import OK");
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

        var ok = _engine.ImportPoem(poemId);
        if (ok)
        {
            Console.WriteLine("Poem import OK");
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
}