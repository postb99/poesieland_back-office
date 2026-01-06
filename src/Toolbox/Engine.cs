using System.Globalization;
using Microsoft.Extensions.Configuration;
using Toolbox.Charts;
using Toolbox.Domain;
using Toolbox.Importers;
using Toolbox.Persistence;
using Toolbox.Settings;

namespace Toolbox;

[Obsolete("Will be replaced by direct use of modules")]
public class Engine
{
    private readonly IConfiguration _configuration;
    private readonly IDataManager _dataManager;
    public Root Data { get; private set; } = default!;
    public Root DataEn { get; private set; } = default!;

    private PoemImporter? _poemContentImporter;
    

    public Engine(IConfiguration configuration, IDataManager dataManager)
    {
        _configuration = configuration;
        _dataManager = dataManager;
        Data = new() { Seasons = [] };
    }

    [Obsolete]
    public void Load()
    {
        _dataManager.Load(out var data, out var dataEn);
        Data = data;
        DataEn = dataEn;
    }
    
   public void GenerateCategoriesBubbleChartDataFile()
    {
        var poems = Data.Seasons.SelectMany(x => x.Poems);
        var categoriesDataDictionary = new Dictionary<KeyValuePair<string, string>, int>();
        var xAxisLabels = new SortedSet<string>();
        var yAxisLabels = new SortedSet<string>();

        foreach (var poem in poems)
        {
            FillCategoriesBubbleChartDataDict(categoriesDataDictionary, xAxisLabels, yAxisLabels, poem);
        }

        // Find max value
        var maxValue = categoriesDataDictionary.Values.Max();

        var fileName = "associated-categories.js";
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CHART_DATA_FILES_ROOT_DIR]!);
        using var streamWriter = new StreamWriter(Path.Combine(rootDir, "taxonomy", fileName));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartType.Bubble, 4);
        chartDataFileHelper.WriteBeforeData();

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
            AddDataLine(xAxisValue, yAxisValue, categoriesDataDictionary[dataKey],
                [firstQuarterDataLines, secondQuarterDataLines, thirdQuarterDataLines, fourthQuarterDataLines],
                maxValue, 10);
        }

        chartDataFileHelper.WriteData(firstQuarterDataLines, false);
        chartDataFileHelper.WriteData(secondQuarterDataLines, false);
        chartDataFileHelper.WriteData(thirdQuarterDataLines, false);
        chartDataFileHelper.WriteData(fourthQuarterDataLines, true);
        chartDataFileHelper.WriteAfterData("associatedCategories",
            [
                "Premier quart (taille fois 4)",
                "Deuxième quart (taille fois 2)",
                "Troisième quart (taille fois 1.5)",
                "Quatrième quart"
            ],
            customScalesOptions: chartDataFileHelper.FormatCategoriesBubbleChartLabelOptions(xAxisLabels.ToList(),
                yAxisLabels.ToList()));
        streamWriter.Close();

        // Automatic listing of topmost associations
        GenerateTopMostAssociatedCategoriesListing(categoriesDataDictionary);

        // Listing of topmost associations with refrain extra tag
        GenerateTopMostCategoriesListing(
            Data.Seasons.SelectMany(x => x.Poems.Where(x => x.ExtraTags.Contains("refrain"))).ToList(),
            "refrain_categories.md");

        // Listing of topmost associations with sonnet
        GenerateTopMostCategoriesListing(Data.Seasons.SelectMany(x => x.Poems.Where(x => x.IsSonnet)).ToList(),
            "sonnet_categories.md");

        // Listing of topmost associations with metrics
        foreach (var metric in Enumerable.Range(1, 12))
        {
            poems = Data.Seasons.SelectMany(x => x.Poems.Where(x => x.HasMetric(metric))).ToList();
            GenerateTopMostCategoriesListing(poems, $"metric-{metric}_categories.md");
        }
    }

    private void GenerateTopMostAssociatedCategoriesListing(Dictionary<KeyValuePair<string, string>, int> dataDict)
    {
        var sortedDict = dataDict.OrderByDescending(x => x.Value).Take(10).ToList();

        var outFile = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CONTENT_ROOT_DIR]!, "../includes", "associated_categories.md");
        using var streamWriter = new StreamWriter(outFile);

        streamWriter.WriteLine("+++");
        streamWriter.WriteLine("title = \"Associations privilégiées\"");
        streamWriter.WriteLine("+++");
        foreach (var (key, _) in sortedDict)
        {
            streamWriter.WriteLine(
                $"- {key.Key.MarkdownLink("categories")} et {key.Value.MarkdownLink("categories")}");
        }

        streamWriter.Close();
    }

    private void GenerateTopMostCategoriesListing(IEnumerable<Poem> poems, string fileName)
    {
        var dict = new Dictionary<string, int>();
        foreach (var poem in poems)
        {
            foreach (var cat in poem.Categories.SelectMany(x => x.SubCategories))
            {
                if (dict.TryGetValue(cat, out var count))
                {
                    dict[cat] = ++count;
                }
                else
                {
                    dict[cat] = 1;
                }
            }
        }

        var topMost = dict.OrderByDescending(x => x.Value).Take(10).ToList();

        var outFile = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CONTENT_ROOT_DIR]!, "../includes", fileName);
        using var streamWriter = new StreamWriter(outFile);

        streamWriter.WriteLine("+++");
        streamWriter.WriteLine("title = \"Associations privilégiées\"");
        streamWriter.WriteLine("+++");
        foreach (var topmost in topMost)
        {
            streamWriter.WriteLine(
                $"- {topmost.Key.MarkdownLink("categories")}");
        }

        streamWriter.Close();
    }

    public void GenerateCategoryMetricBubbleChartDataFile()
    {
        var poems = Data.Seasons.SelectMany(x => x.Poems);
        var categoryMetricDataDictionary = new Dictionary<KeyValuePair<string, int>, int>();
        var xAxisLabels = new SortedSet<string>();

        foreach (var poem in poems)
        {
            FillCategoryMetricBubbleChartDataDict(categoryMetricDataDictionary, xAxisLabels, poem);
        }

        // Find max value
        var maxValue = categoryMetricDataDictionary.Values.Max();

        var fileName = "category-metric.js";
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CHART_DATA_FILES_ROOT_DIR]!);
        using var streamWriter = new StreamWriter(Path.Combine(rootDir, "general", fileName));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartType.Bubble, 4);
        chartDataFileHelper.WriteBeforeData();

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
            AddDataLine(xAxisValue, yAxisValue, categoryMetricDataDictionary[dataKey],
                [firstQuarterDataLines, secondQuarterDataLines, thirdQuarterDataLines, fourthQuarterDataLines],
                maxValue, 10);
        }

        chartDataFileHelper.WriteData(firstQuarterDataLines, false);
        chartDataFileHelper.WriteData(secondQuarterDataLines, false);
        chartDataFileHelper.WriteData(thirdQuarterDataLines, false);
        chartDataFileHelper.WriteData(fourthQuarterDataLines, true);
        chartDataFileHelper.WriteAfterData("categoryMetric",
            [
                "Premier quart (taille fois 4)",
                "Deuxième quart (taille fois 2)",
                "Troisième quart (taille fois 1.5)",
                "Quatrième quart"
            ],
            customScalesOptions: chartDataFileHelper.FormatCategoriesBubbleChartLabelOptions(xAxisLabels.ToList(),
                xAxisTitle: "Catégorie", yAxisTitle: "Métrique (0 = variable)"));
        streamWriter.Close();
    }

    public void FillCategoriesBubbleChartDataDict(Dictionary<KeyValuePair<string, string>, int> dictionary,
        SortedSet<string> xLabels, SortedSet<string> yLabels, Poem poem)
    {
        var subCategories = poem.Categories.SelectMany(x => x.SubCategories).ToList();

        if (subCategories.Count == 1)
        {
            return;
        }

        for (var i = 0; i < subCategories.Count; i++)
        {
            for (var j = subCategories.Count - 1; j > -1; j--)
            {
                if (string.Compare(subCategories[i], subCategories[j]) >= 0) continue;
                var key = new KeyValuePair<string, string>(subCategories[i], subCategories[j]);
                if (dictionary.TryGetValue(key, out _))
                {
                    dictionary[key]++;
                }
                else
                {
                    dictionary.Add(key, 1);
                    xLabels.Add(key.Key);
                    yLabels.Add(key.Value);
                }
            }
        }
    }

    public void FillCategoryMetricBubbleChartDataDict(Dictionary<KeyValuePair<string, int>, int> dictionary,
        SortedSet<string> xLabels, Poem poem)
    {
        var subCategories = poem.Categories.SelectMany(x => x.SubCategories).ToList();
        var metric = poem.HasVariableMetric ? 0 : int.Parse(poem.VerseLength!);

        foreach (var key in subCategories.Select(subCategory => new KeyValuePair<string, int>(subCategory, metric)))
        {
            if (dictionary.TryGetValue(key, out _))
            {
                dictionary[key]++;
            }
            else
            {
                dictionary.Add(key, 1);
                xLabels.Add(key.Key);
            }
        }
    }

    public void OutputSeasonsDuration()
    {
        foreach (var season in Data.Seasons.Where(x => x.Poems.Count > 0))
        {
            var dates = season.Poems.Select(x => x.Date).OrderBy(x => x.Date).ToList();
            var duration = dates[dates.Count() - 1] - dates[0];
            decimal nbDays = int.Parse(duration.ToString("%d"));
            var value = nbDays;
            var unit = "days";
            if (value > 30)
            {
                value = value / 30;
                unit = "months";

                if (value > 12)
                {
                    value = value / 12;
                    unit = "years";
                }
            }

            Console.WriteLine($"{season.NumberedName} ({season.Period}): {value} {unit}");
        }
    }

    private void AddDataLine(int x, int y, int value,
        List<BubbleChartDataLine>[] quarterBubbleChartDatalines, int maxValue,
        int bubbleMaxRadiusPixels)
    {
        // Bubble radius and color
        decimal bubbleSize = (decimal)bubbleMaxRadiusPixels * value / maxValue;
        var bubbleColor = string.Empty;
        if (bubbleSize < (bubbleMaxRadiusPixels / 4))
        {
            // First quarter
            bubbleSize *= 4;
            bubbleColor = "rgba(121, 248, 248, 1)";
            quarterBubbleChartDatalines[0].Add(new(x, y,
                bubbleSize.ToString(new NumberFormatInfo { NumberDecimalSeparator = "." }), bubbleColor));
        }
        else if (bubbleSize < (bubbleMaxRadiusPixels / 2))
        {
            // Second quarter
            bubbleSize *= 2;
            bubbleColor = "rgba(119, 181, 254, 1)";
            quarterBubbleChartDatalines[1].Add(new(x, y,
                bubbleSize.ToString(new NumberFormatInfo { NumberDecimalSeparator = "." }), bubbleColor));
        }
        else if (bubbleSize < (bubbleMaxRadiusPixels * 3 / 4))
        {
            // Third quarter
            bubbleSize *= 1.5m;
            bubbleColor = "rgba(0, 127, 255, 1)";
            quarterBubbleChartDatalines[2].Add(new(x, y,
                bubbleSize.ToString(new NumberFormatInfo { NumberDecimalSeparator = "." }), bubbleColor));
        }
        else
        {
            // Fourth quarter
            bubbleColor = "rgba(50, 122, 183, 1)";
            quarterBubbleChartDatalines[3].Add(new(x, y,
                bubbleSize.ToString(new NumberFormatInfo { NumberDecimalSeparator = "." }), bubbleColor));
        }
    }
  }