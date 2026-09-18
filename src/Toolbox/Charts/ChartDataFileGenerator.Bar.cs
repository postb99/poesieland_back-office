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
    /// Generates bar and pie chart data files based on poem metrics, such as verse length,
    /// and categorizes them by seasons or a general context.
    /// This method processes the given poems from the Root object and distinguishes regular,
    /// variable, and undefined metrics. It outputs:
    /// - A pie chart file "poems-verse-length-pie.js" when a general context is used (seasonId is null, forSonnet is falsy).
    /// - A bar chart file "metrique_variable-bar.js" when a general context is used (seasonId is null, forSonnet is falsy).
    /// - A bar chart file "poems-verse-length-bar.js" when a specific season is selected.
    /// - A pie chart file "sonnet-verse-length-pie.js" when forSonnet is true.
    /// </summary>
    /// <param name="data">The root object containing seasons and poems data.</param>
    /// <param name="seasonId">An optional season identifier to filter poems by season. If null, all seasons are included.</param>
    /// <param name="forSonnet">A flag indicating that only poems of type sonnet are taken into account.</param>
    public void GeneratePoemMetricBarAndPieChartDataFile(Root data, int? seasonId, bool? forSonnet = false)
    {
        var isGeneral = seasonId is null && forSonnet != true;
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CHART_DATA_FILES_ROOT_DIR]!);
        var fileName = seasonId is not null ? "poems-verse-length-bar.js" :
            forSonnet == true ? "sonnet-verse-length-pie.js" : "poems-verse-length-pie.js";
        var subDir = seasonId is not null ? $"season-{seasonId}" : forSonnet == true ? "taxonomy" : "general";
        var chartId = seasonId is not null ? $"season{seasonId}VerseLengthBar" :
            forSonnet == true ? "sonnetVerseLengthPie" : "poemVerseLengthPie";
        Directory.CreateDirectory(Path.Combine(rootDir, subDir));
        using var streamWriter = OpenChartWriter(subDir, fileName,
            seasonId is not null ? ChartType.Bar : ChartType.Pie, out var chartDataFileHelper, 1);
        var regularMetricData = new Dictionary<int, int>();
        var variableMetricData = new Dictionary<string, int>();
        var poems = seasonId is not null
            ? data.Seasons.First(x => x.Id == seasonId).Poems
            : forSonnet == true
                ? data.Seasons.SelectMany(x => x.Poems).Where(x => x.PoemType == "sonnet")
                : data.Seasons.SelectMany(x => x.Poems);

        foreach (var poem in poems)
        {
            if (poem.HasVariableMetric)
            {
                if (isGeneral)
                {
                    foreach (var metric in poem.VerseLength.Split(','))
                    {
                        // Standard metrics defined in variable metrics go to general metric pie chart
                        if (!int.TryParse(metric.Trim(), out var verseLength)) continue;
                        if (regularMetricData.TryGetValue(verseLength, out _))
                        {
                            regularMetricData[verseLength]++;
                        }
                        else
                        {
                            regularMetricData[verseLength] = 1;
                        }
                    }
                }

                // Detailed variable metric go to general metric bar chart
                if (variableMetricData.TryGetValue(poem.DetailedMetric, out _))
                {
                    variableMetricData[poem.DetailedMetric]++;
                }
                else
                {
                    variableMetricData[poem.DetailedMetric] = 1;
                }
            }
            else
            {
                var verseLength = int.Parse(poem.VerseLength);
                if (regularMetricData.TryGetValue(verseLength, out _))
                {
                    regularMetricData[verseLength]++;
                }
                else
                {
                    regularMetricData[verseLength] = 1;
                }
            }
        }

        var regularMetricRange = regularMetricData.Keys.Order().ToList();
        var variableMetricRange = variableMetricData.Keys.Order().ToList();

        var regularMetricChartData = new List<DataLine>();
        var variableMetricChartData = new List<ColoredDataLine>();

        foreach (var metricValue in regularMetricRange)
        {
            regularMetricChartData.Add(new(
                metricValue.ToString(), regularMetricData[metricValue]));
        }

        foreach (var verseLength in variableMetricRange)
        {
            variableMetricChartData.Add(new(verseLength, variableMetricData[verseLength], "rgba(72, 149, 239, 1)"));
        }

        // General/for sonnet pie chart vs Season's bar chart
        var dataLines = new List<DataLine>();

        if (seasonId is null || forSonnet == true)
        {
            var coloredDataLines = new List<ColoredDataLine>();

            foreach (var metricValue in regularMetricRange)
            {
                var term = metricValue == 1 ? "syllabe" : "syllabes";
                coloredDataLines.Add(new($"{metricValue} {term}",
                    regularMetricData[metricValue], MetricSettings.Metrics.First(m => m.Length == metricValue).Color));
            }

            chartDataFileHelper.WriteData(coloredDataLines, true);

            chartDataFileHelper.WriteAfterData(chartId, ["Poèmes"]);
            streamWriter.Close();
        }
        else
        {
            dataLines.AddRange(regularMetricChartData);
            dataLines.AddRange(variableMetricChartData);

            chartDataFileHelper.WriteData(dataLines, true);

            chartDataFileHelper.WriteAfterData(chartId, ["Poèmes"],
                customScalesOptions: "scales: { y: { ticks: { stepSize: 1 } } }");
            streamWriter.Close();
        }

        // Variable metric general bar chart
        if (isGeneral)
        {
            fileName = "metrique_variable-bar.js";
            chartId = "metrique_variableBar";
            using var streamWriter2 = OpenChartWriter("general", fileName, ChartType.Bar, out var chartDataFileHelper2, 1);

            dataLines = [];
            dataLines.AddRange(variableMetricChartData.Select(UpdateVariableMetricColor));

            chartDataFileHelper2.WriteData(dataLines, true);

            chartDataFileHelper2.WriteAfterData(chartId,
                [
                    "Orange : vers impair puis pair, mauve : vers pair puis impair, bleu : vers pairs, vert : vers impairs"
                ],
                customScalesOptions: "scales: { y: { ticks: { stepSize: 1 } } }");
            streamWriter2.Close();
        }
    }

    /// <summary>
    /// Generates a bar chart data file for poems categorized by season and additional filters.
    /// The resulting file captures seasonal trends in poem data and is stored in the
    /// taxonomy subdirectory. The method allows filtering poems based on sub-category,
    /// category, or specific poem forms or features such as acrostiche, sonnet, and others.
    /// Following files may be generated, depending on the applied filters:
    /// - poems-{storageSubCategory}-bar.js
    /// - poems-{storageCategory}-bar.js
    /// - Various files for other poem types like acrostiche, sonnet, and patterns.
    /// </summary>
    /// <param name="data">The primary source of poem data, including seasonal information.</param>
    /// <param name="storageSubCategory">The sub-category to filter poems. If null, filtering is skipped.</param>
    /// <param name="storageCategory">The category to filter poems. If null, filtering is skipped.</param>
    /// <param name="forAcrostiche">A flag indicating to include poems of type "acrostiche".</param>
    /// <param name="poemType">The optional type of poem.</param>
    /// <param name="forVariableMetric">A flag indicating to include poems with variable metrics.</param>
    /// <param name="forMetric">An optional numeric metric filter for poems.</param>
    /// <param name="extraTag">A flag indicating to include poems with this extra tag.</param>
    public void GenerateOverSeasonsChartDataFile(Root data, string? storageSubCategory, string? storageCategory,
        bool forAcrostiche = false, PoemType? poemType = null, bool forVariableMetric = false,
        int? forMetric = null, string? extraTag = null)
    {
        var fileName = string.Empty;

        var chartId = string.Empty;
        var borderColor = "rgba(72, 149, 239, 1)";

        if (storageSubCategory is not null)
        {
            fileName = $"poems-{storageSubCategory.UnaccentedCleaned()}-bar.js";
            chartId = $"poems-{storageSubCategory.UnaccentedCleaned()}Bar";
            borderColor = StorageSettings.Categories
                .SelectMany(x => x.Subcategories).FirstOrDefault(x => x.Name == storageSubCategory)!.Color;

            switch (borderColor)
            {
                // Use some not too light colors
                case "rgba(254, 231, 240, 1)": // Amitié
                    borderColor = "rgba(255, 194, 209, 1)";
                    break;
                case "rgba(247, 235, 253, 1)": // Enfance et adolescance
                    borderColor = "rgba(234, 191, 250, 1)";
                    break;
                case "rgba(244, 254, 254, 1)": // Neige
                    borderColor = "rgba(119, 181, 254, 1)";
                    break;
            }
        }
        else if (storageCategory is not null)
        {
            fileName = $"poems-{storageCategory.UnaccentedCleaned()}-bar.js";
            chartId = $"poems-{storageCategory.UnaccentedCleaned()}Bar";
            borderColor = StorageSettings.Categories
                .FirstOrDefault(x => x.Name == storageCategory)!.Color;
        }
        else if (forAcrostiche)
        {
            fileName = $"poems-acrostiche-bar.js";
            chartId = $"poems-acrosticheBar";
        }
        else if (poemType is not null)
        {
            fileName = $"poems-{poemType.ToString().UnaccentedCleaned()}-bar.js";
            chartId = $"poems-{poemType.ToString().UnaccentedCleaned()}Bar";
        }
        else if (forVariableMetric)
        {
            fileName = $"poems-metrique_variable-bar.js";
            chartId = $"poems-metrique_variableBar";
        }
        else if (forMetric is not null)
        {
            fileName = $"poems-metric-{forMetric}-bar.js";
            chartId = $"poems-metric{forMetric}Bar";
        }
        else if (extraTag is not null)
        {
            fileName = $"poems-{extraTag.UnaccentedCleaned().Replace('_', '-')}-bar.js";
            chartId = $"poems-{extraTag.UnaccentedCleaned()}Bar";
        }

        var backgroundColor = borderColor.Replace("1)", "0.5)");
        if (forVariableMetric)
        {
            // For this one, same color to look nice below other bar chart
            backgroundColor = borderColor;
        }

        using var streamWriter = OpenChartWriter("taxonomy", fileName, ChartType.Bar, out var chartDataFileHelper);

        var dataLines = new List<DataLine>();

        foreach (var season in data.Seasons.Where(x => x.Poems.Count > 0))
        {
            var poemCount = 0;
            if (storageSubCategory is not null)
            {
                poemCount = season.Poems.Count(x =>
                    x.Categories.Any(x => x.SubCategories.Contains(storageSubCategory)));
            }
            else if (storageCategory is not null)
            {
                poemCount = season.Poems.Count(x => x.Categories.Any(x => x.Name == storageCategory));
            }
            else if (forAcrostiche)
            {
                poemCount = season.Poems.Count(x => x.Acrostiche is not null || x.DoubleAcrostiche is not null);
            }
            else if (poemType is not null)
            {
                poemCount = season.Poems.Count(x => x.HasType(poemType.Value));
            }
            else if (forVariableMetric)
            {
                poemCount = season.Poems.Count(x => x.HasVariableMetric);
            }
            else if (forMetric is not null)
            {
                poemCount = season.Poems.Count(x => x.HasMetric(forMetric.Value));
            }
            else if (extraTag is not null)
            {
                poemCount = season.Poems.Count(x => x.ExtraTags != null && x.ExtraTags.Contains(extraTag));
            }

            dataLines.Add(new ColoredDataLine(season.TitleForChartsWithYears,
                poemCount,
                backgroundColor));
        }

        chartDataFileHelper.WriteData(dataLines, true);

        chartDataFileHelper.WriteAfterData(chartId, ["Poèmes au fil des saisons"],
            customScalesOptions: "scales: { y: { ticks: { stepSize: 1 } } }");
        streamWriter.Close();
    }

    /// <summary>
    /// Generates a bar chart data file representing poem intervals, categorized by seasons or other specified criteria.
    /// The method processes poem data from the provided `Root` objects and writes chart data files with interval-based statistics.
    /// Output files:
    /// - "poem-interval-bar.js"
    /// - "longest_intervals.md"
    /// - "poem-series-bar.js"
    /// - "longest_series.md"
    /// </summary>
    /// <param name="data">The primary source of French poems data.</param>
    /// <param name="dataEn">The secondary source of English poems data.</param>
    /// <param name="seasonId">An optional parameter specifying the season ID to filter poems. If null, filtering by season is skipped.</param>
    public void GeneratePoemIntervalBarChartDataFile(Root data, Root dataEn, int? seasonId)
    {
        var frDatesList =
            (seasonId is null ? data.Seasons.SelectMany(x => x.Poems) : data.Seasons.First(x => x.Id == seasonId).Poems)
            .Where(x => x.TextDate != "01.01.1994")
            .Select(x => x.Date);

        // Add EN poems
        var enDatesList = (seasonId is null
                ? dataEn.Seasons.SelectMany(x => x.Poems)
                : dataEn.Seasons.FirstOrDefault(x => x.Id == seasonId)?.Poems)?
            .Select(x => x.Date);

        var datesList = new List<DateTime>();
        datesList.AddRange(frDatesList);
        if (enDatesList is not null)
            datesList.AddRange(enDatesList);
        datesList.Sort();

        var intervalDict = new Dictionary<DateTime, int>(); // end date, duration
        var intervalLengthDict = new Dictionary<int, int>(); // duration, occurrence
        var seriesDict = new Dictionary<DateTime, int>(); // end date, duration

        int dateCount = datesList.Count();
        for (var i = 1; i < dateCount; i++)
        {
            var current = datesList[i];
            var previous = datesList[i - 1];
            var dayDiff = (int)(current - previous).TotalDays;

            if (!intervalLengthDict.TryAdd(dayDiff, 1))
            {
                intervalLengthDict[dayDiff]++;
            }

            intervalDict.TryAdd(current, dayDiff);

            seriesDict.TryAdd(previous, 1);

            if (dayDiff != 1) continue;

            var duration = seriesDict[previous];
            seriesDict.Remove(previous);
            seriesDict[current] = ++duration;
        }

        // Interval length charts

        var dataLines = new List<ColoredDataLine>();
        var orderedIntervalKeys = intervalLengthDict.Keys.Order().ToList();
        var zeroDayColor = "rgba(72, 149, 239, 1)";
        var oneDayColor = "rgba(72, 149, 239, 0.9)";
        var upToSevenDayColor = "rgba(72, 149, 239, 0.7)";
        var upToOneMonthColor = "rgba(72, 149, 239, 0.5)";
        var upToThreeMonthsColor = "rgba(72, 149, 239, 0.3)";
        var upToOneYearColor = "rgba(72, 149, 239, 0.2)";
        var moreThanOneYearColor = "rgba(72, 149, 239, 0.1)";
        var moreThanOneMonthCount = 0;
        var moreThanThreeMonthsCount = 0;
        var moreThanOneYearCount = 0;
        foreach (var key in orderedIntervalKeys)
        {
            if (key == 0)
            {
                dataLines.Add(
                    new("Moins d'un jour", intervalLengthDict[key],
                        zeroDayColor));
            }
            else if (key == 1)
            {
                dataLines.Add(new("Un jour", intervalLengthDict[key], oneDayColor));
            }
            else if (key < 8)
            {
                dataLines.Add(
                    new($"{key}j", intervalLengthDict[key], upToSevenDayColor));
            }
            else if (key < 31)
            {
                dataLines.Add(new($"{key}j", intervalLengthDict[key],
                    upToOneMonthColor));
            }
            else if (key < 91)
            {
                moreThanOneMonthCount++;
            }
            else if (key < 366)
            {
                moreThanThreeMonthsCount++;
            }
            else
            {
                moreThanOneYearCount++;
            }
        }

        if (moreThanOneMonthCount > 0)
            dataLines.Add(new("Entre un et trois mois", moreThanOneMonthCount,
                upToThreeMonthsColor));

        if (moreThanThreeMonthsCount > 0)
            dataLines.Add(new("Entre trois mois et un an", moreThanThreeMonthsCount,
                upToOneYearColor));

        if (moreThanOneYearCount > 0)
            dataLines.Add(new("Plus d'un an", moreThanOneYearCount,
                moreThanOneYearColor));

        var fileName = "poem-interval-bar.js";
        var subDir = seasonId is not null ? $"season-{seasonId}" : "general";
        using var streamWriter = OpenChartWriter(subDir, fileName, ChartType.Bar, out var chartDataFileHelper);
        chartDataFileHelper.WriteData(dataLines, true);
        chartDataFileHelper.WriteAfterData(seasonId is null ? "poemIntervalBar" : $"season{seasonId}PoemIntervalBar",
            ["Fréquence"],
            customScalesOptions: seasonId is null ? string.Empty : "scales: { y: { ticks: { stepSize: 1 } } }");
        streamWriter.Close();

        if (seasonId.HasValue) return;

        // Longest intervals content file

        var longestIntervalKeys = orderedIntervalKeys.OrderDescending().ToList();
        var markdownLines = new List<string>();

        var moreThanOneYearDates = new List<KeyValuePair<DateTime, DateTime>>();
        var moreThanThreeMonthsDates = new List<KeyValuePair<DateTime, DateTime>>();

        foreach (var key in longestIntervalKeys)
        {
            if (key < 91) break;
            var matchingSeries = intervalDict.Where(x => x.Value == key).ToList();
            foreach (var pair in matchingSeries)
            {
                if (key < 366)
                {
                    moreThanThreeMonthsDates.Add(
                        new(pair.Key.AddDays(-key), pair.Key));
                }
                else
                {
                    moreThanOneYearDates.Add(new(pair.Key.AddDays(-key), pair.Key));
                }
            }
        }

        markdownLines.Add("- Plus d'un an, du plus long au plus court :");
        markdownLines.AddRange(moreThanOneYearDates.Select(date =>
            $"  - Du {date.Key:dd.MM.yyyy} au {date.Value:dd.MM.yyyy}"));
        markdownLines.Add("- Plus de trois mois, du plus long au plus court :");
        markdownLines.AddRange(moreThanThreeMonthsDates.Select(date =>
            $"  - Du {date.Key:dd.MM.yyyy} au {date.Value:dd.MM.yyyy}"));

        WriteMarkdownListFile("longest_intervals.md", "Les plus longs intervalles", markdownLines);

        // Series length chart (general chart)

        var seriesLengthDict = new Dictionary<int, int>();
        foreach (var seriesLength in seriesDict.Values)
        {
            if (!seriesLengthDict.TryAdd(seriesLength, 1))
            {
                seriesLengthDict[seriesLength]++;
            }
        }

        var seriesDataLines = new List<ColoredDataLine>();
        var sortedKeys = seriesLengthDict.Keys.Order().ToList();

        foreach (var key in sortedKeys.Skip(1))
        {
            seriesDataLines.Add(new($"{key}j", seriesLengthDict[key],
                "rgba(72, 149, 239, 1)"));
        }

        fileName = "poem-series-bar.js";
        subDir = "general";
        using var streamWriter2 = OpenChartWriter(subDir, fileName, ChartType.Bar, out var chartDataFileHelper2);
        chartDataFileHelper2.WriteData(seriesDataLines, true);
        chartDataFileHelper2.WriteAfterData("poemSeriesBar",
            ["Séries"],
            customScalesOptions: seasonId is null ? string.Empty : "scales: { y: { ticks: { stepSize: 1 } } }");
        streamWriter2.Close();

        // longest series content file

        var longestSeriesKeys = sortedKeys.OrderDescending().Take(5);
        var longestSeriesMarkdownLines = new List<string>();
        foreach (var key in longestSeriesKeys)
        {
            var matchingSeries = seriesDict.Where(x => x.Value == key);
            longestSeriesMarkdownLines.Add($"- {key} jours :");
            longestSeriesMarkdownLines.AddRange(matchingSeries.Select(pair =>
                $"  - Du {pair.Key.AddDays(-key):dd.MM.yyyy} au {pair.Key:dd.MM.yyyy}"));
        }

        WriteMarkdownListFile("longest_series.md", "Les plus longues séries", longestSeriesMarkdownLines);
    }

    /// <summary>
    /// Updates the color of a variable-metric data line according to the parity of its metric values.
    /// The color mapping is determined by the combination of even and odd values in the metric.
    /// If the label cannot be parsed as an integer array, the original data line is returned unchanged.
    /// </summary>
    /// <param name="coloredDataLine">The data line whose color should be evaluated.</param>
    /// <returns>A data line with the calculated variable-metric color, or the original line when parsing fails.</returns>
    private ColoredDataLine UpdateVariableMetricColor(ColoredDataLine coloredDataLine)
    {
        try
        {
            var metrics = coloredDataLine.Label.ToIntArray();
            if (metrics[0] % 2 == 0 && metrics[1] % 2 == 0)
            {
                // twice even => color of hexasyllabe
                return new ColoredDataLine(coloredDataLine.Label, coloredDataLine.Value,
                    "rgb(174, 214, 241)");
            }

            if (metrics[0] % 2 == 1 && metrics[1] % 2 == 1)
            {
                // twice odd => color of octosyllabe
                return new ColoredDataLine(coloredDataLine.Label, coloredDataLine.Value,
                    "rgb(162, 217, 206)");
            }

            if (metrics[0] % 2 == 1 && metrics[1] % 2 == 0)
            {
                // odd then even => color of alexandrin
                return new ColoredDataLine(coloredDataLine.Label, coloredDataLine.Value,
                    "rgb(237, 187, 153)");
            }

            // even then odd => color of tetrasyllabe
            return new ColoredDataLine(coloredDataLine.Label, coloredDataLine.Value,
                "rgb(215, 189, 226)");
        }
        catch (FormatException)
        {
            return coloredDataLine;
        }
    }
}
