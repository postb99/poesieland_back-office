using System.Globalization;
using Microsoft.Extensions.Configuration;
using Toolbox.Domain;
using Toolbox.Settings;

namespace Toolbox.Charts;

public class ChartDataFileGenerator(IConfiguration configuration)
{
    /// <summary>
    /// Generates a radar chart data file for poems categorized by the day of the month.
    /// The method processes poem data from the provided `Root` objects, optionally filtered
    /// by category or sub-category, and writes chart data files. If an optional extra tag
    /// flag is true, it will filter poems containing a special extra tag.
    /// Following files will be generated in appropriate locations:
    /// - poems-day-{storageSubCategory, i.e. "automne"}-radar.js,
    /// - poems-day-{storageCategory, i.e. "saisons"}-radar.js
    /// - poems-day-les-mois-radar.js, poems-day-noel-radar.js
    /// - poems-day-radar.js
    /// - days_without_creation.md
    /// </summary>
    /// <param name="data">The primary source of poem data containing seasons and poems information.</param>
    /// <param name="dataEn">The secondary source of poem data, used for "general" context.</param>
    /// <param name="storageSubCategory">The sub-category to filter poems. If null, sub-category filtering is skipped.</param>
    /// <param name="storageCategory">The category to filter poems. If null, category filtering is skipped.</param>
    /// <param name="forLesMoisExtraTag">A flag indicating whether to filter poems containing the tag "les mois".</param>
    /// <param name="forNoelExtraTag">A flag indicating whether to filter poems containing the tag "noël".</param>
    public void GeneratePoemsByDayRadarChartDataFile(Root data, Root dataEn,
        string? storageSubCategory, string? storageCategory,
        bool forLesMoisExtraTag = false, bool forNoelExtraTag = false)
    {
        var isGeneral = storageSubCategory is null && storageCategory is null && !forLesMoisExtraTag &&
                        !forNoelExtraTag;

        List<string> poemStringDates;

        if (storageSubCategory is not null)
        {
            poemStringDates = data.Seasons.SelectMany(x => x.Poems)
                .Where(x => x.Categories.Any(x => x.SubCategories.Contains(storageSubCategory))).Select(x => x.TextDate)
                .ToList();
        }
        else if (storageCategory is not null)
        {
            poemStringDates = data.Seasons.SelectMany(x => x.Poems)
                .Where(x => x.Categories.Any(x => x.Name == storageCategory)).Select(x => x.TextDate)
                .ToList();
        }
        else if (forLesMoisExtraTag)
        {
            poemStringDates = data.Seasons.SelectMany(x => x.Poems)
                .Where(x => x.ExtraTags != null && x.ExtraTags.Contains("les mois")).Select(x => x.TextDate)
                .ToList();
        }
        else if (forNoelExtraTag)
        {
            poemStringDates = data.Seasons.SelectMany(x => x.Poems)
                .Where(x => x.ExtraTags != null && x.ExtraTags.Contains("noël")).Select(x => x.TextDate)
                .ToList();
        }
        else
        {
            // General
            poemStringDates = data.Seasons.SelectMany(x => x.Poems).Select(x => x.TextDate).ToList();

            // Add EN poems
            poemStringDates.AddRange(dataEn.Seasons.SelectMany(x => x.Poems).Select(x => x.TextDate));
        }

        var dataDict = ChartDataFileHelper.InitMonthDayDictionary();

        foreach (var poemStringDate in poemStringDates)
        {
            var year = poemStringDate.Substring(6);
            if (year == "1994")
                continue;
            var monthDay = $"{poemStringDate.Substring(3, 2)}-{poemStringDate.Substring(0, 2)}";
            dataDict[monthDay]++;
        }

        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            configuration[Constants.CHART_DATA_FILES_ROOT_DIR]!);
        var fileName = string.Empty;

        var chartId = string.Empty;
        var borderColor = string.Empty;

