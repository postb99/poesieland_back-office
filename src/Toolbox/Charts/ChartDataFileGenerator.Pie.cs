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
            _configuration[Constants.CHART_DATA_FILES_ROOT_DIR]!);
        var subDirPath = Path.Combine(rootDir, "general");
        Directory.CreateDirectory(subDirPath);
        using var streamWriter = OpenChartWriter("general", "poems-length-pie.js",
            ChartType.Pie, out var chartDataFileHelper, 1);

        var coloredDataLines = new List<ColoredDataLine>();

        foreach (var nbVerses in nbVersesRange)
        {
            var lookup = nbVerses switch
            {
                3 => 0,
                26 => 1,
                32 => 1,
                _ => nbVerses / 2
            };

            var color = MetricSettings.Metrics.First(m => m.Length == lookup).Color;
            coloredDataLines.Add(new(nbVerses.ToString(),
                nbVersesData[nbVerses], color));
        }

        chartDataFileHelper.WriteData(coloredDataLines, true);

        chartDataFileHelper.WriteAfterData("poemLengthPie", ["Poèmes"]);

        streamWriter.Close();
    }

    /// <summary>
    /// Generates a pie chart data file for categories within a season, for a given metric, or across all seasons.
    /// The method processes poem data from the provided `Root` object, optionally filtered
    /// by a specific season identifier, and writes the resulting chart data to a "categories-pie.js" file
    /// in the appropriate directory structure.
    /// </summary>
    /// <param name="data">The root object containing all seasons and poems data to be processed for chart generation.</param>
    /// <param name="seasonId"> An optional season identifier to filter poems by season. If set to null, the chart data will include poems across all seasons. </param>
    /// <param name="metric">An optional metric value to filter poems by.</param>
    public void GenerateSubsetCategoriesPieChartDataFile(Root data, int? seasonId, int? metric)
    {
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CHART_DATA_FILES_ROOT_DIR]!);

        List<Poem> poems = [];
        string chartId = string.Empty;
        string subDir = string.Empty;
        string chartTitle = string.Empty;

        if (seasonId.HasValue)
        {
            var season = data.Seasons.First(x => x.Id == seasonId);
            poems = season.Poems;
            chartId = $"season{seasonId}Pie";
            subDir = $"season-{seasonId}";
            chartTitle = season.TitleForChartsWithPeriod;
        }
        else if (metric.HasValue)
        {
            poems = data.Seasons.SelectMany(x => x.Poems).Where(x => x.HasMetric(metric.Value)).ToList();
            chartId = $"metric{metric}Pie";
            subDir = $"metric-{metric}";
        }
        else
        {
            poems = data.Seasons.SelectMany(x => x.Poems).ToList();
            chartId = "categoriesPie";
            subDir = "general";
        }

        var storageSettings = StorageSettings;
        var subDirPath = Path.Combine(rootDir, subDir);
        Directory.CreateDirectory(subDirPath);
        using var streamWriter = OpenChartWriter(subDir, "categories-pie.js", ChartType.Pie, out var chartDataFileHelper);
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
            storageSettings.Categories.SelectMany(x => x.Subcategories).ToList();
        var pieChartData = new List<ColoredDataLine>();

        foreach (var subcategory in orderedSubcategories)
        {
            if (byStorageSubcategoryCount.TryGetValue(subcategory.Name, out var value))
                pieChartData.Add(new(subcategory.Title, value,
                    storageSettings.Categories.SelectMany(x => x.Subcategories)
                        .First(x => x.Name == subcategory.Name).Color
                ));
        }

        chartDataFileHelper.WriteData(pieChartData);

        chartDataFileHelper.WriteAfterData(chartId, [chartTitle]);
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
        var storageSettings = StorageSettings;
        using var streamWriter = OpenChartWriter("taxonomy", $"categories-{year}-pie.js", ChartType.Pie, out var chartDataFileHelper);
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
            storageSettings.Categories.SelectMany(x => x.Subcategories).ToList();
        var pieChartData = new List<ColoredDataLine>();

        foreach (var subcategory in orderedSubcategories)
        {
            if (byStorageSubcategoryCount.TryGetValue(subcategory.Name, out var value))
                pieChartData.Add(new(subcategory.Title, value,
                    storageSettings.Categories.SelectMany(x => x.Subcategories)
                        .First(x => x.Name == subcategory.Name).Color
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
    /// Generates a pie chart data file representing the intensity of poem creation by counting the number of poems created on each day.
    /// The method aggregates poem data from two `Root` objects, processes the intensity of poem creation,
    /// and generates a pie chart data file "poem-intensity-pie.js"
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
                    (baseAlpha + 0.1 * (key - 1)).ToString("F1", CultureInfo.InvariantCulture))));
        }

        var fileName = "poem-intensity-pie.js";
        using var streamWriter = OpenChartWriter("general", fileName, ChartType.Pie, out var chartDataFileHelper);
        chartDataFileHelper.WriteData(dataLines, true);
        chartDataFileHelper.WriteAfterData("poemIntensityPie", ["Les jours de création sont-ils intenses ?"]);
        streamWriter.Close();
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

        WriteByDayOfWeekChartFile(dataDict, "poem-dayofweek-pie.js", "poemDayOfWeekPie");
    }

    /// <summary>
    /// Writes a pie chart data file from day-of-week occurrence counts.
    /// </summary>
    /// <param name="dataDict">The occurrence count for each numeric day-of-week key.</param>
    /// <param name="fileName">The name of the chart data file to create.</param>
    /// <param name="chartId">The identifier assigned to the generated chart.</param>
    private void WriteByDayOfWeekChartFile(Dictionary<int, int> dataDict, string fileName, string chartId)
    {
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
                    (baseAlpha + 0.1 * (key == 0 ? 7 : key)).ToString("F1", CultureInfo.InvariantCulture))));
        }
        using var streamWriter = OpenChartWriter("general", fileName, ChartType.Pie, out var chartDataFileHelper);
        chartDataFileHelper.WriteData(dataLines, true);
        chartDataFileHelper.WriteAfterData(chartId, ["Par jour de la semaine"]);
        streamWriter.Close();
    }

    /// <summary>
    /// Processes and generates a pie chart data file representing the distribution of poems
    /// categorized by the day of the week on which they were written, for days when more than two poems were written.
    /// This method consolidates
    /// data from two `Root` objects (primary and secondary sources) and calculates poem counts
    /// for each day of the week, identified as Monday through Sunday.
    /// The generated "intenseDays-dayofweek-pie.js" file will include visual data for each day of the week with corresponding
    /// values and colors.
    /// </summary>
    /// <param name="data">The primary source of French poems data.</param>
    /// <param name="dataEn">The secondary source of English poems data.</param>
    public void GenerateIntenseByDayOfWeekPieChartDataFile(Root data, Root dataEn)
    {
        // Get poems grouped by TextDate when there are more than 2 poems for a given date
        var poems = data.Seasons.SelectMany(x => x.Poems)
            .Where(x => x.TextDate != "01.01.1994")
            .ToList();
        poems.AddRange(dataEn.Seasons.SelectMany(x => x.Poems));
        var groups = poems.GroupBy(x => x.TextDate)
            .Where(x => x.Count() > 2)
            .ToDictionary(x => x.Key, x => x.Count());

        var dataDict = new Dictionary<int, int>();
        foreach (var group in groups)
        {
            var dayOfWeek = group.Key.ToDateTime().DayOfWeek;
            if (!dataDict.TryAdd((int)dayOfWeek, group.Value))
            {
                dataDict[(int)dayOfWeek] += group.Value;
            }
        }

        WriteByDayOfWeekChartFile(dataDict, "intenseDays-dayofweek-pie.js", "intenseDaysDayOfWeekPie");
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
                    (baseAlpha + 0.1 * (key == 0 ? 7 : key)).ToString("F1", CultureInfo.InvariantCulture))));
        }

        var fileName = "poem-en-dayofweek-pie.js";
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CONTENT_ROOT_DIR_EN]!);
        using var streamWriter = OpenChartWriter(rootDir, "../charts/general", fileName, ChartType.Pie, out var chartDataFileHelper);
        chartDataFileHelper.WriteData(dataLines, true);
        chartDataFileHelper.WriteAfterData("poemEnDayOfWeekPie", ["By day of week"]);
        streamWriter.Close();
    }
}
