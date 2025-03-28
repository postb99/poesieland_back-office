namespace Toolbox.Settings;

public class MetricSettings
{
    public List<Metric> Metrics { get; set; } = [];
}

public class Metric
{
    public string Name { get; set; } = string.Empty;
    
    public int Length { get; set; } = 0;
    
    public string Color { get; set; } = string.Empty;
}
