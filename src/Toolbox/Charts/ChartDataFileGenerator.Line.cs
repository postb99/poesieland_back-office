using System.Globalization;
using Microsoft.Extensions.Configuration;
using Toolbox.Domain;
using Toolbox.Settings;

namespace Toolbox.Charts;

/// <summary>
/// Provides chart-specific data file generation methods for the application's chart data files.
/// </summary>
public partial class ChartDataFileGenerator
{
    /// <summary>
    /// Generates a line chart data file for visualizing metrics distributed over seasons.
    /// The method processes the data provided in the `Root` object and writes poems-verseLength-line.js
    /// line chart data file containing the chart-ready data.
    /// </summary>
    /// <param name="data">The primary source of French poems data.</param>
    public void GenerateOverSeasonsMetricLineChartDataFile(Root data)
    {
        var dataDict = ChartDataFileHelper.FillMetricDataDict(data, out var xLabels);

        var fileName = "poems-verseLength-line.js";

        using var streamWriter = OpenChartWriter("general", fileName, ChartType.Line, out var chartDataFileHelper, 14);

        var oneFootDataLines =
            new LineChartDataLine("1 syllabe", dataDict[1],
                MetricSettings.Metrics.First(x => x.Length == 1).Color);
        var twoFeetDataLines =
            new LineChartDataLine("2 syllabes", dataDict[2],
                MetricSettings.Metrics.First(x => x.Length == 2).Color);
        var threeFeetDataLines =
            new LineChartDataLine("3 syllabes", dataDict[3],
                MetricSettings.Metrics.First(x => x.Length == 3).Color);
        var fourFeetDataLines =
            new LineChartDataLine("4 syllabes", dataDict[4],
                MetricSettings.Metrics.First(x => x.Length == 4).Color);
        var fiveFeetDataLines =
            new LineChartDataLine("5 syllabes", dataDict[5],
                MetricSettings.Metrics.First(x => x.Length == 5).Color);
        var sixFeetDataLines =
            new LineChartDataLine("6 syllabes", dataDict[6],
                MetricSettings.Metrics.First(x => x.Length == 6).Color);
        var sevenFeetDataLines =
            new LineChartDataLine("7 syllabes", dataDict[7],
                MetricSettings.Metrics.First(x => x.Length == 7).Color);
        var eightFeetDataLines =
            new LineChartDataLine("8 syllabes", dataDict[8],
                MetricSettings.Metrics.First(x => x.Length == 8).Color);
        var nineFeetDataLines =
            new LineChartDataLine("9 syllabes", dataDict[9],
                MetricSettings.Metrics.First(x => x.Length == 9).Color);
        var tenFeetDataLines =
            new LineChartDataLine("10 syllabes", dataDict[10],
                MetricSettings.Metrics.First(x => x.Length == 10).Color);
        var elevenFeetDataLines =
            new LineChartDataLine("11 syllabes", dataDict[11],
                MetricSettings.Metrics.First(x => x.Length == 11).Color);
        var twelveFeetDataLines =
            new LineChartDataLine("12 syllabes", dataDict[12],
                MetricSettings.Metrics.First(x => x.Length == 12).Color);
        var fourteenFeetDataLines =
            new LineChartDataLine("14 syllabes", dataDict[14],
                MetricSettings.Metrics.First(x => x.Length == 14).Color);

        chartDataFileHelper.WriteData(oneFootDataLines);
        chartDataFileHelper.WriteData(twoFeetDataLines);
        chartDataFileHelper.WriteData(threeFeetDataLines);
        chartDataFileHelper.WriteData(fourFeetDataLines);
        chartDataFileHelper.WriteData(fiveFeetDataLines);
        chartDataFileHelper.WriteData(sixFeetDataLines);
        chartDataFileHelper.WriteData(sevenFeetDataLines);
        chartDataFileHelper.WriteData(eightFeetDataLines);
        chartDataFileHelper.WriteData(nineFeetDataLines);
        chartDataFileHelper.WriteData(tenFeetDataLines);
        chartDataFileHelper.WriteData(elevenFeetDataLines);
        chartDataFileHelper.WriteData(twelveFeetDataLines);
        chartDataFileHelper.WriteData(fourteenFeetDataLines);

        chartDataFileHelper.WriteAfterData("poemsVerseLengthLine",
            [
                "1 syllabe",
                "2 syllabes",
                "3 syllabes",
                "4 syllabes",
                "5 syllabes",
                "6 syllabes",
                "7 syllabes",
                "8 syllabes",
                "9 syllabes",
                "10 syllabes",
                "11 syllabes",
                "12 syllabes",
                "14 syllabes"
            ], chartYAxisTitle: "Métrique", chartXAxisTitle: "Au fil des Saisons",
            xLabels: xLabels.ToArray(), stack: "stack0");
        streamWriter.Close();
    }
}
