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
    /// Generates a radar chart data file for poems categorized by the day of the month.
    /// The method processes poem data from the provided `Root` objects, optionally filtered
    /// by category or sub-category, and writes chart data files. If an optional extra tag
    /// flag is true, it will filter poems containing a special extra tag.
    /// Following files will be generated in appropriate locations:
    /// - poems-day-{storageSubCategory, i.e. "automne"}-radar.js,
    /// - poems-day-{storageCategory, i.e. "saisons"}-radar.js
    /// - poems-day-{extraTag, i.e. "les_mois"}-radar.js
    /// - days_without_creation.md
    /// </summary>
    /// <param name="data">The primary source of French poems data.</param>
    /// <param name="dataEn">The secondary source of English poems data.</param>
    /// <param name="storageSubCategory">The optional sub-category to filter poems.</param>
    /// <param name="storageCategory">The optional category to filter poems.</param>
    /// <param name="extraTag">The optional item with extra tag value to filter poems.</param>
    public void GeneratePoemsByDayRadarChartDataFile(Root data, Root dataEn,
        string? storageSubCategory = null, string? storageCategory = null, RadarItem? extraTag = null)
    {
        var isGeneral = storageSubCategory is null && storageCategory is null && extraTag is null;

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
        else if (extraTag is not null)
        {
            poemStringDates = data.Seasons.SelectMany(x => x.Poems)
                .Where(x => x.ExtraTags != null && x.ExtraTags.Contains(extraTag.Name)).Select(x => x.TextDate)
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

        string fileName;
        string chartId;
        string borderColor = string.Empty;

        if (storageSubCategory is not null)
        {
            // categories
            fileName = $"poems-day-{storageSubCategory.UnaccentedCleaned()}-radar.js";
            chartId = $"poemDay-{storageSubCategory.UnaccentedCleaned()}Radar";
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
            // tags
            fileName = $"poems-day-{storageCategory.UnaccentedCleaned()}-radar.js";
            chartId = $"poemDay-{storageCategory.UnaccentedCleaned()}Radar";
            borderColor = StorageSettings.Categories
                .FirstOrDefault(x => x.Name == storageCategory)!.Color;
        }
        else if (extraTag is not null)
        {
            fileName = $"poems-day-{extraTag.Name.UnaccentedCleaned().Replace('_', '-')}-radar.js";
            chartId = $"poemDay-{extraTag.Name.UnaccentedCleaned()}Radar";

            if (extraTag.Color is not null)
            {
                borderColor = extraTag.Color;
            }
        }
        else
        {
            // general
            fileName = "poems-day-radar.js";
            chartId = "poemDayRadar";
        }

        using var streamWriter = OpenChartWriter(isGeneral ? "general" : "taxonomy", fileName,
            ChartType.Radar, out var chartDataFileHelper);

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

        var backgroundColor = borderColor.Replace("1)", "0.5)");

        var title = $"Mois les plus représentés : {string.Join(", ", GetTopMostMonths(dataDict))}";

        chartDataFileHelper.WriteAfterData(chartId, [title], borderColor, backgroundColor);
        streamWriter.Close();

        if (!isGeneral) return;

        // Days without poems listing

        WriteMarkdownListFile("days_without_creation.md", "Les jours sans",
            dayWithoutPoems.Select(monthDay =>
            {
                var splitted = monthDay.Split('-');
                return
                    $"- {splitted[1].TrimStart('0')} {ChartDataFileHelper.GetRadarChartLabel($"{splitted[0]}-01").ToLower()}";
            }));
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
            _configuration[Constants.CONTENT_ROOT_DIR_EN]!, "../charts/general");

        var fileName = "poems-en-day-radar.js";
        var chartId = "poemEnDayRadar";

        using var streamWriter = OpenChartWriter(rootDir, fileName, ChartType.Radar, out var chartDataFileHelper);

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

        var fileName = $"poems-day-{year}-radar.js";
        var chartId = $"poemDay-{year}Radar";

        using var streamWriter = OpenChartWriter("taxonomy", fileName, ChartType.Radar, out var chartDataFileHelper);

        var dataLines = new List<DataLine>();

        foreach (var monthDay in dataDict.Keys)
        {
            var value = dataDict[monthDay];
            dataLines.Add(new(ChartDataFileHelper.GetRadarChartLabel(monthDay), value));
        }

        chartDataFileHelper.WriteData(dataLines, true);

        chartDataFileHelper.WriteAfterData(chartId, ["Poèmes selon le jour de l'année"], string.Empty,
            string.Empty);
        streamWriter.Close();
    }

    /// <summary>
    /// Gets the four months with the highest aggregate occurrence counts from month-day data.
    /// </summary>
    /// <param name="monthDayDict">A dictionary whose keys are month-day values in <c>MM-DD</c> format and whose values are occurrence counts.</param>
    /// <returns>The labels of the four most represented months, ordered by descending occurrence count.</returns>
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
}