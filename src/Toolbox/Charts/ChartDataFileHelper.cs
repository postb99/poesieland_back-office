using System.Globalization;
using System.Text;

namespace Toolbox.Charts;

public class ChartDataFileHelper(StreamWriter streamWriter, ChartType chartType, int nbDatasets = 1)
{
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
    /// Write addXXXChart() javascript declaration.
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
            chartTitlesBuilder.Append('\'').Append(chartTitle).Append("',");
        }

        chartTitlesBuilder.Remove(chartTitlesBuilder.Length - 1, 1);

        switch (chartType)
        {
            case ChartType.Bar:
                streamWriter.WriteLine(nbDatasets == 1
                    ? $"    addBarChart('{chartId}', [{chartTitlesBuilder}], [data], {{{customScalesOptions ?? ""}}});"
                    : $"    addBarChart('{chartId}', [{chartTitlesBuilder}], data, {{{customScalesOptions ?? ""}}});");
                break;
            case ChartType.Pie:
                streamWriter.WriteLine(
                    $"  addPieChart('{chartId}', [data], {{ plugins: {{ title: {{ display: true, text: '{chartTitles[0]}' }} }} }});");
                break;
            case ChartType.Radar:
                var backgroundColor = string.IsNullOrEmpty(radarChartBackgroundColor)
                    ? "rgba(76, 201, 240)"
                    : radarChartBackgroundColor;
                var borderColor = string.IsNullOrEmpty(radarChartBorderColor)
                    ? "rgba(0, 0, 0, 0.1)"
                    : radarChartBorderColor;

                streamWriter.WriteLine(
                    $"  addRadarChart('{chartId}', ['{chartTitles[0]}'], [data], {{ backgroundColor: '{backgroundColor}', borderColor: '{borderColor}', pointBackgroundColor: '{borderColor}', pointBorderColor: '#fff', pointHoverBackgroundColor: '#fff', pointHoverBorderColor: 'rgb(54, 162, 235)', elements: {{ line: {{ borderWidth: 1  }} }}, scales: {{ r: {{ ticks: {{ stepSize: 1 }} }} }} }});");
                break;
            case ChartType.Bubble:
                var scalesOptions = customScalesOptions ??
                                    $"scales: {{x:{{ticks:{{stepSize:{xAxisStep}}}, title: {{display:true, text:'{chartXAxisTitle}'}}}},y:{{ticks:{{stepSize:{yAxisStep}}}, title: {{display:true, text:'{chartYAxisTitle}'}}}}}}";
                streamWriter.WriteLine(
                    $"  addBubbleChart('{chartId}', [{chartTitlesBuilder}], data, {{{scalesOptions}}});");
                break;
            case ChartType.Line:
                var xLabelsBuilder = new StringBuilder();
                foreach (var xLabel in xLabels)
                {
                    xLabelsBuilder.Append('\'').Append(xLabel).Append("',");
                }

                xLabelsBuilder.Remove(xLabelsBuilder.Length - 1, 1);
                streamWriter.WriteLine(
                    $"    addLineChart('{chartId}', [{chartTitlesBuilder}], data, [{xLabelsBuilder}], '{stack}', {{{customScalesOptions ?? ""}}});");
                break;
        }

        streamWriter.WriteLine("})();");
        streamWriter.Flush();
    }

    public void WriteData(IEnumerable<DataLine> dataLines, bool isLastDataLine)
    {
        if (nbDatasets > 1)
        {
            streamWriter.WriteLine("[");
        }

        foreach (var dataLine in dataLines)
        {
            streamWriter.WriteLine(dataLine.DefaultColor
                ? $"    {{ label: '{dataLine.Label}', value: {dataLine.Value} }},"
                : $"    {{ label: '{dataLine.Label}', value: {dataLine.Value}, color: '{((ColoredDataLine)dataLine).RgbaColor}' }},");
        }

        if (nbDatasets > 1)
        {
            streamWriter.WriteLine(isLastDataLine ? "]" : "],");
        }

        streamWriter.Flush();
    }

    public void WriteData(IEnumerable<ColoredDataLine> dataLines, bool isLastDataLine = true)
    {
        if (nbDatasets > 1)
        {
            streamWriter.WriteLine("[");
        }

        foreach (var dataLine in dataLines)
        {
            streamWriter.WriteLine(
                $"    {{ label: '{dataLine.Label}', value: {dataLine.Value}, color: '{dataLine.RgbaColor}' }},");
        }

        if (nbDatasets > 1)
        {
            streamWriter.WriteLine(isLastDataLine ? "]" : "],");
        }

        streamWriter.Flush();
    }

    public void WriteData(IEnumerable<BubbleChartDataLine> dataLines, bool isLastDataLine)
    {
        streamWriter.WriteLine("[");

        foreach (var dataLine in dataLines)
        {
            streamWriter.WriteLine(
                $"    {{ x: {dataLine.X}, y: {dataLine.Y}, r: {dataLine.Value}, color: '{dataLine.RgbaColor}' }},");
        }

        streamWriter.WriteLine(isLastDataLine ? "]" : "],");
        streamWriter.Flush();
    }

    public void WriteData(LineChartDataLine dataLine)
    {
        streamWriter.WriteLine(
            $"    {{ label: '{dataLine.Label}', data: [{string.Join(',', dataLine.Values.Select(x => x.ToString(new NumberFormatInfo { NumberDecimalSeparator = "." })))}], borderColor: '{dataLine.RgbaColor}', backgroundColor: '{dataLine.RgbaColor}', fill: true }},");

        streamWriter.Flush();
    }

    public string FormatCategoriesBubbleChartLabelOptions(List<string>? xAxisLabelsForCallback,
        List<string>? yAxisLabelsForCallback = null, string? xAxisTitle = null, string? yAxisTitle = null)
    {
        var xAxisTitleOption = xAxisTitle is null ? " " : $", title: {{display:true, text:'{xAxisTitle}'}} ";
        var yAxisTitleOption = yAxisTitle is null ? " " : $", title: {{display:true, text:'{yAxisTitle}'}} ";

        // https://www.chartjs.org/docs/latest/axes/labelling.html
        var sb = new StringBuilder("scales: { x: { ")
            .Append("ticks: { stepSize: 1, ")
            .Append("autoSkip: false");
        if (xAxisLabelsForCallback is not null)
        {
            sb.Append(", callback: function(value, index, ticks) { return [")
                .Append(string.Join(',', xAxisLabelsForCallback.Select(x => $"'{x}'")))
                .Append("][index]; }");
        }
        sb.Append(" }")
          .Append(xAxisTitleOption)
          .Append('}');

        sb.Append(", y: { ")
            .Append("ticks: { stepSize: 1, ")
            .Append("autoSkip: false");
        if (yAxisLabelsForCallback is not null)
        {
            sb.Append(", callback: function(value, index, ticks) { return [")
                .Append(string.Join(',', yAxisLabelsForCallback.Select(x => $"'{x}'")))
                .Append("][index]; }");
        }
        sb.Append(" }")
            .Append(yAxisTitleOption)
            .Append('}');

        sb.Append(" }");

        return sb.ToString();
    }
}