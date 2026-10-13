using Toolbox.Charts;
using Toolbox.Domain;
using Xunit;

namespace Tests.Charts;

public class ChartDataFileHelperTest
{
    /// <summary>
    /// - xAxisLabelsForCallback: set
    /// - yAxisLabelsForCallback: set
    /// - xAxisTitle: not set
    /// - yAxisTitle: not set
    /// </summary>
    [Fact]
    [Trait("UnitTest", "Computation")]
    public void ShouldComputeBubbleChartWithAxisCallbacks()
    {
        // https://www.chartjs.org/docs/latest/axes/labelling.html
        var formattedString =
            new ChartDataFileHelper(default!, default).FormatCategoriesBubbleChartLabelOptions(["A", "B", "D"], ["A", "B", "C"]);

        Assert.Equal(formattedString.Count(x => x == '}'), formattedString.Count(x => x == '{'));
        Assert.Equal("scales: { x: { ticks: { stepSize: 1, autoSkip: false, callback: function(value, index, ticks) { return ['A','B','D'][index]; } } }, y: { ticks: { stepSize: 1, autoSkip: false, callback: function(value, index, ticks) { return ['A','B','C'][index]; } } } }", formattedString);
    }
    
    /// <summary>
    /// - xAxisLabelsForCallback: set
    /// - yAxisLabelsForCallback: not set
    /// - xAxisTitle: not set
    /// - yAxisTitle: not set
    /// </summary>
    [Fact]
    [Trait("UnitTest", "Computation")]
    public void ShouldComputeBubbleChartWithXAxisCallback()
    {
        // https://www.chartjs.org/docs/latest/axes/labelling.html
        var formattedString =
            new ChartDataFileHelper(default!, default).FormatCategoriesBubbleChartLabelOptions(["A", "B", "D"]);

        Assert.Equal(formattedString.Count(x => x == '}'), formattedString.Count(x => x == '{'));
        Assert.Equal("scales: { x: { ticks: { stepSize: 1, autoSkip: false, callback: function(value, index, ticks) { return ['A','B','D'][index]; } } }, y: { ticks: { stepSize: 1, autoSkip: false } } }", formattedString);
    }
    
    /// <summary>
    /// - xAxisLabelsForCallback: set
    /// - yAxisLabelsForCallback: not set
    /// - xAxisTitle: set
    /// - yAxisTitle: set
    /// </summary>
    [Fact]
    [Trait("UnitTest", "Computation")]
    public void ShouldComputeBubbleChartWithXAxisCallbackAndAxisTitle()
    {
        // https://www.chartjs.org/docs/latest/axes/labelling.html
        var formattedString =
            new ChartDataFileHelper(default!, default).FormatCategoriesBubbleChartLabelOptions(["A", "B", "D"], xAxisTitle: "X", yAxisTitle: "Y");

        Assert.Equal(formattedString.Count(x => x == '}'), formattedString.Count(x => x == '{'));
        Assert.Equal("scales: { x: { ticks: { stepSize: 1, autoSkip: false, callback: function(value, index, ticks) { return ['A','B','D'][index]; } }, title: {display:true, text:'X'} }, y: { ticks: { stepSize: 1, autoSkip: false }, title: {display:true, text:'Y'} } }", formattedString);
    }
    
