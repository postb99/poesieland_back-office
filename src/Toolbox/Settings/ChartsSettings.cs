namespace Toolbox.Settings;

public class ChartsSettings
{
    public Radar Radar { get; set; }
    
    public Bar Bar { get; set; }
}

public class Radar
{
    public List<RadarItem> ByDayExtraTags { get; set; } = new();
}

public class Bar
{
    public List<BarItem> OverSeasonsItems { get; set; } = new();
}

public class BarItem
{
    public string Name { get; set; }
    
    public BarItemType Type { get; set; }
    
    public BarItem? StackedItem { get; set; }
}

public enum BarItemType
{
    Category,
    SubCategory,
    ExtraTag
}

public class RadarItem
{
    public string Name { get; set; }
    
    public string? Color { get; set; }
}