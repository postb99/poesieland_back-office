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
    /// <param name="data">The primary source of French poems data.</param>
    /// <param name="dataEn">The secondary source of English poems data.</param>
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
    /// Generates pie chart data file for poem lengths.
    /// The data includes distribution of poem lengths and sonnet counts.
    /// </summary>
    /// <param name="data">The primary source of French poems data.</param>
    public void GeneratePoemsLengthBarAndPieChartDataFile(Root data)
    {
        var poems = data.Seasons.SelectMany(x => x.Poems);

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

        // General pie chart
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            configuration[Constants.CHART_DATA_FILES_ROOT_DIR]!);
        var subDirPath = Path.Combine(rootDir, "general");
        Directory.CreateDirectory(subDirPath);
        using var streamWriter = new StreamWriter(Path.Combine(subDirPath, "poems-length-pie.js"));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter,
            ChartType.Pie, 1);
        chartDataFileHelper.WriteBeforeData();

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

        chartDataFileHelper.WriteAfterData("poemLengthPie", ["Poèmes"]);

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
            seasonId.HasValue ? $"{season!.EscapedTitleForChartsWithPeriod}" : string.Empty
        ]);
        streamWriter.Close();
    }
    
    /// <summary>
    /// Generates a pie chart data file for categories within a year.
    /// The method processes poem data from the provided `Root` object, filtered
    /// by a specific year, and writes the resulting chart data to a "categories-{year}-pie.js" file.
    /// </summary>
    /// <param name="data">The root object containing all seasons and poems data to be processed for chart generation.</param>
    /// <param name="year">A year to filter poems by date.</param>
    public void GenerateYearCategoriesPieChartDataFile(Root data, int year)
    {
        var poems = data.Seasons.SelectMany(x => x.Poems).Where(x => x.Date.Year == year);
        if (!poems.Any()) return;
        
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            configuration[Constants.CHART_DATA_FILES_ROOT_DIR]!);
        var storageSettings = configuration.GetSection(Constants.STORAGE_SETTINGS).Get<StorageSettings>()!;
        using var streamWriter = new StreamWriter(Path.Combine(rootDir, "taxonomy", $"categories-{year}-pie.js"));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartType.Pie);
        chartDataFileHelper.WriteBeforeData();
        var byStorageSubcategoryCount = new Dictionary<string, int>();
        
        foreach (var poem in poems)
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

        chartDataFileHelper.WriteAfterData($"categories{year}Pie",
        [
            year.ToString()
        ]);
        streamWriter.Close();
    }

    /// <summary>
    /// Generates a radar chart data file for English poems categorized by the day of the year.
    /// The method processes poem data from the provided `Root` object, extracts dates of poems,
    /// and compiles a dataset representing the number of poems created on each day throughout
    /// the year. The resulting chart data is written to a specified file.
    /// </summary>
    /// <param name="dataEn">The source of English poems.</param>
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
    /// The generated file is named in the format: "poems-day-{year}-radar.js".
    /// </summary>
    /// <param name="data">The primary source of French poems data.</param>
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
    /// - A pie chart file "poems-verse-length-pie.js" when a general context is used (seasonId is null).
    /// - A bar chart file "poems-verse-length-bar.js" when a specific season is selected.
    /// - A bar chart file "metrique_variable-bar.js" when a general context is used (seasonId is null).
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
    /// and generates output files: a pie chart data file "poem-intensity-pie.js"
    /// and a markdown file "most_intense_days.md" listing the most intense creation days.
    /// </summary>
    /// <param name="data">The primary source of French poems data.</param>
    /// <param name="dataEn">The secondary source of English poems data.</param>
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
    /// Processes and generates a pie chart data file representing the distribution of poems
    /// categorized by the day of the week on which they were written. This method consolidates
    /// data from two `Root` objects (primary and secondary sources) and calculates poem counts
    /// for each day of the week, identified as Monday through Sunday.
    /// The generated "poem-dayofweek-pie.js" file will include visual data for each day of the week with corresponding
    /// values and colors.
    /// </summary>
    /// <param name="data">The primary source of French poems data.</param>
    /// <param name="dataEn">The secondary source of English poems data.</param>
    public void GeneratePoemByDayOfWeekPieChartDataFile(Root data, Root dataEn)
    {
        var dataDict = new Dictionary<int, int>();

        var dayOfWeekData = data.Seasons.SelectMany(x => x.Poems).Where(x => x.TextDate != "01.01.1994")
            .Select(x => x.Date.DayOfWeek).ToList();

        // Add EN poems
        dayOfWeekData.AddRange(dataEn.Seasons.SelectMany(x => x.Poems).Select(x => x.Date.DayOfWeek));

        foreach (var dayOfWeek in dayOfWeekData)
        {
            if (!dataDict.TryAdd((int)dayOfWeek, 1))
            {
                dataDict[(int)dayOfWeek]++;
            }
        }

        var dataLines = new List<DataLine>();
        var baseColor = "rgba(72, 149, 239, {0})";
        var baseAlpha = 0.2;
        int[] daysOfWeek = [1, 2, 3, 4, 5, 6, 0];
        foreach (var key in daysOfWeek)
        {
            dataLines.Add(new ColoredDataLine(
                key == 1 ? "Lundi" :
                key == 2 ? "Mardi" :
                key == 3 ? "Mercredi" :
                key == 4 ? "Jeudi" :
                key == 5 ? "Vendredi" :
                key == 6 ? "Samedi" : "Dimanche",
                dataDict[key],
                string.Format(baseColor,
                    (baseAlpha + 0.1 * (key == 0 ? 7 : key)).ToString(new NumberFormatInfo
                        { NumberDecimalSeparator = ".", NumberDecimalDigits = 1 }))));
        }

        var fileName = "poem-dayofweek-pie.js";
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            configuration[Constants.CHART_DATA_FILES_ROOT_DIR]!);
        using var streamWriter = new StreamWriter(Path.Combine(rootDir, "general", fileName));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartType.Pie);
        chartDataFileHelper.WriteBeforeData();
        chartDataFileHelper.WriteData(dataLines, true);
        chartDataFileHelper.WriteAfterData("poemDayOfWeekPie", ["Par jour de la semaine"]);
        streamWriter.Close();
    }

    /// <summary>
    /// Generates a pie chart data file for English poems based on the day of the week they are dated.
    /// The method processes poems from the given `Root` object and categorizes them by the day of the week.
    /// The generated "poem-en-dayofweek-pie.js" file will include visual data for each day of the week with corresponding
    /// values and colors.
    /// </summary>
    /// <param name="dataEn">The source of English poems data.</param>
    public void GenerateEnPoemByDayOfWeekPieChartDataFile(Root dataEn)
    {
        var dataDict = new Dictionary<int, int>();

        var dayOfWeekData = dataEn.Seasons.SelectMany(x => x.Poems).Select(x => x.Date.DayOfWeek);

        foreach (var dayOfWeek in dayOfWeekData)
        {
            if (!dataDict.TryAdd((int)dayOfWeek, 1))
            {
                dataDict[(int)dayOfWeek]++;
            }
        }

        var dataLines = new List<DataLine>();
        var baseColor = "rgba(72, 149, 239, {0})";
        var baseAlpha = 0.2;
        int[] daysOfWeek = [1, 2, 3, 4, 5, 6, 0];
        foreach (var key in daysOfWeek)
        {
            dataLines.Add(new ColoredDataLine(
                key == 1 ? "Monday" :
                key == 2 ? "Tuesday" :
                key == 3 ? "Wednesday" :
                key == 4 ? "Thursday" :
                key == 5 ? "Friday" :
                key == 6 ? "Saturday" : "Sunday",
                dataDict[key],
                string.Format(baseColor,
                    (baseAlpha + 0.1 * (key == 0 ? 7 : key)).ToString(new NumberFormatInfo
                        { NumberDecimalSeparator = ".", NumberDecimalDigits = 1 }))));
        }

        var fileName = "poem-en-dayofweek-pie.js";
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            configuration[Constants.CONTENT_ROOT_DIR_EN]!);
        using var streamWriter = new StreamWriter(Path.Combine(rootDir, "../charts/general", fileName));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartType.Pie);
        chartDataFileHelper.WriteBeforeData();
        chartDataFileHelper.WriteData(dataLines, true);
        chartDataFileHelper.WriteAfterData("poemEnDayOfWeekPie", ["By day of week"]);
        streamWriter.Close();
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
    /// <param name="forSonnet">A flag indicating to include poems of type "sonnet".</param>
    /// <param name="forPantoun">A flag indicating to include poems of type "pantoun".</param>
    /// <param name="forVariableMetric">A flag indicating to include poems with variable metrics.</param>
    /// <param name="forRefrain">A flag indicating to include poems with a refrain.</param>
    /// <param name="forMetric">An optional numeric metric filter for poems.</param>
    /// <param name="forLovecat">A flag indicating to include poems in the "lovecat" category.</param>
    /// <param name="forLesMois">A flag indicating to include poems categorized under "les mois".</param>
    public void GenerateOverSeasonsChartDataFile(Root data, string? storageSubCategory, string? storageCategory,
        bool forAcrostiche = false, bool forSonnet = false, bool forPantoun = false, bool forVariableMetric = false,
        bool forRefrain = false, int? forMetric = null, bool forLovecat = false, bool forLesMois = false)
    {
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            configuration[Constants.CHART_DATA_FILES_ROOT_DIR]!);
        var fileName = string.Empty;

        var chartId = string.Empty;
        var borderColor = "rgba(72, 149, 239, 1)";

        if (storageSubCategory is not null)
        {
            fileName = $"poems-{storageSubCategory.UnaccentedCleaned()}-bar.js";
            chartId = $"poems-{storageSubCategory.UnaccentedCleaned()}Bar";
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
            fileName = $"poems-{storageCategory.UnaccentedCleaned()}-bar.js";
            chartId = $"poems-{storageCategory.UnaccentedCleaned()}Bar";
            borderColor = configuration.GetSection(Constants.STORAGE_SETTINGS).Get<StorageSettings>()!.Categories
                .FirstOrDefault(x => x.Name == storageCategory)!.Color;
        }
        else if (forAcrostiche)
        {
            fileName = $"poems-acrostiche-bar.js";
            chartId = $"poems-acrosticheBar";
        }
        else if (forSonnet)
        {
            fileName = $"poems-sonnet-bar.js";
            chartId = $"poems-sonnetBar";
        }
        else if (forPantoun)
        {
            fileName = $"poems-pantoun-bar.js";
            chartId = $"poems-pantounBar";
        }
        else if (forVariableMetric)
        {
            fileName = $"poems-metrique_variable-bar.js";
            chartId = $"poems-metrique_variableBar";
        }
        else if (forRefrain)
        {
            fileName = $"poems-refrain-bar.js";
            chartId = $"poems-refrainBar";
        }
        else if (forMetric is not null)
        {
            fileName = $"poems-metric-{forMetric}-bar.js";
            chartId = $"poems-metric{forMetric}Bar";
        }
        else if (forLovecat)
        {
            fileName = $"poems-lovecat-bar.js";
            chartId = $"poems-lovecatBar";
        }
        else if (forLesMois)
        {
            fileName = $"poems-les-mois-bar.js";
            chartId = $"poems-les-moisBar";
        }

        var backgroundColor = borderColor.Replace("1)", "0.5)");
        if (forVariableMetric)
        {
            // For this one, same color to look nice below other bar chart
            backgroundColor = borderColor;
        }

        using var streamWriter = new StreamWriter(Path.Combine(rootDir, "taxonomy", fileName));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartType.Bar);
        chartDataFileHelper.WriteBeforeData();

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
            else if (forSonnet)
            {
                poemCount = season.Poems.Count(x => x.IsSonnet);
            }
            else if (forPantoun)
            {
                poemCount = season.Poems.Count(x => x.IsPantoun);
            }
            else if (forVariableMetric)
            {
                poemCount = season.Poems.Count(x => x.HasVariableMetric);
            }
            else if (forRefrain)
            {
                poemCount = season.Poems.Count(x => x.ExtraTags != null && x.ExtraTags.Contains("refrain"));
            }
            else if (forMetric is not null)
            {
                poemCount = season.Poems.Count(x => x.HasMetric(forMetric.Value));
            }
            else if (forLovecat)
            {
                poemCount = season.Poems.Count(x => x.ExtraTags != null && x.ExtraTags.Contains("lovecat"));
            }
            else if (forLesMois)
            {
                poemCount = season.Poems.Count(x => x.ExtraTags != null && x.ExtraTags.Contains("les mois"));
            }

            dataLines.Add(new ColoredDataLine($"{season.EscapedTitleForChartsWithYears}",
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
                    new("Moins d\\'un jour", intervalLengthDict[key],
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
            dataLines.Add(new("Plus d\\'un an", moreThanOneYearCount,
                moreThanOneYearColor));

        var fileName = "poem-interval-bar.js";
        var subDir = seasonId is not null ? $"season-{seasonId}" : "general";
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            configuration[Constants.CHART_DATA_FILES_ROOT_DIR]!);
        using var streamWriter = new StreamWriter(Path.Combine(rootDir, subDir, fileName));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartType.Bar);
        chartDataFileHelper.WriteBeforeData();
        chartDataFileHelper.WriteData(dataLines, true);
        chartDataFileHelper.WriteAfterData(seasonId is null ? "poemIntervalBar" : $"season{seasonId}PoemIntervalBar",
            ["Fréquence"],
            customScalesOptions: seasonId is null ? string.Empty : "scales: { y: { ticks: { stepSize: 1 } } }");
        streamWriter.Close();

        if (seasonId.HasValue) return;

        // Longest intervals content file

        var longestIntervalKeys = orderedIntervalKeys.OrderDescending().ToList();
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), configuration[Constants.CONTENT_ROOT_DIR]!,
            "../includes/longest_intervals.md");
        var streamWriter3b = new StreamWriter(filePath);

        streamWriter3b.WriteLine("+++");
        streamWriter3b.WriteLine("title = \"Les plus longs intervalles\"");
        streamWriter3b.WriteLine("+++");

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

        streamWriter3b.WriteLine($"- Plus d'un an, du plus long au plus court :");

        foreach (var date in moreThanOneYearDates)
        {
            streamWriter3b.WriteLine(
                $"  - Du {date.Key.ToString("dd.MM.yyyy")} au {date.Value.ToString("dd.MM.yyyy")}");
        }

        streamWriter3b.WriteLine($"- Plus de trois mois, du plus long au plus court :");

        foreach (var date in moreThanThreeMonthsDates)
        {
            streamWriter3b.WriteLine(
                $"  - Du {date.Key.ToString("dd.MM.yyyy")} au {date.Value.ToString("dd.MM.yyyy")}");
        }

        streamWriter3b.Close();

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
        using var streamWriter2 = new StreamWriter(Path.Combine(rootDir, subDir, fileName));
        var chartDataFileHelper2 = new ChartDataFileHelper(streamWriter2, ChartType.Bar);
        chartDataFileHelper2.WriteBeforeData();
        chartDataFileHelper2.WriteData(seriesDataLines, true);
        chartDataFileHelper2.WriteAfterData("poemSeriesBar",
            ["Séries"],
            customScalesOptions: seasonId is null ? string.Empty : "scales: { y: { ticks: { stepSize: 1 } } }");
        streamWriter2.Close();

        // longest series content file

        var longestSeriesKeys = sortedKeys.OrderDescending().Take(5);
        filePath = Path.Combine(Directory.GetCurrentDirectory(), configuration[Constants.CONTENT_ROOT_DIR]!,
            "../includes/longest_series.md");
        var streamWriter3 = new StreamWriter(filePath);

        streamWriter3.WriteLine("+++");
        streamWriter3.WriteLine("title = \"Les plus longues séries\"");
        streamWriter3.WriteLine("+++");

        foreach (var key in longestSeriesKeys)
        {
            var matchingSeries = seriesDict.Where(x => x.Value == key);
            streamWriter3.WriteLine($"- {key} jours :");
            foreach (var pair in matchingSeries)
            {
                streamWriter3.WriteLine(
                    $"  - Du {pair.Key.AddDays(-key).ToString("dd.MM.yyyy")} au {pair.Key.ToString("dd.MM.yyyy")}");
            }
        }

        streamWriter3.Close();
    }

    /// <summary>
    /// Generates a bubble chart data file visualizing the relationship between the length of poems
    /// and their verse lengths. The method processes poem data from the provided `Root` object,
    /// organizes the data by verse lengths and poem lengths, and outputs poem-length-by-verse-length.js file
    /// containing the chart-ready data. Data is further divided into four distinct quarters
    /// based on calculated thresholds. Additionally, poems with variable metrics are grouped
    /// and visualized separately.
    /// </summary>
    /// <param name="data">The primary source of French poems data.</param>
    public void GeneratePoemLengthByVerseLengthBubbleChartDataFile(Root data)
    {
        var poems = data.Seasons.SelectMany(x => x.Poems);
        var poemLengthByVerseLength = new Dictionary<KeyValuePair<int, int>, int>();
        var variableMetric = new Dictionary<int, int>();
        foreach (var poem in poems)
        {
            var poemLength = poem.VersesCount;
            if (poem.HasVariableMetric)
            {
                if (!variableMetric.TryAdd(poemLength, 1))
                {
                    variableMetric[poemLength]++;
                }

                continue;
            }

            var key = new KeyValuePair<int, int>(int.Parse(poem.VerseLength!), poemLength);
            if (!poemLengthByVerseLength.TryAdd(key, 1))
            {
                poemLengthByVerseLength[key]++;
            }
        }

        // Find max value
        var maxValue = poemLengthByVerseLength.Values.Max();

        var fileName = "poem-length-by-verse-length.js";
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            configuration[Constants.CHART_DATA_FILES_ROOT_DIR]!);
        using var streamWriter = new StreamWriter(Path.Combine(rootDir, "general", fileName));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartType.Bubble, 4);
        chartDataFileHelper.WriteBeforeData();

        var firstQuarterDataLines = new List<BubbleChartDataLine>();
        var secondQuarterDataLines = new List<BubbleChartDataLine>();
        var thirdQuarterDataLines = new List<BubbleChartDataLine>();
        var fourthQuarterDataLines = new List<BubbleChartDataLine>();

        foreach (var dataKey in poemLengthByVerseLength.Keys)
        {
            ChartDataFileHelper.AddDataLine(dataKey.Key, dataKey.Value, poemLengthByVerseLength[dataKey],
                [firstQuarterDataLines, secondQuarterDataLines, thirdQuarterDataLines, fourthQuarterDataLines],
                maxValue, 30);
        }

        foreach (var dataKey in variableMetric.Keys)
        {
            ChartDataFileHelper.AddDataLine(0, dataKey, variableMetric[dataKey],
                [firstQuarterDataLines, secondQuarterDataLines, thirdQuarterDataLines, fourthQuarterDataLines],
                maxValue, 30);
        }

        chartDataFileHelper.WriteData(firstQuarterDataLines, false);
        chartDataFileHelper.WriteData(secondQuarterDataLines, false);
        chartDataFileHelper.WriteData(thirdQuarterDataLines, false);
        chartDataFileHelper.WriteData(fourthQuarterDataLines, true);
        chartDataFileHelper.WriteAfterData("poemLengthByVerseLength",
        [
            "Premier quart (taille fois 4)",
            "Deuxième quart (taille fois 2)",
            "Troisième quart (taille fois 1.5)",
            "Quatrième quart"
        ], chartXAxisTitle: "Métrique (0 = variable)", chartYAxisTitle: "Nombre de vers", yAxisStep: 2);
        streamWriter.Close();
    }

    /// <summary>
    /// Generates a line chart data file for visualizing metrics distributed over seasons.
    /// The method processes the data provided in the `Root` object and writes poems-verseLength-line.js
    /// line chart data file containing the chart-ready data.
    /// </summary>
    /// <param name="data">The primary source of French poems data.</param>
    public void GenerateOverSeasonsMetricLineChartDataFile(Root data)
    {
        var dataDict = ChartDataFileHelper.FillMetricDataDict(data, out var xLabels);

        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            configuration[Constants.CHART_DATA_FILES_ROOT_DIR]!);

        var metrics = configuration.GetSection(Constants.METRIC_SETTINGS).Get<MetricSettings>().Metrics;

        var fileName = "poems-verseLength-line.js";

        using var streamWriter = new StreamWriter(Path.Combine(rootDir, "general", fileName));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartType.Line, 14);
        chartDataFileHelper.WriteBeforeData();

        var oneFootDataLines =
            new LineChartDataLine("1 syllabe", dataDict[1],
                metrics.First(x => x.Length == 1).Color);
        var twoFeetDataLines =
            new LineChartDataLine("2 syllabes", dataDict[2],
                metrics.First(x => x.Length == 2).Color);
        var threeFeetDataLines =
            new LineChartDataLine("3 syllabes", dataDict[3],
                metrics.First(x => x.Length == 3).Color);
        var fourFeetDataLines =
            new LineChartDataLine("4 syllabes", dataDict[4],
                metrics.First(x => x.Length == 4).Color);
        var fiveFeetDataLines =
            new LineChartDataLine("5 syllabes", dataDict[5],
                metrics.First(x => x.Length == 5).Color);
        var sixFeetDataLines =
            new LineChartDataLine("6 syllabes", dataDict[6],
                metrics.First(x => x.Length == 6).Color);
        var sevenFeetDataLines =
            new LineChartDataLine("7 syllabes", dataDict[7],
                metrics.First(x => x.Length == 7).Color);
        var eightFeetDataLines =
            new LineChartDataLine("8 syllabes", dataDict[8],
                metrics.First(x => x.Length == 8).Color);
        var nineFeetDataLines =
            new LineChartDataLine("9 syllabes", dataDict[9],
                metrics.First(x => x.Length == 9).Color);
        var tenFeetDataLines =
            new LineChartDataLine("10 syllabes", dataDict[10],
                metrics.First(x => x.Length == 10).Color);
        var elevenFeetDataLines =
            new LineChartDataLine("11 syllabes", dataDict[11],
                metrics.First(x => x.Length == 11).Color);
        var twelveFeetDataLines =
            new LineChartDataLine("12 syllabes", dataDict[12],
                metrics.First(x => x.Length == 12).Color);
        var fourteenFeetDataLines =
            new LineChartDataLine("14 syllabes", dataDict[14],
                metrics.First(x => x.Length == 14).Color);

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