      [Fact]
    [Trait("UnitTest", "Computation")]
    public void ShouldCorrectlyFillCategoriesBubbleChartDataDict()
    {
        Dictionary<KeyValuePair<string, string>, int> dict = new();
        var xAxisLabels = new SortedSet<string>();
        var yAxisLabels = new SortedSet<string>();

        // Poem with single subcategory
        var poem = new Poem { Categories = [new() { SubCategories = ["A"] }] };
        ChartDataFileHelper.FillCategoriesBubbleChartDataDict(dict, xAxisLabels, yAxisLabels, poem);
        Assert.Empty(xAxisLabels);
        Assert.Empty(yAxisLabels);

        Assert.Empty(dict);

        // Poem with two categories
        poem = new() { Categories = [new() { SubCategories = ["A", "B"] }] };
        ChartDataFileHelper.FillCategoriesBubbleChartDataDict(dict, xAxisLabels, yAxisLabels, poem);

        var expectedKey = new KeyValuePair<string, string>("A", "B");
        Assert.True(dict.TryGetValue(expectedKey, out var counter));
        Assert.Equal(1, counter);

        var unExpectedKey = new KeyValuePair<string, string>("B", "A");
        Assert.False(dict.TryGetValue(unExpectedKey, out var _));

        Assert.Equal(new List<string> { "A" }, xAxisLabels.ToList());
        Assert.Equal(new List<string> { "B" }, yAxisLabels.ToList());

        // Poem with three categories
        poem = new() { Categories = [new() { SubCategories = ["A", "B", "C"] }] };
        ChartDataFileHelper.FillCategoriesBubbleChartDataDict(dict, xAxisLabels, yAxisLabels, poem);

        expectedKey = new("A", "B");
        Assert.True(dict.TryGetValue(expectedKey, out var counter2));
        Assert.Equal(2, counter2);

        unExpectedKey = new("B", "A");
        Assert.False(dict.TryGetValue(unExpectedKey, out var _));

        expectedKey = new("A", "C");
        Assert.True(dict.TryGetValue(expectedKey, out var counter3));
        Assert.Equal(1, counter3);

        expectedKey = new("B", "C");
        Assert.True(dict.TryGetValue(expectedKey, out var counter4));
        Assert.Equal(1, counter4);

        unExpectedKey = new("C", "B");
        Assert.False(dict.TryGetValue(unExpectedKey, out var _));

        unExpectedKey = new("C", "A");
        Assert.False(dict.TryGetValue(unExpectedKey, out var _));

        Assert.Equal(new List<string> { "A", "B" }, xAxisLabels.ToList());
        Assert.Equal(new List<string> { "B", "C" }, yAxisLabels.ToList());

        // Poem with two categories, one per category
        poem = new() { Categories = [new() { SubCategories = ["A"] }, new() { SubCategories = ["B"] }] };
        ChartDataFileHelper.FillCategoriesBubbleChartDataDict(dict, xAxisLabels, yAxisLabels, poem);

        expectedKey = new("A", "B");
        Assert.True(dict.TryGetValue(expectedKey, out var counter5));
        Assert.Equal(3, counter5);

        unExpectedKey = new("B", "A");
        Assert.False(dict.TryGetValue(unExpectedKey, out var _));

        Assert.Equal(new List<string> { "A", "B" }, xAxisLabels.ToList());
        Assert.Equal(new List<string> { "B", "C" }, yAxisLabels.ToList());
    }
    
        [Fact]
    [Trait("UnitTest", "Computation")]
    public void ShouldCorrectlyFillCategoryMetricBubbleChartDataDict()
    {
        Dictionary<KeyValuePair<string, int>, int> dict = new();
        var xAxisLabels = new SortedSet<string>();

        // Poem with single subcategory
        var poem = new Poem { VerseLength = "6", Categories = [new() { SubCategories = ["A"] }] };
        ChartDataFileHelper.FillCategoryMetricBubbleChartDataDict(dict, xAxisLabels, poem);
        var expectedKey = new KeyValuePair<string, int>("A", 6);
        Assert.True(dict.TryGetValue(expectedKey, out var counter));
        Assert.Equal(1, counter);
        Assert.Equal(new List<string> { "A" }, xAxisLabels.ToList());
        
        // Poem with two categories
        poem = new() { VerseLength = "8", Categories = [new() { SubCategories = ["A", "B"] }] };
        ChartDataFileHelper.FillCategoryMetricBubbleChartDataDict(dict, xAxisLabels, poem);
        expectedKey = new("A", 8);
        Assert.True(dict.TryGetValue(expectedKey, out var counter2));
        Assert.Equal(1, counter2);
        Assert.Equal(new List<string> { "A", "B" }, xAxisLabels.ToList());
        
        expectedKey = new("B", 8);
        Assert.True(dict.TryGetValue(expectedKey, out var counter3));
        Assert.Equal(1, counter3);
        Assert.Equal(new List<string> { "A", "B" }, xAxisLabels.ToList());
        
        // Poem with reused metric
        poem = new() { VerseLength = "8", Categories = [new() { SubCategories = ["A"] }] };
        ChartDataFileHelper.FillCategoryMetricBubbleChartDataDict(dict, xAxisLabels, poem);
        expectedKey = new("A", 8);
        Assert.True(dict.TryGetValue(expectedKey, out var counter4));
        Assert.Equal(2, counter4);
        Assert.Equal(new List<string> { "A", "B" }, xAxisLabels.ToList());
        
        // Variable metric
        poem = new() { VerseLength = "-1", Categories = [new() { SubCategories = ["A"] }] };
        ChartDataFileHelper.FillCategoryMetricBubbleChartDataDict(dict, xAxisLabels, poem);
        expectedKey = new("A", 0);
        Assert.True(dict.TryGetValue(expectedKey, out var counter5));
        Assert.Equal(1, counter5);
        Assert.Equal(new List<string> { "A", "B" }, xAxisLabels.ToList());
    }
}