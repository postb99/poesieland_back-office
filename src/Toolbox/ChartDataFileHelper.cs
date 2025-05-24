using System.Globalization;
using System.Text;
using Microsoft.Extensions.Primitives;

namespace Toolbox;

public class ChartDataFileHelper
{
    public enum ChartType
    {
        Bar,
        Pie,
        Radar,
        Bubble,
        Line
    }

    public record DataLine
    {
        public string Label { get; }
        public int Value { get; }

        public virtual bool DefaultColor => true;

        public DataLine(string label, int value)
        {
            Label = label;
            Value = value;
        }
    }

    /// <summary>
    /// RgbaColor sample value: "rgb(255, 205, 86)", "rgba(255, 205, 86, 0.5)".
    /// </summary>
    public record ColoredDataLine : DataLine
    {
        public string RgbaColor { get; }

        public override bool DefaultColor => false;

        public ColoredDataLine(string label, int value, string rgbaColor) : base(label, value)
        {
            RgbaColor = rgbaColor;
        }
    }

    public record BubbleChartDataLine
    {
        public int X { get; }
        public int Y { get; }

        /// <summary>
        /// Bubble radius in pixels, with dot for decimal separator.
        /// </summary>
        public string Value { get; }

        public string RgbaColor { get; }

        public BubbleChartDataLine(int x, int y, string value, string rgbaColor)
        {
            X = x;
            Y = y;
            Value = value;
            RgbaColor = rgbaColor;
        }
    }

    public record LineChartDataLine
    {
        public string Label { get; }

        public List<decimal> Values { get; }

        public string RgbaColor { get; }

        public LineChartDataLine(string label, List<decimal> values, string rgbaColor)
        {
            Label = label;
            Values = values;
            RgbaColor = rgbaColor;
        }
    }

    private readonly StreamWriter _streamWriter;
    private readonly ChartType _chartType;
    private readonly int _nbDatasets;

    public ChartDataFileHelper(StreamWriter streamWriter, ChartType chartType, int nbDatasets = 1)
    {
        _streamWriter = streamWriter;
        _chartType = chartType;
        _nbDatasets = nbDatasets;
    }

    public void WriteBeforeData()
    {
        switch (_chartType)
        {
            case ChartType.Bar:
                _streamWriter.WriteLine("import { addBarChart } from '../add-chart.js'");
                break;
            case ChartType.Pie:
                _streamWriter.WriteLine("import { addPieChart } from '../add-chart.js'");
                break;
            case ChartType.Radar:
                _streamWriter.WriteLine("import { addRadarChart } from '../add-chart.js'");
                break;
            case ChartType.Bubble:
                _streamWriter.WriteLine("import { addBubbleChart } from '../add-chart.js'");
                break;
            case ChartType.Line:
                _streamWriter.WriteLine("import { addLineChart } from '../add-chart.js'");
                break;
        }

        _streamWriter.WriteLine("(async function () {");
        _streamWriter.WriteLine("  const data = [");

        _streamWriter.Flush();
    }

    /// <summary>
    /// Write addXXXChart() javascript declaration.
    /// </summary>
    /// <param name="chartId">Mandatory and unique</param>
    /// <param name="chartTitles">Used for bar, pie (but single), line and bubble charts</param>
    /// <param name="radarChartBorderColor">Radar chart option</param>
    /// <param name="radarChartBackgroundColor">Radar chart option</param>
    /// <param name="chartXAxisTitle">Option for bubble chart</param>
    /// <param name="chartYAxisTitle">Option for bubble chart</param>
    /// <param name="xAxisStep">Option for bubble chart</param>
    /// <param name="yAxisStep">Option for bubble chart</param>
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

        _streamWriter.WriteLine("  ];");

        var chartTitlesBuilder = new StringBuilder();
        foreach (var chartTitle in chartTitles)
        {
            chartTitlesBuilder.Append("'").Append(chartTitle).Append("',");
        }

        chartTitlesBuilder.Remove(chartTitlesBuilder.Length - 1, 1);