        if (storageSubCategory is not null)
        {
            // categories
            fileName = $"poems-day-{storageSubCategory.UnaccentedCleaned()}-radar.js";
            chartId = $"poemDay-{storageSubCategory.UnaccentedCleaned()}Radar";
            borderColor = configuration.GetSection(Constants.STORAGE_SETTINGS).Get<StorageSettings>()!.Categories
                .SelectMany(x => x.Subcategories).FirstOrDefault(x => x.Name == storageSubCategory)!.Color;

            switch (borderColor)
            {
                // Use some not too light colors
                case "rgba(254, 231, 240, 1)":
                    borderColor = "rgba(255, 194, 209, 1)";
                    break;
                case "rgba(247, 235, 253, 1)":
                    borderColor = "rgba(234, 191, 250, 1)";
                    break;
                case "rgba(244, 254, 254, 1)":
                    borderColor = "rgba(119, 181, 254, 1)";
                    break;
            }
        }
        else if (storageCategory is not null)
        {
            // tags
            fileName = $"poems-day-{storageCategory.UnaccentedCleaned()}-radar.js";
            chartId = $"poemDay-{storageCategory.UnaccentedCleaned()}Radar";
            borderColor = configuration.GetSection(Constants.STORAGE_SETTINGS).Get<StorageSettings>()!.Categories
                .FirstOrDefault(x => x.Name == storageCategory)!.Color;
        }
        else if (forLesMoisExtraTag)
        {
            fileName = "poems-day-les-mois-radar.js";
            chartId = "poemDayLesMoisRadar";
        }
        else if (forNoelExtraTag)
        {
            fileName = "poems-day-noel-radar.js";
            chartId = "poemDayNoelRadar";
        }
        else
        {
            // general
            fileName = "poems-day-radar.js";
            chartId = "poemDayRadar";
        }

