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
    public void GeneratePoemsByDayRadarChartDataFile(Root data, Root dataEn,
        string? storageSubCategory, string? storageCategory,
        bool forLesMoisExtraTag = false, bool forNoelExtraTag = false)
    {
        var isGeneral = storageSubCategory is null && storageCategory is null && !forLesMoisExtraTag && !forNoelExtraTag;

        var poemStringDates = new List<string>();

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
                .Where(x => x.ExtraTags.Contains("les mois")).Select(x => x.TextDate)
                .ToList();
        }
        else if (forNoelExtraTag)
        {
            poemStringDates = data.Seasons.SelectMany(x => x.Poems)
                .Where(x => x.ExtraTags.Contains("noël")).Select(x => x.TextDate)
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
            borderColor = configuration.GetSection(Constants.STORAGE_SETTINGS).Get<StorageSettings>().Categories
                .SelectMany(x => x.Subcategories).FirstOrDefault(x => x.Name == storageSubCategory).Color;

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
            borderColor = configuration.GetSection(Constants.STORAGE_SETTINGS).Get<StorageSettings>().Categories
                .FirstOrDefault(x => x.Name == storageCategory).Color;
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

        return monthDict.OrderByDescending(x => x.Value).Take(4).Select(x => x.Key).Select(ChartDataFileHelper.GetMonthLabel).ToList();
    }
}