namespace Toolbox;

public class MainMenuSettings
{
    public enum MenuChoices
    {
        GenerateSeasonIndexFile = 1
    }
    
    public List<KeyValuePair<string, string>> MenuItems { get; set; }
}