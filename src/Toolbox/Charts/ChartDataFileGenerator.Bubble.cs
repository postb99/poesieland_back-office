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
        using var streamWriter = OpenChartWriter("general", fileName, ChartType.Bubble, out var chartDataFileHelper, 4);

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
    /// Generates a bubble chart data file for associated categories.
    /// This method processes the data provided in the `Root` object and generates a chart data
    /// file that reflects category associations. Additionally, it creates markdown files
    /// listing top category associations for specific tags or properties.
    /// The following files are generated:
    /// - associated-categories.js: The main bubble chart data file with category associations.
    /// - refrain_categories.md: Lists topmost categories associations for poems with the "refrain" extra tag.
    /// - la_mort_categories.md: Lists topmost categories associations for poems related to "la mort".
    /// - sonnet_categories.md: Lists topmost categories associations for sonnets.
    /// </summary>
    /// <param name="data">The primary source of French poems data.</param>
    public void GenerateCategoriesBubbleChartDataFile(Root data)
    {
        var poems = data.Seasons.SelectMany(x => x.Poems);
        var categoriesDataDictionary = new Dictionary<KeyValuePair<string, string>, int>();
        var xAxisLabels = new SortedSet<string>();
        var yAxisLabels = new SortedSet<string>();

        foreach (var poem in poems)
        {
            ChartDataFileHelper.FillCategoriesBubbleChartDataDict(categoriesDataDictionary, xAxisLabels, yAxisLabels,
                poem);
        }

        // Find max value
        var maxValue = categoriesDataDictionary.Values.Max();

        var fileName = "associated-categories.js";
        using var streamWriter = OpenChartWriter("taxonomy", fileName, ChartType.Bubble, out var chartDataFileHelper, 4);

        var firstQuarterDataLines = new List<BubbleChartDataLine>();
        var secondQuarterDataLines = new List<BubbleChartDataLine>();
        var thirdQuarterDataLines = new List<BubbleChartDataLine>();
        var fourthQuarterDataLines = new List<BubbleChartDataLine>();

        // Get the values for x-axis
        var xAxisKeys = categoriesDataDictionary.Keys.Select(x => x.Key).Distinct().ToList();
        xAxisKeys.Sort();

        var yAxisKeys = categoriesDataDictionary.Keys.Select(x => x.Value).Distinct().ToList();
        yAxisKeys.Sort();

        foreach (var dataKey in categoriesDataDictionary.Keys)
        {
            var xAxisValue = xAxisKeys.IndexOf(dataKey.Key);
            var yAxisValue = yAxisKeys.IndexOf(dataKey.Value);
            ChartDataFileHelper.AddDataLine(xAxisValue, yAxisValue, categoriesDataDictionary[dataKey],
                [firstQuarterDataLines, secondQuarterDataLines, thirdQuarterDataLines, fourthQuarterDataLines],
                maxValue, 10);
        }

        chartDataFileHelper.WriteData(firstQuarterDataLines, false);
        chartDataFileHelper.WriteData(secondQuarterDataLines, false);
        chartDataFileHelper.WriteData(thirdQuarterDataLines, false);
        chartDataFileHelper.WriteData(fourthQuarterDataLines, true);
        
        // Sometimes replace subcategory name by label
        var subcategories = StorageSettings.Categories
            .SelectMany(x => x.Subcategories)
            .ToDictionary(x => x.Name);

        var xAxisTitles = xAxisKeys
            .Select(key => subcategories[key].Title)
            .ToList();
        
        var yAxisTitles = yAxisKeys
            .Select(key => subcategories[key].Title)
            .ToList();
        
        chartDataFileHelper.WriteAfterData("associatedCategories",
            [
                "Premier quart (taille fois 4)",
                "Deuxième quart (taille fois 2)",
                "Troisième quart (taille fois 1.5)",
                "Quatrième quart"
            ],
            customScalesOptions: chartDataFileHelper.FormatCategoriesBubbleChartLabelOptions(xAxisTitles,
                yAxisTitles));
        streamWriter.Close();

        // Automatic listing of topmost associations
        GenerateTopMostAssociatedCategoriesListing(categoriesDataDictionary);

        // Listing of topmost associations with refrain extra tag
        GenerateTopMostCategoriesListing(
            data.Seasons.SelectMany(x => x.Poems.Where(x => x.ExtraTags.Contains("refrain"))).ToList(),
            "refrain_categories.md");

        // Listing of topmost associations with la mort extra tag
        GenerateTopMostCategoriesListing(
            data.Seasons.SelectMany(x => x.Poems.Where(x => x.ExtraTags.Contains("la mort"))).ToList(),
            "la_mort_categories.md");

        // Listing of topmost associations with sonnet
        GenerateTopMostCategoriesListing(data.Seasons.SelectMany(x => x.Poems.Where(x => x.IsSonnet)).ToList(),
            "sonnet_categories.md");
    }

    /// <summary>
    /// Generates a bubble chart data file for category and metric associations.
    /// This method processes the data provided in the `Root` object and generates a chart data
    /// file that reflects category and metric associations. It distributes the data points
    /// into four quarters based on their metric values and assigns appropriate scaling factors
    /// to each quarter.
    /// Generated file: category-metric.js.
    /// </summary>
    /// <param name="data">The primary source of French poems data.</param>
    public void GenerateCategoryMetricBubbleChartDataFile(Root data)
    {
        var poems = data.Seasons.SelectMany(x => x.Poems);
        var categoryMetricDataDictionary = new Dictionary<KeyValuePair<string, int>, int>();
        var xAxisLabels = new SortedSet<string>();

        foreach (var poem in poems)
        {
            ChartDataFileHelper.FillCategoryMetricBubbleChartDataDict(categoryMetricDataDictionary, xAxisLabels, poem);
        }

        // Find max value
        var maxValue = categoryMetricDataDictionary.Values.Max();

        var fileName = "category-metric.js";
        using var streamWriter = OpenChartWriter("general", fileName, ChartType.Bubble, out var chartDataFileHelper, 4);

        var firstQuarterDataLines = new List<BubbleChartDataLine>();
        var secondQuarterDataLines = new List<BubbleChartDataLine>();
        var thirdQuarterDataLines = new List<BubbleChartDataLine>();
        var fourthQuarterDataLines = new List<BubbleChartDataLine>();

        // Get the values for x-axis
        var xAxisKeys = categoryMetricDataDictionary.Keys.Select(x => x.Key).Distinct().ToList();
        xAxisKeys.Sort();
        
        foreach (var dataKey in categoryMetricDataDictionary.Keys)
        {
            var xAxisValue = xAxisKeys.IndexOf(dataKey.Key);
            var yAxisValue = dataKey.Value;
            ChartDataFileHelper.AddDataLine(xAxisValue, yAxisValue, categoryMetricDataDictionary[dataKey],
                [firstQuarterDataLines, secondQuarterDataLines, thirdQuarterDataLines, fourthQuarterDataLines],
                maxValue, 10);
        }

        chartDataFileHelper.WriteData(firstQuarterDataLines, false);
        chartDataFileHelper.WriteData(secondQuarterDataLines, false);
        chartDataFileHelper.WriteData(thirdQuarterDataLines, false);
        chartDataFileHelper.WriteData(fourthQuarterDataLines, true);
        
        // Sometimes replace subcategory name by label
        var subcategories = StorageSettings.Categories
            .SelectMany(x => x.Subcategories)
            .ToDictionary(x => x.Name);

        var xAxisTitles = xAxisKeys
            .Select(key => subcategories[key].Title)
            .ToList();
        
        chartDataFileHelper.WriteAfterData("categoryMetric",
            [
                "Premier quart (taille fois 4)",
                "Deuxième quart (taille fois 2)",
                "Troisième quart (taille fois 1.5)",
                "Quatrième quart"
            ],
            customScalesOptions: chartDataFileHelper.FormatCategoriesBubbleChartLabelOptions(xAxisTitles,
                xAxisTitle: "Catégorie", yAxisTitle: "Métrique (0 = variable)"));
        streamWriter.Close();
    }
}
