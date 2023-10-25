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
    
    private readonly StreamWriter _streamWriter;
    private readonly ChartType _chartType;

    public ChartDataFileHelper(StreamWriter streamWriter, ChartType chartType)
    {
        _streamWriter = streamWriter;
        _chartType = chartType;
    }

    public void WriteBeforeData()
    {
        switch (_chartType)
        {
            case ChartType.Bar:
                _streamWriter.WriteLine("import { addBarChart } from './add-chart.js'");
                break;
        }
        
        _streamWriter.WriteLine("(async function () {");
        _streamWriter.WriteLine("  const data = [");
        _streamWriter.Flush();
    }

    public void WriteAfterData(string chartId, string chartTitle)
    {
        _streamWriter.WriteLine("  ];");
        switch (_chartType)
        {
            case ChartType.Bar:
                _streamWriter.WriteLine($"    addBarChart('{chartId}', '{chartTitle}', data)");
                break;
        }
        
        _streamWriter.WriteLine("})();");
        _streamWriter.Flush();
    }

    public void WriteData(IEnumerable<DataLine> dataLines)
    {
        foreach (var dataLine in dataLines)
        {
            _streamWriter.WriteLine($"    {{ label: '{dataLine.Label}', value: {dataLine.Value} }},");
        }
        
        _streamWriter.Flush();
    }
}