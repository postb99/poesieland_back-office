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

        var dataDict = InitMonthDayDictionary();

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
            dataLines.Add(new(GetRadarChartLabel(monthDay), value
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
                $"- {splitted[1].TrimStart('0')} {GetRadarChartLabel($"{splitted[0]}-01").ToLower()}");
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

        return monthDict.OrderByDescending(x => x.Value).Take(4).Select(x => x.Key).Select(GetMonthLabel).ToList();
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

    private string GetRadarChartLabel(string monthDay)
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

    private string GetMonthLabel(string month)
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
}