        switch (_chartType)
        {
            case ChartType.Bar:
                _streamWriter.WriteLine(_nbDatasets == 1
                    ? $"    addBarChart('{chartId}', [{chartTitlesBuilder}], [data], {{{customScalesOptions ?? ""}}});"
                    : $"    addBarChart('{chartId}', [{chartTitlesBuilder}], data, {{{customScalesOptions ?? ""}}});");
                break;
            case ChartType.Pie:
                _streamWriter.WriteLine(
                    $"  addPieChart('{chartId}', [data], {{ plugins: {{ title: {{ display: true, text: '{chartTitles[0]}' }} }} }});");
                break;
            case ChartType.Radar:
                var backgroundColor = string.IsNullOrEmpty(radarChartBackgroundColor)
                    ? "rgba(76, 201, 240)"
                    : radarChartBackgroundColor;
                var borderColor = string.IsNullOrEmpty(radarChartBorderColor)
                    ? "rgba(0, 0, 0, 0.1)"
                    : radarChartBorderColor;

                _streamWriter.WriteLine(
                    $"  addRadarChart('{chartId}', ['{chartTitles[0]}'], [data], {{ backgroundColor: '{backgroundColor}', borderColor: '{borderColor}', pointBackgroundColor: '{borderColor}', pointBorderColor: '#fff', pointHoverBackgroundColor: '#fff', pointHoverBorderColor: 'rgb(54, 162, 235)', elements: {{ line: {{ borderWidth: 1  }} }}, scales: {{ r: {{ ticks: {{ stepSize: 1 }} }} }} }});");
                break;
            case ChartType.Bubble:
                var scalesOptions = customScalesOptions ??
                                    $"scales: {{x:{{ticks:{{stepSize:{xAxisStep}}}, title: {{display:true, text:'{chartXAxisTitle}'}}}},y:{{ticks:{{stepSize:{yAxisStep}}}, title: {{display:true, text:'{chartYAxisTitle}'}}}}}}";
                _streamWriter.WriteLine(
                    $"  addBubbleChart('{chartId}', [{chartTitlesBuilder}], data, {{{scalesOptions}}});");
                break;
            case ChartType.Line:
                var xLabelsBuilder = new StringBuilder();
                foreach (var xLabel in xLabels)
                {
                    xLabelsBuilder.Append("'").Append(xLabel).Append("',");
                }

                xLabelsBuilder.Remove(xLabelsBuilder.Length - 1, 1);
                _streamWriter.WriteLine(
                    $"    addLineChart('{chartId}', [{chartTitlesBuilder}], data, [{xLabelsBuilder}], '{stack}', {{{customScalesOptions ?? ""}}});");
                break;
        }

        _streamWriter.WriteLine("})();");
        _streamWriter.Flush();
    }

    public void WriteData(IEnumerable<DataLine> dataLines, bool isLastDataLine)
    {
        if (_nbDatasets > 1)
        {
            _streamWriter.WriteLine("[");
        }

        foreach (var dataLine in dataLines)
        {
            _streamWriter.WriteLine(dataLine.DefaultColor
                ? $"    {{ label: '{dataLine.Label}', value: {dataLine.Value} }},"
                : $"    {{ label: '{dataLine.Label}', value: {dataLine.Value}, color: '{((ColoredDataLine)dataLine).RgbaColor}' }},");
        }

        if (_nbDatasets > 1)
        {
            _streamWriter.WriteLine(isLastDataLine ? "]" : "],");
        }

        _streamWriter.Flush();
    }

    public void WriteData(IEnumerable<ColoredDataLine> dataLines, bool isLastDataLine = true)
    {
        if (_nbDatasets > 1)
        {
            _streamWriter.WriteLine("[");
        }

        foreach (var dataLine in dataLines)
        {
            _streamWriter.WriteLine(
                $"    {{ label: '{dataLine.Label}', value: {dataLine.Value}, color: '{dataLine.RgbaColor}' }},");
        }

        if (_nbDatasets > 1)
        {
            _streamWriter.WriteLine(isLastDataLine ? "]" : "],");
        }

        _streamWriter.Flush();
    }

    public void WriteData(IEnumerable<BubbleChartDataLine> dataLines, bool isLastDataLine)
    {
        _streamWriter.WriteLine("[");

        foreach (var dataLine in dataLines)
        {
            _streamWriter.WriteLine(
                $"    {{ x: {dataLine.X}, y: {dataLine.Y}, r: {dataLine.Value}, color: '{dataLine.RgbaColor}' }},");
        }

        _streamWriter.WriteLine(isLastDataLine ? "]" : "],");
        _streamWriter.Flush();
    }

    public void WriteData(LineChartDataLine dataLine)
    {
        _streamWriter.WriteLine(
            $"    {{ label: '{dataLine.Label}', data: [{string.Join(',', dataLine.Values.Select(x => x.ToString(new NumberFormatInfo { NumberDecimalSeparator = "." })))}], borderColor: '{dataLine.RgbaColor}', backgroundColor: '{dataLine.RgbaColor}', fill: true }},");

        _streamWriter.Flush();
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
          .Append("}");

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
            .Append("}");

        sb.Append(" }");

        return sb.ToString();
    }
}