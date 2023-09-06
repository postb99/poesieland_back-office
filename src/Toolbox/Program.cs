using Microsoft.Extensions.Configuration;
using Toolbox;

public class Program
{
    private static IConfiguration _configuration;
    private static Engine _engine;

    public static void Main(string[] args)
    {
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        _configuration = configurationBuilder.Build();

        _engine = new Engine(_configuration);
        _engine.Load();

        MainMenuSettings.MenuChoices? menuChoice = null;
        do
        {
            var menuEntry = MainMenu();
            menuChoice = ValidateMainMenuEntry(menuEntry);
        } while (menuChoice == null);
    }

    private static char MainMenu()
    {
        var mainMenuSettings = _configuration.GetSection(Settings.MAIN_MENU).Get<MainMenuSettings>(); // FIXME
        foreach (var menuItem in mainMenuSettings.MenuItems)
        {
            Console.WriteLine($"[{menuItem.Key}] {menuItem.Value}");
        }

        Console.WriteLine("Choice:");
        return Console.ReadKey().KeyChar;
    }

    private static MainMenuSettings.MenuChoices? ValidateMainMenuEntry(char entry)
    {
        var ok = Enum.TryParse<MainMenuSettings.MenuChoices>(entry.ToString(), true, out var menuChoice);
        return ok ? menuChoice : null;
    }

    private static void PerformAction(MainMenuSettings.MenuChoices menuChoice)
    {
        switch (menuChoice)
        {
            case MainMenuSettings.MenuChoices.GenerateSeasonIndexFile:
                _engine.GenerateSeasonIndexFile(1); // TODO submenu configuration
                break;
        }
    }
}