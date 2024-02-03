using System.Text;

namespace Toolbox;

public class ChartDataFileHelper
{
    public enum ChartType
    {
        Bar,
        Pie,
        Radar
    }

    public const int VERSE_LENGTH_MAX_Y = 210;
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

    private readonly StreamWriter _streamWriter;
    private readonly ChartType _chartType;
    private readonly int _nbDatasets;
    private int _datasetIndex;

    public ChartDataFileHelper(StreamWriter streamWriter, ChartType chartType, int nbDatasets = 1)
    {
        _streamWriter = streamWriter;
        _chartType = chartType;
        _nbDatasets = nbDatasets;
        _datasetIndex = 0;
    }

    public void WriteBeforeData()
    {
        switch (_chartType)
        {
            case ChartType.Bar:
                _streamWriter.WriteLine("import { addBarChart } from './add-chart.js'");
                break;
            case ChartType.Pie:
                _streamWriter.WriteLine("import { addPieChart } from './add-chart.js'");
                break;
            case ChartType.Radar:
                _streamWriter.WriteLine("import { addRadarChart } from './add-chart.js'");
                break;
        }

        _streamWriter.WriteLine("(async function () {");
        _streamWriter.WriteLine("  const data = [");

        _streamWriter.Flush();
    }

    public void WriteAfterData(string chartId, string[] chartTitles, string radarChartBorderColor = null,
        string radarChartBackgroundColor = null, string barChartOptions = "{}")
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
                _streamWriter.WriteLine($"  addPieChart('{chartId}', [data], '{chartTitles[0]}');");
                break;
            case ChartType.Radar:
                if (radarChartBorderColor != null)
                    _streamWriter.WriteLine(
                        $"  addRadarChart('{chartId}', ['{chartTitles[0]}'], [data], '{radarChartBorderColor}', '{radarChartBackgroundColor}');");
                else
                    _streamWriter.WriteLine($"  addRadarChart('{chartId}', ['{chartTitles[0]}'], [data]);");
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

        _datasetIndex++;
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
}