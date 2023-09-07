using Microsoft.Extensions.Configuration;
using Toolbox;

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

        _engine = new Engine(_configuration);
        _engine.Load();

        Tuple<MainMenuSettings.MenuChoices, MenuItem>? menuChoice = null;
        do
        {
            var menuEntry = MainMenu();
            menuChoice = ValidateMainMenuEntry(menuEntry);
        } while (menuChoice == null);

        PerformAction(menuChoice);
    }

    private static string MainMenu()
    {
        _mainMenuSettings = _configuration.GetSection(Settings.MAIN_MENU).Get<MainMenuSettings>();
        foreach (var menuItem in _mainMenuSettings.MenuItems)
        {
            Console.WriteLine($"[{menuItem.Key}] {menuItem.Label}");
        }

        Console.WriteLine("Choice:");
        return Console.ReadLine();
    }

    private static Tuple<MainMenuSettings.MenuChoices, MenuItem>? ValidateMainMenuEntry(string entry)
    {
        var ok = Enum.TryParse<MainMenuSettings.MenuChoices>(entry, true, out var menuChoice);
        if (ok)
        {
            var menuItem = _mainMenuSettings.MenuItems.FirstOrDefault(x => x.Key == (int)menuChoice);
            return new Tuple<MainMenuSettings.MenuChoices, MenuItem>(menuChoice, menuItem);
        }

        return null;
    }

    private static void PerformAction(Tuple<MainMenuSettings.MenuChoices, MenuItem> menuChoice)
    {
        switch (menuChoice.Item1)
        {
            case MainMenuSettings.MenuChoices.GenerateSeasonIndexFile:
                GenerateSeasonIndexFiles(menuChoice);
                break;
        }
    }

    private static void GenerateSeasonIndexFiles(Tuple<MainMenuSettings.MenuChoices, MenuItem> menuChoice)
    {
        Console.WriteLine(menuChoice.Item2.SubMenuItems.First().Label, _engine.Data.Seasons.Count);
        var choice = Console.ReadLine();
        if (choice == "0")
        {
            _engine.GenerateAllSeasonsIndexFile();
            return;
        }

        if (int.TryParse(choice, out var intChoice))
        {
            _engine.GenerateSeasonIndexFile(intChoice);
        }
        else
        {
            Console.WriteLine("No matching season for input");
        }
    }
}