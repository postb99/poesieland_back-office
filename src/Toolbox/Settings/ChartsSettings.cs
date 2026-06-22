namespace Toolbox.Settings;

public class ChartsSettings
{
    public Radar Radar { get; set; }
}

public class Radar
{
    public List<string> ByDayExtraTags { get; set; } = new();
}