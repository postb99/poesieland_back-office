namespace Toolbox.Charts;

/// <summary>
/// Description of data for a Chart.js bar, pie, or radar chart point element: value and label, with custom color.
/// RgbaColor sample value: "rgb(255, 205, 86)", "rgba(255, 205, 86, 0.5)".
/// </summary>
public record ColoredDataLine(string Label, int Value, string RgbaColor) : DataLine(Label, Value)
{
    public override bool DefaultColor => false;
}