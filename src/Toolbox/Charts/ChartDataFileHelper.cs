using System.Globalization;
using System.Text;
namespace Toolbox.Charts;

public class ChartDataFileHelper(StreamWriter streamWriter, ChartType chartType, int nbDatasets = 1)
{
    /// <summary>Writes the JavaScript imports and opens the chart data array.</summary>
    public void WriteBeforeData()
    {
        switch (chartType)
        {
            case ChartType.Bar:
                streamWriter.WriteLine("import { addBarChart } from '../add-chart.js'");
                break;
            case ChartType.Pie:
                streamWriter.WriteLine("import { addPieChart } from '../add-chart.js'");
                break;
            case ChartType.Radar:
                streamWriter.WriteLine("import { addRadarChart } from '../add-chart.js'");
                break;
            case ChartType.Bubble:
                streamWriter.WriteLine("import { addBubbleChart } from '../add-chart.js'");
                break;
            case ChartType.Line:
                streamWriter.WriteLine("import { addLineChart } from '../add-chart.js'");
                break;
        }

        streamWriter.WriteLine("(async function () {");
        streamWriter.WriteLine("  const data = [");

        streamWriter.Flush();
    }

    /// <summary>
    /// <summary>Writes the chart-specific JavaScript call after the data array.</summary>
    /// </summary>
    /// <param name="chartId">Mandatory and unique</param>
    /// <param name="chartTitles">Used for bar, pie (but single), line and bubble charts</param>
    /// <param name="radarChartBorderColor">Radar chart option</param>
    /// <param name="radarChartBackgroundColor">Radar chart option</param>
    /// <param name="chartXAxisTitle">Bubble chart option</param>
    /// <param name="chartYAxisTitle">Bubble chart option</param>
    /// <param name="xAxisStep">Bubble chart option</param>
    /// <param name="yAxisStep">Bubble chart option</param>
    /// <param name="xLabels">Option for line chart</param>
    /// <param name="stack">Option for line chart</param>
    /// <param name="customScalesOptions">Option for bar, line, bubble chart: "scales: { ... }.
    /// NB: chartXAxisTitle, chartYAxisTitle, xAxisStep, yAxisStep options will be ignored"</param>
    public void WriteAfterData(string chartId, string[] chartTitles, string? radarChartBorderColor = null,
        string? radarChartBackgroundColor = null, string chartXAxisTitle = "",
        string chartYAxisTitle = "", int xAxisStep = 1, int yAxisStep = 1, string[]? xLabels = null,
        string? stack = null,
        string? customScalesOptions = null)
    {
        if (customScalesOptions?.Count(x => x == '{') != customScalesOptions?.Count(x => x == '}'))
            throw new ArgumentException("Not the same number of { and } for custom scale options!");

        streamWriter.WriteLine("  ];");

        var chartTitlesBuilder = new StringBuilder();
        foreach (var chartTitle in chartTitles)
        {
            chartTitlesBuilder.Append('\'').Append(chartTitle.JavaScriptString()).Append("',");
        }

        if (chartTitlesBuilder.Length > 0) chartTitlesBuilder.Length--;

        switch (chartType)
        {
            case ChartType.Bar:
                streamWriter.WriteLine(nbDatasets == 1
                    ? $"    addBarChart('{chartId.JavaScriptString()}', [{chartTitlesBuilder}], [data], {{{customScalesOptions ?? ""}}});"
                    : $"    addBarChart('{chartId.JavaScriptString()}', [{chartTitlesBuilder}], data, {{{customScalesOptions ?? ""}}});");
                break;
            case ChartType.Pie:
                streamWriter.WriteLine(
                    $"  addPieChart('{chartId.JavaScriptString()}', [data], {{ plugins: {{ title: {{ display: true, text: '{chartTitles[0].JavaScriptString()}' }} }} }});");
                break;
            case ChartType.Radar:
                var backgroundColor = string.IsNullOrEmpty(radarChartBackgroundColor)
                    ? "rgba(76, 201, 240)"
                    : radarChartBackgroundColor;
                var borderColor = string.IsNullOrEmpty(radarChartBorderColor)
                    ? "rgba(0, 0, 0, 0.1)"
                    : radarChartBorderColor;

                streamWriter.WriteLine(
                    $"  addRadarChart('{chartId.JavaScriptString()}', ['{chartTitles[0].JavaScriptString()}'], [data], {{ backgroundColor: '{backgroundColor}', borderColor: '{borderColor}', pointBackgroundColor: '{borderColor}', pointBorderColor: '#fff', pointHoverBackgroundColor: '#fff', pointHoverBorderColor: 'rgb(54, 162, 235)', elements: {{ line: {{ borderWidth: 1  }} }}, scales: {{ r: {{ ticks: {{ stepSize: 1 }} }} }} }});");
                break;
            case ChartType.Bubble:
                var scalesOptions = customScalesOptions ??
                                    $"scales: {{x:{{ticks:{{stepSize:{xAxisStep}}}, title: {{display:true, text:'{chartXAxisTitle.JavaScriptString()}'}}}},y:{{ticks:{{stepSize:{yAxisStep}}}, title: {{display:true, text:'{chartYAxisTitle.JavaScriptString()}'}}}}}}";
                streamWriter.WriteLine(
                    $"  addBubbleChart('{chartId.JavaScriptString()}', [{chartTitlesBuilder}], data, {{{scalesOptions}}});");
                break;
            case ChartType.Line:
                var xLabelsBuilder = new StringBuilder();
                foreach (var xLabel in xLabels ?? [])
                {
                    xLabelsBuilder.Append('\'').Append(xLabel.JavaScriptString()).Append("',");
                }

                if (xLabelsBuilder.Length > 0) xLabelsBuilder.Length--;
                streamWriter.WriteLine(
                    $"    addLineChart('{chartId.JavaScriptString()}', [{chartTitlesBuilder}], data, [{xLabelsBuilder}], '{stack.JavaScriptString()}', {{{customScalesOptions ?? ""}}});");
                break;
        }

        streamWriter.WriteLine("})();");
        streamWriter.Flush();
    }

