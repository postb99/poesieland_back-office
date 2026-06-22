namespace Toolbox.Settings;

public class ChartsSettings
{
    public Radar Radar { get; set; }
    
    public Bar Bar { get; set; }
}

public class Radar
{
    public List<string> ByDayExtraTags { get; set; } = new();
}

public class Bar
{
    public List<string> OverSeasonsExtraTags { get; set; } = new();
}