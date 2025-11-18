namespace Toolbox.Charts;

/// <summary>
/// Description of data for a Chart.js bar or radar chart point element: value and label.
/// </summary>
public record DataLine(string Label, int Value)
{
    public virtual bool DefaultColor => true;
}