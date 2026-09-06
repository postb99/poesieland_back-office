namespace Toolbox.Settings;

public class WordCloudSettings
{
    public string GeneratorUrl { get; set; } = "https://nuagedemots.co/";

    // A visible browser makes upstream UI changes and anti-bot prompts actionable.
    public bool Headless { get; set; }

    public string DownloadEmail => $"arcenciel{new Random().Next(100000, 999999).ToString()}@yopmail.com";
}
