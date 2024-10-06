using System.Text;

namespace Toolbox;

public class ChartDataFileHelper
{
    public enum ChartType
    {
        Bar,
        Pie,
        Radar,
        Bubble
    }

    public const int VERSE_LENGTH_MAX_Y = 260;
    public const int NBVERSES_MAX_Y = 300;

    public class DataLine
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
    public class ColoredDataLine : DataLine
    {
        public string RgbaColor { get; }

        public override bool DefaultColor => false;

        public ColoredDataLine(string label, int value, string rgbaColor) : base(label, value)
        {
            RgbaColor = rgbaColor;
        }
    }

    public class BubbleChartDataLine
    {
        public string Label { get; }
        public int X { get; }
        public int Y { get; }

        /// <summary>
        /// Bubble radius in pixels, with dot for decimal separator.
        /// </summary>
        public string Value { get; }

        public BubbleChartDataLine(int x, int y, string value)
        {
            X = x;
            Y = y;
            Value = value;
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
        }

        _streamWriter.WriteLine("(async function () {");
        _streamWriter.WriteLine("  const data = [");

        _streamWriter.Flush();
    }

    public void WriteAfterData(string chartId, string[] chartTitles, string radarChartBorderColor = null,
        string radarChartBackgroundColor = null, string barChartOptions = "{}", string chartXAxisTitle = "",
        string chartYAxisTitle = "", int xAxisStep = 1, int yAxisStep = 1, List<string>? bubbleColors = null)
    {
        _streamWriter.WriteLine("  ];");

        switch (_chartType)
        {
            case ChartType.Bar:
                var chartTitlesBuilder = new StringBuilder();
                foreach (var chartTitle in chartTitles)
                {
                    chartTitlesBuilder.Append("'").Append(chartTitle).Append("',");
                }

                chartTitlesBuilder.Remove(chartTitlesBuilder.Length - 1, 1);

                _streamWriter.WriteLine(_nbDatasets == 1
                    ? $"    addBarChart('{chartId}', [{chartTitlesBuilder}], [data], {barChartOptions});"
                    : $"    addBarChart('{chartId}', [{chartTitlesBuilder}], data, {barChartOptions});");
                break;
            case ChartType.Pie:
                _streamWriter.WriteLine($"  addPieChart('{chartId}', [data], {{ plugins: {{ title: {{ display: true, text: '{chartTitles[0]}' }} }} }});");
                break;
            case ChartType.Radar:
                var backgroundColor = string.IsNullOrEmpty(radarChartBackgroundColor) ? "rgba(76, 201, 240)" : radarChartBackgroundColor;
                var borderColor = string.IsNullOrEmpty(radarChartBorderColor) ? "rgba(0, 0, 0, 0.1)" : radarChartBorderColor;
               
                _streamWriter.WriteLine(
                        $"  addRadarChart('{chartId}', ['{chartTitles[0]}'], [data], {{ backgroundColor: '{backgroundColor}', borderColor: '{borderColor}', pointBackgroundColor: '{borderColor}', pointBorderColor: '#fff', pointHoverBackgroundColor: '#fff', pointHoverBorderColor: 'rgb(54, 162, 235)', elements: {{ line: {{ borderWidth: 1  }} }}, scales: {{ r: {{ ticks: {{ stepSize: 1 }} }} }} }});");
                break;
            case ChartType.Bubble:
                if (bubbleColors != null)
                {
                    var sb = new StringBuilder();
                    foreach (var color in bubbleColors)
                    {
                        sb.AppendFormat($"'{color}',");
                    }
                    _streamWriter.WriteLine($"  const borderColorsArray = [{sb}];");
                }

                _streamWriter.WriteLine(
                    $"  addBubbleChart('{chartId}', '{chartTitles[0]}', data, borderColorsArray, {{scales: {{x:{{ticks:{{stepSize:{xAxisStep}}}, title: {{display:true, text:'{chartXAxisTitle}'}}}},y:{{ticks:{{stepSize:{yAxisStep}}}, title: {{display:true, text:'{chartYAxisTitle}'}}}}}}}});");
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

    public void WriteData(IEnumerable<ColoredDataLine> dataLines)
    {
        foreach (var dataLine in dataLines)
        {
            _streamWriter.WriteLine(
                $"    {{ label: '{dataLine.Label}', value: {dataLine.Value}, color: '{dataLine.RgbaColor}' }},");
        }

        _streamWriter.Flush();
    }

    public void WriteData(IEnumerable<BubbleChartDataLine> dataLines)
    {
        foreach (var dataLine in dataLines)
        {
            _streamWriter.WriteLine(
                $"    {{ x: {dataLine.X}, y: {dataLine.Y}, r: {dataLine.Value} }},");
        }

        _streamWriter.Flush();
    }
}