    /// <summary>Writes data lines, including colors when present.</summary>
    public void WriteData(IEnumerable<DataLine> dataLines, bool isLastDataLine)
    {
        if (nbDatasets > 1)
        {
            streamWriter.WriteLine("[");
        }

        foreach (var dataLine in dataLines)
        {
            streamWriter.WriteLine(dataLine.DefaultColor
                ? $"    {{ label: '{dataLine.Label.JavaScriptString()}', value: {dataLine.Value} }},"
                : $"    {{ label: '{dataLine.Label.JavaScriptString()}', value: {dataLine.Value}, color: '{((ColoredDataLine)dataLine).RgbaColor}' }},");
        }

        if (nbDatasets > 1)
        {
            streamWriter.WriteLine(isLastDataLine ? "]" : "],");
        }

        streamWriter.Flush();
    }

    /// <summary>Writes colored data lines to the chart file.</summary>
    public void WriteData(IEnumerable<ColoredDataLine> dataLines, bool isLastDataLine = true)
    {
        if (nbDatasets > 1)
        {
            streamWriter.WriteLine("[");
        }

        foreach (var dataLine in dataLines)
        {
            streamWriter.WriteLine(
                $"    {{ label: '{dataLine.Label.JavaScriptString()}', value: {dataLine.Value}, color: '{dataLine.RgbaColor}' }},");
        }

        if (nbDatasets > 1)
        {
            streamWriter.WriteLine(isLastDataLine ? "]" : "],");
        }

        streamWriter.Flush();
    }

    /// <summary>Writes bubble chart data lines to the chart file.</summary>
    public void WriteData(IEnumerable<BubbleChartDataLine> dataLines, bool isLastDataLine)
    {
        streamWriter.WriteLine("[");

        foreach (var dataLine in dataLines)
        {
            streamWriter.WriteLine(
                $"    {{ x: {dataLine.X}, y: {dataLine.Y}, r: {decimal.Parse(dataLine.Value, NumberStyles.Float, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)}, color: '{dataLine.RgbaColor}' }},");
        }

        streamWriter.WriteLine(isLastDataLine ? "]" : "],");
        streamWriter.Flush();
    }

    /// <summary>Writes a line chart data series to the chart file.</summary>
    public void WriteData(LineChartDataLine dataLine)
    {
        streamWriter.WriteLine(
            $"    {{ label: '{dataLine.Label.JavaScriptString()}', data: [{string.Join(',', dataLine.Values.Select(x => x.ToString(CultureInfo.InvariantCulture)))}], borderColor: '{dataLine.RgbaColor}', backgroundColor: '{dataLine.RgbaColor}', fill: true }},");

        streamWriter.Flush();
    }

}




