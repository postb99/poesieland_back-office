namespace Toolbox.Settings;

public class MetricSettings
{
    public List<Metric> Metrics { get; set; } = [];
}

public class Metric
{
    public required string Name { get; set; }
    
    public int Length { get; set; }
    
    public required string Color { get; set; }
}
