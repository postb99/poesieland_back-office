using System.Globalization;
using System.Text;
using Toolbox.Domain;

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

    public static Dictionary<string, int> InitMonthDayDictionary()
    {
        var dataDict = new Dictionary<string, int>();
        for (var i = 1; i < 32; i++)
            dataDict.Add(i < 10 ? $"01-0{i}" : $"01-{i}", 0);
        for (var i = 1; i < 30; i++)
            dataDict.Add(i < 10 ? $"02-0{i}" : $"02-{i}", 0);
        for (var i = 1; i < 32; i++)
            dataDict.Add(i < 10 ? $"03-0{i}" : $"03-{i}", 0);
        for (var i = 1; i < 31; i++)
            dataDict.Add(i < 10 ? $"04-0{i}" : $"04-{i}", 0);
        for (var i = 1; i < 32; i++)
            dataDict.Add(i < 10 ? $"05-0{i}" : $"05-{i}", 0);
        for (var i = 1; i < 31; i++)
            dataDict.Add(i < 10 ? $"06-0{i}" : $"06-{i}", 0);
        for (var i = 1; i < 32; i++)
            dataDict.Add(i < 10 ? $"07-0{i}" : $"07-{i}", 0);
        for (var i = 1; i < 32; i++)
            dataDict.Add(i < 10 ? $"08-0{i}" : $"08-{i}", 0);
        for (var i = 1; i < 31; i++)
            dataDict.Add(i < 10 ? $"09-0{i}" : $"09-{i}", 0);
        for (var i = 1; i < 32; i++)
            dataDict.Add(i < 10 ? $"10-0{i}" : $"10-{i}", 0);
        for (var i = 1; i < 31; i++)
            dataDict.Add(i < 10 ? $"11-0{i}" : $"11-{i}", 0);
        for (var i = 1; i < 32; i++)
            dataDict.Add(i < 10 ? $"12-0{i}" : $"12-{i}", 0);
        return dataDict;
    }

    public static string GetRadarChartLabel(string monthDay)
    {
        var day = monthDay.Substring(3);
        var month = monthDay.Substring(0, 2);
        switch (month)
        {
            case "01":
                return day == "01" ? "Janvier" : string.Empty;
            case "02":
                return day == "01" ? "Février" : string.Empty;
            case "03":
                return day == "01" ? "Mars" : day == "20" ? "Printemps" : string.Empty;
            case "04":
                return day == "01" ? "Avril" : string.Empty;
            case "05":
                return day == "01" ? "Mai" : string.Empty;
            case "06":
                return day == "01" ? "Juin" : day == "21" ? "Eté" : string.Empty;
            case "07":
                return day == "01" ? "Juillet" : string.Empty;
            case "08":
                return day == "01" ? "Août" : string.Empty;
            case "09":
                return day == "01" ? "Septembre" : day == "23" ? "Automne" : string.Empty;
            case "10":
                return day == "01" ? "Octobre" : string.Empty;
            case "11":
                return day == "01" ? "Novembre" : string.Empty;
            case "12":
                return day == "01" ? "Décembre" : day == "21" ? "Hiver" : string.Empty;
            default:
                return string.Empty;
        }
    }
    
    public static string GetRadarEnChartLabel(string monthDay)
    {
        var day = monthDay.Substring(3);
        var month = monthDay.Substring(0, 2);
        switch (month)
        {
            case "01":
                return day == "01" ? "January" : string.Empty;
            case "02":
                return day == "01" ? "February" : string.Empty;
            case "03":
                return day == "01" ? "March" : day == "20" ? "Spring" : string.Empty;
            case "04":
                return day == "01" ? "April" : string.Empty;
            case "05":
                return day == "01" ? "May" : string.Empty;
            case "06":
                return day == "01" ? "June" : day == "21" ? "Summer" : string.Empty;
            case "07":
                return day == "01" ? "July" : string.Empty;
            case "08":
                return day == "01" ? "August" : string.Empty;
            case "09":
                return day == "01" ? "September" : day == "23" ? "Fall" : string.Empty;
            case "10":
                return day == "01" ? "October" : string.Empty;
            case "11":
                return day == "01" ? "November" : string.Empty;
            case "12":
                return day == "01" ? "December" : day == "21" ? "Winter" : string.Empty;
            default:
                return string.Empty;
        }
    }

    public static string GetMonthLabel(string month)
    {
        switch (month)
        {
            case "01":
                return "janvier";
            case "02":
                return "février";
            case "03":
                return "mars";
            case "04":
                return "avril";
            case "05":
                return "mai";
            case "06":
                return "juin";
            case "07":
                return "juillet";
            case "08":
                return "août";
            case "09":
                return "septembre";
            case "10":
                return "octobre";
            case "11":
                return "novembre";
            case "12":
                return "décembre";
            default:
                return "?";
        }
    }
    
    public static void AddDataLine(int x, int y, int value,
        List<BubbleChartDataLine>[] quarterBubbleChartDatalines, int maxValue,
        int bubbleMaxRadiusPixels)
    {
        // Bubble radius and color
        decimal bubbleSize = (decimal)bubbleMaxRadiusPixels * value / maxValue;
        var bubbleColor = string.Empty;
        if (bubbleSize < (bubbleMaxRadiusPixels / 4))
        {
            // First quarter
            bubbleSize *= 4;
            bubbleColor = "rgba(121, 248, 248, 1)";
            quarterBubbleChartDatalines[0].Add(new(x, y,
                bubbleSize.ToString(new NumberFormatInfo { NumberDecimalSeparator = "." }), bubbleColor));
        }
        else if (bubbleSize < (bubbleMaxRadiusPixels / 2))
        {
            // Second quarter
            bubbleSize *= 2;
            bubbleColor = "rgba(119, 181, 254, 1)";
            quarterBubbleChartDatalines[1].Add(new(x, y,
                bubbleSize.ToString(new NumberFormatInfo { NumberDecimalSeparator = "." }), bubbleColor));
        }
        else if (bubbleSize < (bubbleMaxRadiusPixels * 3 / 4))
        {
            // Third quarter
            bubbleSize *= 1.5m;
            bubbleColor = "rgba(0, 127, 255, 1)";
            quarterBubbleChartDatalines[2].Add(new(x, y,
                bubbleSize.ToString(new NumberFormatInfo { NumberDecimalSeparator = "." }), bubbleColor));
        }
        else
        {
            // Fourth quarter
            bubbleColor = "rgba(50, 122, 183, 1)";
            quarterBubbleChartDatalines[3].Add(new(x, y,
                bubbleSize.ToString(new NumberFormatInfo { NumberDecimalSeparator = "." }), bubbleColor));
        }
    }
    
    public static Dictionary<int, List<decimal>> FillMetricDataDict(Root data, out List<string> xLabels)
    {
        var metricRange = Enumerable.Range(1, 14);
        var dataDict = new Dictionary<int, List<decimal>> { };

        xLabels = new();
        foreach (var metric in metricRange)
        {
            dataDict.Add(metric, new());
        }

        foreach (var season in data.Seasons.Where(x => x.Poems.Count > 0))
        {
            // Multiplication to get 50
            var multiple = 50m / season.Poems.Count;
            xLabels.Add($"{season.EscapedTitleForChartsWithYears}");

            foreach (var metric in metricRange)
            {
                dataDict[metric]
                    .Add(Decimal.Round(
                        season.Poems.Count(x =>
                            x.HasMetric(metric)) * multiple, 1));
            }
        }

        return dataDict;
    }
    
    public static void FillCategoriesBubbleChartDataDict(Dictionary<KeyValuePair<string, string>, int> dictionary,
        SortedSet<string> xLabels, SortedSet<string> yLabels, Poem poem)
    {
        var subCategories = poem.Categories.SelectMany(x => x.SubCategories).ToList();

        if (subCategories.Count == 1)
        {
            return;
        }

        for (var i = 0; i < subCategories.Count; i++)
        {
            for (var j = subCategories.Count - 1; j > -1; j--)
            {
                if (string.Compare(subCategories[i], subCategories[j]) >= 0) continue;
                var key = new KeyValuePair<string, string>(subCategories[i], subCategories[j]);
                if (dictionary.TryGetValue(key, out _))
                {
                    dictionary[key]++;
                }
                else
                {
                    dictionary.Add(key, 1);
                    xLabels.Add(key.Key);
                    yLabels.Add(key.Value);
                }
            }
        }
    }
}