        using var streamWriter = new StreamWriter(Path.Combine(rootDir, isGeneral ? "general" : "taxonomy", fileName));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartType.Radar);
        chartDataFileHelper.WriteBeforeData();

        var dataLines = new List<DataLine>();

        var dayWithoutPoems = new List<string>();

        foreach (var monthDay in dataDict.Keys)
        {
            var value = dataDict[monthDay];
            dataLines.Add(new(ChartDataFileHelper.GetRadarChartLabel(monthDay), value
            ));
            if (isGeneral && value == 0)
            {
                dayWithoutPoems.Add(monthDay);
            }
        }

        chartDataFileHelper.WriteData(dataLines, true);

        var backgroundColor = borderColor?.Replace("1)", "0.5)");

        var title = $"Mois les plus représentés : {string.Join(", ", GetTopMostMonths(dataDict))}";

        chartDataFileHelper.WriteAfterData(chartId, [title], borderColor, backgroundColor);
        streamWriter.Close();

        if (!isGeneral) return;

        // Days without poems listing

        var filePath = Path.Combine(Directory.GetCurrentDirectory(), configuration[Constants.CONTENT_ROOT_DIR]!,
            "../includes/days_without_creation.md");
        var streamWriter2 = new StreamWriter(filePath);

        streamWriter2.WriteLine("+++");
        streamWriter2.WriteLine("title = \"Les jours sans\"");
        streamWriter2.WriteLine("+++");

        foreach (var monthDay in dayWithoutPoems)
        {
            var splitted = monthDay.Split('-');
            streamWriter2.WriteLine(
                $"- {splitted[1].TrimStart('0')} {ChartDataFileHelper.GetRadarChartLabel($"{splitted[0]}-01").ToLower()}");
        }

        streamWriter2.Close();
    }

    /// <summary>
    /// Generates bar and pie chart data files for poem lengths, categorized by season or in a general context.
    /// The method processes poem data from the given `Root` object and creates a bar chart file if a season ID
    /// is provided, or a pie chart file if the data is for the general context. The files are organized into
    /// appropriate sub-directories, and the data includes distribution of poem lengths and sonnet counts.
    /// </summary>
    /// <param name="data">The primary source of poem data containing seasons and poems information.</param>
    /// <param name="seasonId">The ID of the season to generate chart data for. If null, generates data for all seasons.</param>
    public void GeneratePoemsLengthBarAndPieChartDataFile(Root data, int? seasonId)
    {
        var isGeneral = seasonId is null;
        var fileName = isGeneral ? "poems-length-pie.js" : "poems-length-bar.js";
        var subDir = isGeneral ? "general" : $"season-{seasonId}";
        var chartId = isGeneral ? "poemLengthPie" : $"season{seasonId}PoemLengthBar";

        var poems = seasonId is not null
            ? data.Seasons.First(x => x.Id == seasonId).Poems
            : data.Seasons.SelectMany(x => x.Poems);

        var nbVersesData = new Dictionary<int, int>();
        var nbSonnets = 0;
        foreach (var poem in poems)
        {
            var nbVerses = poem.VersesCount;
            if (nbVersesData.TryGetValue(nbVerses, out _))
            {
                nbVersesData[nbVerses]++;
            }
            else
            {
                nbVersesData[nbVerses] = 1;
            }

            if (poem.IsSonnet)
            {
                nbSonnets++;
            }
        }

        var nbVersesRange = nbVersesData.Keys.Order().ToList();

        // General pie chart or Season's bar chart
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            configuration[Constants.CHART_DATA_FILES_ROOT_DIR]!);
        var subDirPath = Path.Combine(rootDir, subDir);
        Directory.CreateDirectory(subDirPath);
        using var streamWriter = new StreamWriter(Path.Combine(subDirPath, fileName));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter,
            isGeneral ? ChartType.Pie : ChartType.Bar, isGeneral ? 1 : 2);
        chartDataFileHelper.WriteBeforeData();

        if (isGeneral)
        {
            var metrics = configuration.GetSection(Constants.METRIC_SETTINGS).Get<MetricSettings>()!.Metrics;
            var coloredDataLines = new List<ColoredDataLine>();

            foreach (var nbVerses in nbVersesRange)
            {
                var lookup = nbVerses switch
                {
                    3 => 0,
                    26 => 1,
                    _ => nbVerses / 2
                };

                var color = metrics.First(m => m.Length == lookup).Color;
                coloredDataLines.Add(new(nbVerses.ToString(),
                    nbVersesData[nbVerses], color));
            }

            chartDataFileHelper.WriteData(coloredDataLines, true);

            chartDataFileHelper.WriteAfterData(chartId, ["Poèmes"]);
        }
        else
        {
            var nbVersesChartData = new List<DataLine>();
            var isSonnetChartData = new List<DataLine>();

            foreach (var nbVerses in nbVersesRange)
            {
                isSonnetChartData.Add(new(string.Empty, 0));

                nbVersesChartData.Add(new(nbVerses.ToString(),
                    nbVersesData[nbVerses]));
            }

            var index = nbVersesRange.FindIndex(x => x == 14);
            if (index != -1)
            {
                isSonnetChartData[index] = new("Sonnets", nbSonnets);
                nbVersesChartData[index] =
                    new(nbVersesChartData[index].Label, nbVersesChartData[index].Value - nbSonnets);
            }

            string[] chartTitles;
            if (nbSonnets > 0)
            {
                chartDataFileHelper.WriteData(nbVersesChartData, false);
                chartDataFileHelper.WriteData(isSonnetChartData, true);
                chartTitles = ["Poèmes", "Sonnets"];
            }
            else
            {
                chartDataFileHelper.WriteData(nbVersesChartData, true);
                chartTitles = ["Poèmes"];
            }

            chartDataFileHelper.WriteAfterData(chartId, chartTitles,
                customScalesOptions: "scales: { y: { ticks: { stepSize: 1 } } }");
        }

        streamWriter.Close();
    }

    /// <summary>
    /// Generates a pie chart data file for categories within a season or across all seasons.
    /// The method processes poem data from the provided `Root` object, optionally filtered
    /// by a specific season identifier, and writes the resulting chart data to a "categories-pie.js" file
    /// in the appropriate directory structure.
    /// </summary>
    /// <param name="data">The root object containing all seasons and poems data to be processed for chart generation.</param>
    /// <param name="seasonId">
    /// An optional season identifier to filter poems by season. If set to null, the chart data will include poems across all seasons.
    /// </param>
    public void GenerateSeasonCategoriesPieChartDataFile(Root data, int? seasonId)
    {
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            configuration[Constants.CHART_DATA_FILES_ROOT_DIR]!);
        var subDir = seasonId.HasValue ? $"season-{seasonId}" : "general";
        var storageSettings = configuration.GetSection(Constants.STORAGE_SETTINGS).Get<StorageSettings>()!;
        using var streamWriter = new StreamWriter(Path.Combine(rootDir, subDir, "categories-pie.js"));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartType.Pie);
        chartDataFileHelper.WriteBeforeData();
        var byStorageSubcategoryCount = new Dictionary<string, int>();

        var season = seasonId.HasValue ? data.Seasons.First(x => x.Id == seasonId) : null;
        foreach (var poem in season?.Poems ?? data.Seasons.SelectMany(x => x.Poems))
        {
            foreach (var subCategory in poem.Categories.SelectMany(x => x.SubCategories))
            {
                if (byStorageSubcategoryCount.TryGetValue(subCategory, out _))
                {
                    byStorageSubcategoryCount[subCategory]++;
                }
                else
                {
                    byStorageSubcategoryCount[subCategory] = 1;
                }
            }
        }

        var orderedSubcategories =
            storageSettings.Categories.SelectMany(x => x.Subcategories).Select(x => x.Name).ToList();
        var pieChartData = new List<ColoredDataLine>();

        foreach (var subcategory in orderedSubcategories)
        {
            if (byStorageSubcategoryCount.TryGetValue(subcategory, out var value))
                pieChartData.Add(new(subcategory, value,
                    storageSettings.Categories.SelectMany(x => x.Subcategories)
                        .First(x => x.Name == subcategory).Color
                ));
        }

        chartDataFileHelper.WriteData(pieChartData);

        chartDataFileHelper.WriteAfterData(seasonId.HasValue ? $"season{seasonId}Pie" : "categoriesPie",
        [
            seasonId.HasValue ? $"{season.EscapedTitleForChartsWithPeriod}" : string.Empty
        ]);
        streamWriter.Close();
    }

    public void GeneratePoemsEnByDayRadarChartDataFile(Root dataEn)
    {
        var poemStringDates = dataEn.Seasons.SelectMany(x => x.Poems).Select(x => x.TextDate).ToList();

        var dataDict = ChartDataFileHelper.InitMonthDayDictionary();

        foreach (var poemStringDate in poemStringDates)
        {
            var year = poemStringDate.Substring(6);
            var day = $"{poemStringDate.Substring(3, 2)}-{poemStringDate.Substring(0, 2)}";
            dataDict[day]++;
        }

        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            configuration[Constants.CONTENT_ROOT_DIR_EN]!, "../charts/general");

        var fileName = "poems-en-day-radar.js";
        var chartId = "poemEnDayRadar";

        using var streamWriter = new StreamWriter(Path.Combine(rootDir, fileName));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartType.Radar);
        chartDataFileHelper.WriteBeforeData();

        var dataLines = new List<DataLine>();

        foreach (var monthDay in dataDict.Keys)
        {
            var value = dataDict[monthDay];
            dataLines.Add(new(ChartDataFileHelper.GetRadarEnChartLabel(monthDay), value
            ));
        }

        chartDataFileHelper.WriteData(dataLines, true);

        chartDataFileHelper.WriteAfterData(chartId, ["Poems by day of year"], string.Empty, string.Empty);
        streamWriter.Close();
    }

    /// <summary>
    /// Generates a radar chart data file for poems organized by the day of the year for a specific year.
    /// The method processes poem data from the provided `Root` object, filtering by the specified year,
    /// and writes chart data files in the format required for radar charts.
    /// The generated file is named in the format: poems-day-{year}-radar.js
    /// </summary>
    /// <param name="data">The primary source of poem data containing seasons and poems information.</param>
    /// <param name="year">The year for which poems should be filtered and represented in the chart.</param>
    public void GeneratePoemsOfYearByDayRadarChartDataFile(Root data, int year)
    {
        var poemStringDates = data.Seasons.SelectMany(x => x.Poems)
            .Where(x => x.Date.Year == year).Select(x => x.TextDate)
            .ToList();

        var dataDict = ChartDataFileHelper.InitMonthDayDictionary();

        foreach (var poemStringDate in poemStringDates)
        {
            var day = $"{poemStringDate.Substring(3, 2)}-{poemStringDate.Substring(0, 2)}";
            dataDict[day]++;
        }

        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            configuration[Constants.CHART_DATA_FILES_ROOT_DIR]!);
        var fileName = $"poems-day-{year}-radar.js";
        var chartId = $"poemDay-{year}Radar";

        using var streamWriter = new StreamWriter(Path.Combine(rootDir, "taxonomy", fileName));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartType.Radar);
        chartDataFileHelper.WriteBeforeData();

        var dataLines = new List<DataLine>();

        foreach (var monthDay in dataDict.Keys)
        {
            var value = dataDict[monthDay];
            dataLines.Add(new(ChartDataFileHelper.GetRadarChartLabel(monthDay), value));
        }

        chartDataFileHelper.WriteData(dataLines, true);

        chartDataFileHelper.WriteAfterData(chartId, ["Poèmes selon le jour de l\\\'année"], string.Empty,
            string.Empty);
        streamWriter.Close();
    }

    /// <summary>
    /// Generates bar and pie chart data files based on poem metrics, such as verse length,
    /// and categorizes them by seasons or a general context.
    /// This method processes the given poems from the Root object and distinguishes regular,
    /// variable, and undefined metrics. It outputs:
    /// - A pie chart file `poems-verse-length-pie.js` when a general context is used (seasonId is null).
    /// - A bar chart file `poems-verse-length-bar.js` when a specific season is selected.
    /// - A bar chart file `metrique_variable-bar.js` when a general context is used (seasonId is null).
    /// </summary>
    /// <param name="data">The root object containing seasons and poems data.</param>
    /// <param name="seasonId">An optional season identifier to filter poems by season. If null, all seasons are included.</param>
    public void GeneratePoemMetricBarAndPieChartDataFile(Root data, int? seasonId)
    {
        var isGeneral = seasonId is null;
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            configuration[Constants.CHART_DATA_FILES_ROOT_DIR]!);
        var fileName = isGeneral ? "poems-verse-length-pie.js" : "poems-verse-length-bar.js";
        var subDir = isGeneral ? "general" : $"season-{seasonId}";
        var chartId = isGeneral ? "poemVerseLengthPie" : $"season{seasonId}VerseLengthBar";
        using var streamWriter = new StreamWriter(Path.Combine(rootDir, subDir, fileName));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter,
            isGeneral ? ChartType.Pie : ChartType.Bar, 1);
        chartDataFileHelper.WriteBeforeData();
        var regularMetricData = new Dictionary<int, int>();
        var variableMetricData = new Dictionary<string, int>();
        var nbUndefinedVerseLength = 0;
        var poems = seasonId is not null
            ? data.Seasons.First(x => x.Id == seasonId).Poems
            : data.Seasons.SelectMany(x => x.Poems);

        foreach (var poem in poems)
        {
            if (string.IsNullOrEmpty(poem.VerseLength))
            {
                nbUndefinedVerseLength++;
            }
            else if (poem.HasVariableMetric)
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

        var undefinedVerseLengthChartData = new ColoredDataLine
        ("Pas de données pour l\\'instant", nbUndefinedVerseLength, "rgb(211, 211, 211)"
        );

        // General pie chart or Season's bar chart
        var dataLines = new List<DataLine>();

        if (isGeneral)
        {
            var metrics = configuration.GetSection(Constants.METRIC_SETTINGS).Get<MetricSettings>()!.Metrics;
            var coloredDataLines = new List<ColoredDataLine>();

            foreach (var metricValue in regularMetricRange)
            {
                var term = metricValue == 1 ? "syllabe" : "syllabes";
                coloredDataLines.Add(new($"{metricValue} {term}",
                    regularMetricData[metricValue], metrics.First(m => m.Length == metricValue).Color));
            }

            chartDataFileHelper.WriteData(coloredDataLines, true);

            chartDataFileHelper.WriteAfterData(chartId, ["Poèmes"]);
            streamWriter.Close();
        }
        else
        {
            dataLines.AddRange(regularMetricChartData);
            dataLines.AddRange(variableMetricChartData);
            if (nbUndefinedVerseLength > 0)
                dataLines.Add(undefinedVerseLengthChartData);

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
            using var streamWriter2 = new StreamWriter(Path.Combine(rootDir, "general", fileName));
            var chartDataFileHelper2 = new ChartDataFileHelper(streamWriter2, ChartType.Bar, 1);
            chartDataFileHelper2.WriteBeforeData();

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
    /// Generates a pie chart data file representing the intensity of poem creation by counting the number of poems created on each day.
    /// The method aggregates poem data from two `Root` objects, processes the intensity of poem creation,
    /// and generates output files including a pie chart data file `poem-intensity-pie.js`
    /// and a markdown file `most_intense_days.md` listing the most intense creation days.
    /// </summary>
    /// <param name="data">The primary source of poem data containing seasons and poems information.</param>
    /// <param name="dataEn">The secondary source of poem data, used for "general" context.</param>
    public void GeneratePoemIntensityPieChartDataFile(Root data, Root dataEn)
    {
        var dataDict = new Dictionary<string, int>();

        var fullDates = data.Seasons.SelectMany(x => x.Poems).Select(x => x.TextDate)
            .Where(x => x != "01.01.1994").ToList();

        // Add EN poems
        fullDates.AddRange(dataEn.Seasons.SelectMany(x => x.Poems).Select(x => x.TextDate));

        foreach (var fullDate in fullDates)
        {
            if (!dataDict.TryAdd(fullDate, 1))
            {
                dataDict[fullDate]++;
            }
        }

        var intensityDict = new Dictionary<int, int>();

        foreach (var dataDictItem in dataDict)
        {
            var value = dataDictItem.Value;
            if (!intensityDict.TryAdd(value, 1))
            {
                intensityDict[value]++;
            }
        }

        var dataLines = new List<DataLine>();
        var orderedIntensitiesKeys = intensityDict.Keys.Order();
        var baseColor = "rgba(72, 149, 239, {0})";
        var baseAlpha = 0.5;
        foreach (var key in orderedIntensitiesKeys)
        {
            if (key == 0) continue;
            dataLines.Add(new ColoredDataLine($"{key} {(key == 1 ? "poème" : "poèmes")}",
                intensityDict[key],
                string.Format(baseColor,
                    (baseAlpha + 0.1 * (key - 1)).ToString(new NumberFormatInfo
                        { NumberDecimalSeparator = ".", NumberDecimalDigits = 1 }))));
        }

        var fileName = "poem-intensity-pie.js";
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            configuration[Constants.CHART_DATA_FILES_ROOT_DIR]!);
        using var streamWriter = new StreamWriter(Path.Combine(rootDir, "general", fileName));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartType.Pie);
        chartDataFileHelper.WriteBeforeData();
        chartDataFileHelper.WriteData(dataLines, true);
        chartDataFileHelper.WriteAfterData("poemIntensityPie", ["Les jours de création sont-ils intenses ?"]);
        streamWriter.Close();

        // Most intense days content file
        var intensityKeys = intensityDict.Keys.OrderDescending().Where(x => x > 2);
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), configuration[Constants.CONTENT_ROOT_DIR]!,
            "../includes/most_intense_days.md");
        var streamWriter2 = new StreamWriter(filePath);

        streamWriter2.WriteLine("+++");
        streamWriter2.WriteLine("title = \"Les jours les plus intenses\"");
        streamWriter2.WriteLine("+++");

        foreach (var key in intensityKeys)
        {
            streamWriter2.WriteLine($"- {key} poèmes en un jour :");
            var matchingIntensities = dataDict.Where(x => x.Value == key).Select(x => x.Key);
            // ReSharper disable once PossibleMultipleEnumeration
            var years = matchingIntensities.Select(x => x.Substring(6)).Distinct();

            foreach (var year in years)
            {
                // ReSharper disable once PossibleMultipleEnumeration
                var dates = matchingIntensities.Where(x => x.Substring(6) == year).Select(x => x.ToDateTime()).Order();
                streamWriter2.Write($"  - {year} : ");
                streamWriter2.WriteLine(string.Join(", ", dates.Select(x => x.ToString("ddd dd MMM"))));
            }
        }

        streamWriter2.Close();
    }

    /// <summary>
    /// Retrieves the top most represented months from the provided dictionary of month-day data.
    /// The method aggregates the data by month, ranks the months by their total occurrences,
    /// and returns a list of the top four months with the highest occurrence counts.
    /// </summary>
    /// <param name="monthDayDict">A dictionary where the keys are month-day strings in the format "MM-DD"
    /// and the values are the associated occurrence counts.</param>
    /// <returns>A list of the top four month names, ordered by their occurrence counts in descending order.</returns>
    public List<string> GetTopMostMonths(Dictionary<string, int> monthDayDict)
    {
        var monthDict = new Dictionary<string, int>();
        foreach (var monthDay in monthDayDict.Keys)
        {
            var month = monthDay.Substring(0, 2);
            if (monthDict.TryGetValue(month, out _))
            {
                monthDict[month] += monthDayDict[monthDay];
            }
            else
            {
                monthDict.Add(month, monthDayDict[monthDay]);
            }
        }

        return monthDict.OrderByDescending(x => x.Value).Take(4).Select(x => x.Key)
            .Select(ChartDataFileHelper.GetMonthLabel).ToList();
    }

    /// <summary>
    /// Updates the color of a given metric data line based on the parity of the metric values.
    /// The color mapping is determined by the combination of even and odd values in the metric.
    /// </summary>
    /// <param name="coloredDataLine">The metric data line for which the color needs to be updated.</param>
    /// <returns>A new <see cref="ColoredDataLine"/> object with the updated color information.
    /// If the format of the metric data cannot be parsed, the original data line is returned.</returns>
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