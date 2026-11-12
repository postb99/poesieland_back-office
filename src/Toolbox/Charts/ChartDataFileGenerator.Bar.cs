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
        var poems = seasonId is not null
            ? data.Seasons.First(x => x.Id == seasonId).Poems
            : forSonnet == true
                ? data.Seasons.SelectMany(x => x.Poems).Where(x => x.PoemType == "sonnet")
                : data.Seasons.SelectMany(x => x.Poems);

        var (regularMetricData, variableMetricData) = CollectMetricCounts(poems, isGeneral);
        var regularMetricChartData = CreateRegularMetricDataLines(regularMetricData);
        var variableMetricChartData = CreateVariableMetricDataLines(variableMetricData);

        // General/for sonnet pie chart vs Season's bar chart
        var dataLines = new List<DataLine>();

        if (seasonId is null || forSonnet == true)
        {
            var coloredDataLines = CreateMetricPieDataLines(regularMetricData);

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
            using var streamWriter2 =
                OpenChartWriter("general", fileName, ChartType.Bar, out var chartDataFileHelper2, 1);

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
    /// <param name="stackedItem">An optional stacked item.</param>
    public void GenerateOverSeasonsChartDataFile(Root data, string? storageSubCategory, string? storageCategory,
        bool forAcrostiche = false, PoemType? poemType = null, bool forVariableMetric = false,
        int? forMetric = null, string? extraTag = null, BarItem? stackedItem = null)
    {
        var fileName = string.Empty;

        var chartId = string.Empty;
        var borderColor = "rgba(72, 149, 239, 1)";
        
        List<string> chartTitles = [];
        var defaultChartTitle = "Poèmes au fil des saisons";
        var firstChartTitle = defaultChartTitle;

        if (storageSubCategory is not null)
        {
            fileName = $"poems-{storageSubCategory.UnaccentedCleaned()}-bar.js";
            chartId = $"poems-{storageSubCategory.UnaccentedCleaned()}Bar";
            borderColor = StorageSettings.Categories
                .SelectMany(x => x.Subcategories).FirstOrDefault(x => x.Name == storageSubCategory)!.Color;
            
            firstChartTitle = char.ToUpper(storageSubCategory[0]) + storageSubCategory[1..];
        }
        else if (storageCategory is not null)
        {
            fileName = $"poems-{storageCategory.UnaccentedCleaned()}-bar.js";
            chartId = $"poems-{storageCategory.UnaccentedCleaned()}Bar";
            borderColor = StorageSettings.Categories
                .FirstOrDefault(x => x.Name == storageCategory)!.Color;
            
            firstChartTitle = char.ToUpper(storageCategory[0]) + storageCategory[1..];
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
            firstChartTitle = char.ToUpper(extraTag[0]) + extraTag[1..];
        }

        var backgroundColor = borderColor.Replace("1)", "0.5)");
        if (forVariableMetric)
        {
            // For this one, same color to look nice below other bar chart
            backgroundColor = borderColor;
        }

        using var streamWriter = OpenChartWriter("taxonomy", fileName, ChartType.Bar, out var chartDataFileHelper,
            stackedItem is null ? 1 : 2);

        var (dataLines, stackedDataLines) = CreateSeasonalDataLines(data, storageSubCategory, storageCategory,
            forAcrostiche, poemType, forVariableMetric, forMetric, extraTag, stackedItem, backgroundColor);

        chartDataFileHelper.WriteData(dataLines, stackedItem is null);
        chartTitles.Add(stackedItem is null ? defaultChartTitle : firstChartTitle);

        if (stackedItem is not null)
        {
            chartDataFileHelper.WriteData(stackedDataLines, true);
            chartTitles.Add(char.ToUpper(stackedItem.Name[0]) + stackedItem.Name[1..]);
        }

        chartDataFileHelper.WriteAfterData(chartId, chartTitles.ToArray(),
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
            .Where(x => x.TextDate != Poem.TextDateToIgnore)
            .Select(x => x.Date);

        // Add EN poems
        var enDatesList = (seasonId is null
                ? dataEn.Seasons.SelectMany(x => x.Poems)
                : dataEn.Seasons.FirstOrDefault(x => x.Id == seasonId)?.Poems)?
            .Select(x => x.Date);

        var datesList = CollectPoemDates(frDatesList, enDatesList);
        var (intervalDict, intervalLengthDict, poemSeries) = CalculateIntervalsAndSeries(datesList);
        var (dataLines, orderedIntervalKeys) = CreateIntervalDataLines(intervalLengthDict);

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

        var markdownLines = CreateLongestIntervalMarkdownLines(intervalDict, orderedIntervalKeys);

        WriteMarkdownListFile("longest_intervals.md", "Les plus longs intervalles", markdownLines);

        // Series poem count chart (general chart)

        var seriesDataLines = CreateSeriesDataLines(poemSeries);

        fileName = "poem-series-bar.js";
        subDir = "general";
        using var streamWriter2 = OpenChartWriter(subDir, fileName, ChartType.Bar, out var chartDataFileHelper2);
        chartDataFileHelper2.WriteData(seriesDataLines, true);
        chartDataFileHelper2.WriteAfterData("poemSeriesBar",
            ["Séries"],
            customScalesOptions: seasonId is null ? string.Empty : "scales: { y: { ticks: { stepSize: 1 } } }");
        streamWriter2.Close();

        // longest series content file

        var longestSeriesMarkdownLines = CreateLongestSeriesMarkdownLines(poemSeries);

        WriteMarkdownListFile("longest_series.md", "Les plus longues séries", longestSeriesMarkdownLines);
    }

    /// <summary>
    /// Counts regular verse lengths and detailed variable metrics from the supplied poems.
    /// When requested, verse lengths listed on variable-metric poems are also included in the regular counts.
    /// </summary>
    /// <param name="poems">The poems whose metrics are collected.</param>
    /// <param name="includeVariableValuesInRegular">Whether to count the individual verse lengths of variable-metric poems as regular metrics.</param>
    /// <returns>Regular verse-length counts and detailed variable-metric counts.</returns>
    private static (Dictionary<int, int> Regular, Dictionary<string, int> Variable) CollectMetricCounts(
        IEnumerable<Poem> poems, bool includeVariableValuesInRegular)
    {
        var regular = new Dictionary<int, int>();
        var variable = new Dictionary<string, int>();
        foreach (var poem in poems)
        {
            if (poem.HasVariableMetric)
            {
                if (includeVariableValuesInRegular)
                {
                    foreach (var metric in poem.VerseLength.Split(','))
                    {
                        if (!int.TryParse(metric.Trim(), out var verseLength)) continue;
                        regular[verseLength] = regular.GetValueOrDefault(verseLength) + 1;
                    }
                }
                variable[poem.DetailedMetric] = variable.GetValueOrDefault(poem.DetailedMetric) + 1;
            }
            else
            {
                var verseLength = int.Parse(poem.VerseLength);
                regular[verseLength] = regular.GetValueOrDefault(verseLength) + 1;
            }
        }
        return (regular, variable);
    }

    private static List<DataLine> CreateRegularMetricDataLines(Dictionary<int, int> counts) =>
        counts.OrderBy(x => x.Key).Select(x => new DataLine(x.Key.ToString(), x.Value)).ToList();

    private static List<ColoredDataLine> CreateVariableMetricDataLines(Dictionary<string, int> counts) =>
        counts.OrderBy(x => x.Key).Select(x => new ColoredDataLine(x.Key, x.Value, "rgba(72, 149, 239, 1)")).ToList();

    private static List<ColoredDataLine> CreateMetricPieDataLines(Dictionary<int, int> counts) =>
        counts.OrderBy(x => x.Key).Select(x => new ColoredDataLine(
            $"{x.Key} {(x.Key == 1 ? "syllabe" : "syllabes")}", x.Value,
            MetricSettings.Metrics.First(m => m.Length == x.Key).Color)).ToList();

    /// <summary>
    /// Counts poems per non-empty season for the selected filter and optionally separates a stacked category.
    /// The first matching filter is applied in the same priority order as the chart generator.
    /// </summary>
    /// <param name="data">The seasons and poems to aggregate.</param>
    /// <param name="storageSubCategory">An optional storage subcategory filter.</param>
    /// <param name="storageCategory">An optional storage category filter.</param>
    /// <param name="forAcrostiche">Whether to count poems with an acrostic.</param>
    /// <param name="poemType">An optional poem type filter.</param>
    /// <param name="forVariableMetric">Whether to count poems with a variable metric.</param>
    /// <param name="forMetric">An optional verse metric filter.</param>
    /// <param name="extraTag">An optional extra-tag filter.</param>
    /// <param name="stackedItem">An optional item whose count is returned separately and subtracted from the main count.</param>
    /// <param name="backgroundColor">The color applied to the main and stacked data lines.</param>
    /// <returns>The main seasonal data lines and, when requested, the stacked data lines.</returns>
    private static (List<DataLine> Main, List<DataLine> Stacked) CreateSeasonalDataLines(Root data,
        string? storageSubCategory, string? storageCategory, bool forAcrostiche, PoemType? poemType,
        bool forVariableMetric, int? forMetric, string? extraTag, BarItem? stackedItem, string backgroundColor)
    {
        var main = new List<DataLine>();
        var stacked = new List<DataLine>();
        foreach (var season in data.Seasons.Where(x => x.Poems.Count > 0))
        {
            var poemCount = 0;
            if (storageSubCategory is not null)
                poemCount = season.Poems.Count(x => x.Categories.Any(c => c.SubCategories.Contains(storageSubCategory)));
            else if (storageCategory is not null)
                poemCount = season.Poems.Count(x => x.Categories.Any(c => c.Name == storageCategory));
            else if (forAcrostiche)
                poemCount = season.Poems.Count(x => x.Acrostiche is not null || x.DoubleAcrostiche is not null);
            else if (poemType is not null)
                poemCount = season.Poems.Count(x => x.HasType(poemType.Value));
            else if (forVariableMetric)
                poemCount = season.Poems.Count(x => x.HasVariableMetric);
            else if (forMetric is not null)
                poemCount = season.Poems.Count(x => x.HasMetric(forMetric.Value));
            else if (extraTag is not null)
                poemCount = season.Poems.Count(x => x.ExtraTags != null && x.ExtraTags.Contains(extraTag));

            var stackedCount = stackedItem?.Type switch
            {
                BarItemType.Category => season.Poems.Count(x => x.Categories.Any(c => c.Name == stackedItem.Name)),
                BarItemType.SubCategory => season.Poems.Count(x => x.Categories.Any(c => c.SubCategories.Contains(stackedItem.Name))),
                BarItemType.ExtraTag => season.Poems.Count(x => x.ExtraTags != null && x.ExtraTags.Contains(stackedItem.Name)),
                _ => 0
            };
            main.Add(new ColoredDataLine(season.TitleForChartsWithYears, poemCount - stackedCount, backgroundColor));
            if (stackedItem is not null)
                stacked.Add(new ColoredDataLine(stackedItem.Name, stackedCount, backgroundColor.Replace("0.5", "0.3")));
        }
        return (main, stacked);
    }

    private static List<DateTime> CollectPoemDates(IEnumerable<DateTime> frDates, IEnumerable<DateTime>? enDates)
    {
        var dates = frDates.ToList();
        if (enDates is not null) dates.AddRange(enDates);
        dates.Sort();
        return dates;
    }

    private static (Dictionary<DateTime, int> Intervals, Dictionary<int, int> IntervalCounts,
        List<(DateTime StartDate, DateTime EndDate, int DurationInDays, int PoemCount)> PoemSeries)
        CalculateIntervalsAndSeries(List<DateTime> dates)
    {
        var poemsPerDay = dates.GroupBy(date => date).ToDictionary(group => group.Key, group => group.Count());
        var poemDates = poemsPerDay.Keys.Order().ToList();
        var intervals = new Dictionary<DateTime, int>();
        var intervalCounts = new Dictionary<int, int>();
        var series = new List<(DateTime StartDate, DateTime EndDate, int DurationInDays, int PoemCount)>();
        if (poemDates.Count > 0)
        {
            var seriesStart = poemDates[0];
            var previous = poemDates[0];
            var seriesPoemCount = poemsPerDay[seriesStart];
            for (var i = 1; i < poemDates.Count; i++)
            {
                var current = poemDates[i];
                if ((current - previous).TotalDays == 1)
                    seriesPoemCount += poemsPerDay[current];
                else
                {
                    series.Add((seriesStart, previous, (previous - seriesStart).Days + 1, seriesPoemCount));
                    seriesStart = current;
                    seriesPoemCount = poemsPerDay[current];
                }
                previous = current;
            }
            series.Add((seriesStart, previous, (previous - seriesStart).Days + 1, seriesPoemCount));
        }
        for (var i = 1; i < dates.Count; i++)
        {
            var dayDiff = (int)(dates[i] - dates[i - 1]).TotalDays;
            intervalCounts[dayDiff] = intervalCounts.GetValueOrDefault(dayDiff) + 1;
            intervals.TryAdd(dates[i], dayDiff);
        }
        return (intervals, intervalCounts, series);
    }

    private static (List<ColoredDataLine> Lines, List<int> OrderedKeys) CreateIntervalDataLines(
        Dictionary<int, int> intervalLengthCounts)
    {
        var dataLines = new List<ColoredDataLine>();
        var orderedKeys = intervalLengthCounts.Keys.Order().ToList();
        const string zeroDayColor = "rgba(72, 149, 239, 1)";
        const string oneDayColor = "rgba(72, 149, 239, 0.9)";
        const string upToSevenDayColor = "rgba(72, 149, 239, 0.7)";
        const string upToOneMonthColor = "rgba(72, 149, 239, 0.5)";
        const string upToThreeMonthsColor = "rgba(72, 149, 239, 0.3)";
        const string upToOneYearColor = "rgba(72, 149, 239, 0.2)";
        const string moreThanOneYearColor = "rgba(72, 149, 239, 0.1)";
        var moreThanOneMonthCount = 0;
        var moreThanThreeMonthsCount = 0;
        var moreThanOneYearCount = 0;
        foreach (var key in orderedKeys)
        {
            if (key == 0) dataLines.Add(new("Moins d'un jour", intervalLengthCounts[key], zeroDayColor));
            else if (key == 1) dataLines.Add(new("Un jour", intervalLengthCounts[key], oneDayColor));
            else if (key < 8) dataLines.Add(new($"{key}j", intervalLengthCounts[key], upToSevenDayColor));
            else if (key < 31) dataLines.Add(new($"{key}j", intervalLengthCounts[key], upToOneMonthColor));
            else if (key < 91) moreThanOneMonthCount++;
            else if (key < 366) moreThanThreeMonthsCount++;
            else moreThanOneYearCount++;
        }
        if (moreThanOneMonthCount > 0) dataLines.Add(new("Entre un et trois mois", moreThanOneMonthCount, upToThreeMonthsColor));
        if (moreThanThreeMonthsCount > 0) dataLines.Add(new("Entre trois mois et un an", moreThanThreeMonthsCount, upToOneYearColor));
        if (moreThanOneYearCount > 0) dataLines.Add(new("Plus d'un an", moreThanOneYearCount, moreThanOneYearColor));
        return (dataLines, orderedKeys);
    }

    private static List<string> CreateLongestIntervalMarkdownLines(Dictionary<DateTime, int> intervals,
        List<int> orderedIntervalKeys)
    {
        var moreThanOneYearDates = new List<KeyValuePair<DateTime, DateTime>>();
        var moreThanThreeMonthsDates = new List<KeyValuePair<DateTime, DateTime>>();
        foreach (var key in orderedIntervalKeys.OrderDescending())
        {
            if (key < 91) break;
            foreach (var pair in intervals.Where(x => x.Value == key))
            {
                var dates = new KeyValuePair<DateTime, DateTime>(pair.Key.AddDays(-key), pair.Key);
                if (key < 366) moreThanThreeMonthsDates.Add(dates);
                else moreThanOneYearDates.Add(dates);
            }
        }
        var lines = new List<string> { "- Plus d'un an, du plus long au plus court :" };
        lines.AddRange(moreThanOneYearDates.Select(date => $"  - Du {date.Key:dd.MM.yyyy} au {date.Value:dd.MM.yyyy}"));
        lines.Add("- Plus de trois mois, du plus long au plus court :");
        lines.AddRange(moreThanThreeMonthsDates.Select(date => $"  - Du {date.Key:dd.MM.yyyy} au {date.Value:dd.MM.yyyy}"));
        return lines;
    }

    private static List<ColoredDataLine> CreateSeriesDataLines(
        List<(DateTime StartDate, DateTime EndDate, int DurationInDays, int PoemCount)> poemSeries)
    {
        var chartSeries = poemSeries.Where(series => series.PoemCount > 1).ToList();
        var seriesByPoemCount = new Dictionary<int, SortedSet<int>>();
        foreach (var series in chartSeries)
        {
            if (!seriesByPoemCount.TryGetValue(series.PoemCount, out var durations))
                seriesByPoemCount[series.PoemCount] = durations = [];
            durations.Add(series.DurationInDays);
        }
        return seriesByPoemCount.Keys.Order().Select(key =>
        {
            var durationLabel = string.Join(", ", seriesByPoemCount[key].Select(duration => $"{duration} jours"));
            var count = chartSeries.Count(series => series.PoemCount == key);
            return new ColoredDataLine($"{key} poèmes (en {durationLabel})", count, "rgba(72, 149, 239, 1)");
        }).ToList();
    }

    private static List<string> CreateLongestSeriesMarkdownLines(
        List<(DateTime StartDate, DateTime EndDate, int DurationInDays, int PoemCount)> poemSeries)
    {
        var lines = new List<string>();
        var longestSeriesKeys = poemSeries.Select(series => series.DurationInDays).Distinct()
            .OrderDescending().Take(6);
        foreach (var key in longestSeriesKeys)
        {
            var matchingSeries = poemSeries.Where(series => series.DurationInDays == key);
            lines.Add($"- {key} jours :");
            lines.AddRange(matchingSeries.Select(series =>
                $"  - Du {series.StartDate:dd.MM.yyyy} au {series.EndDate:dd.MM.yyyy} ({series.PoemCount} poème{(series.PoemCount > 1 ? "s" : string.Empty)})"));
        }
        return lines;
    }

    /// <summary>
    /// Updates the color of a variable-metric data line according to the parity of its metric values.
    /// The color mapping is determined by the combination of even and odd values in the metric.
    /// If the label cannot be parsed as an integer array, the original data line is returned unchanged.
    /// </summary>
    /// <param name="coloredDataLine">The data line whose color should be evaluated.</param>
    /// <returns>A data line with the calculated variable-metric color, or the original line when parsing fails.</returns>
    private static ColoredDataLine UpdateVariableMetricColor(ColoredDataLine coloredDataLine)
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
