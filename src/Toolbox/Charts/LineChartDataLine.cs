namespace Toolbox.Charts;

/// <summary>
/// Description of data for a Chart.js line chart point element: values and label, with custom color.
/// </summary>
public record LineChartDataLine(string Label, List<decimal> Values, string RgbaColor);