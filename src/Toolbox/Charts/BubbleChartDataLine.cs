namespace Toolbox.Charts;

/// <summary>
/// Description of data for a Chart.js bubble chart point element: x, y, value and label.
/// </summary>
public record BubbleChartDataLine(int X, int Y, string Value, string RgbaColor)
{
    /// <summary>
    /// Bubble radius in pixels, with dot for decimal separator.
    /// </summary>
    public string Value { get; } = Value;
}