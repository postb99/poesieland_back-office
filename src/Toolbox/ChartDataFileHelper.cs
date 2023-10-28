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

    public struct DataLine
    {
        public string Label;
        public int Value;
    }
    
    /// <summary>
    /// RgbColor sample value: "rgb(255, 205, 86)".
    /// </summary>
    public struct ColoredDataLine
    {
        public string Label;
        public int Value;
        public string RgbColor;
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
        }

        _streamWriter.WriteLine("(async function () {");
        _streamWriter.WriteLine("  const data = [");

        _streamWriter.Flush();
    }

    public void WriteAfterData(string chartId, string[] chartTitles)
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
                    ? $"    addBarChart('{chartId}', [{chartTitlesBuilder}], [data]);"
                    : $"    addBarChart('{chartId}', [{chartTitlesBuilder}], data);");
                break;
            case ChartType.Pie:
                _streamWriter.WriteLine($"  addPieChart('{chartId}', [data], '{chartTitles[0]}');");
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
            _streamWriter.WriteLine($"    {{ label: '{dataLine.Label}', value: {dataLine.Value} }},");
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
            _streamWriter.WriteLine($"    {{ label: '{dataLine.Label}', value: {dataLine.Value}, color: '{dataLine.RgbColor}' }},");
        }
        _streamWriter.Flush();
    }
}