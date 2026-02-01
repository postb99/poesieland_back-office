using Shouldly;
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

        formattedString.Count(x => x == '{').ShouldBe(formattedString.Count(x => x == '}'));
        formattedString.ShouldBe("scales: { x: { ticks: { stepSize: 1, autoSkip: false, callback: function(value, index, ticks) { return ['A','B','D'][index]; } } }, y: { ticks: { stepSize: 1, autoSkip: false, callback: function(value, index, ticks) { return ['A','B','C'][index]; } } } }");
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

        formattedString.Count(x => x == '{').ShouldBe(formattedString.Count(x => x == '}'));
        formattedString.ShouldBe("scales: { x: { ticks: { stepSize: 1, autoSkip: false, callback: function(value, index, ticks) { return ['A','B','D'][index]; } } }, y: { ticks: { stepSize: 1, autoSkip: false } } }");
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

        formattedString.Count(x => x == '{').ShouldBe(formattedString.Count(x => x == '}'));
        formattedString.ShouldBe("scales: { x: { ticks: { stepSize: 1, autoSkip: false, callback: function(value, index, ticks) { return ['A','B','D'][index]; } }, title: {display:true, text:'X'} }, y: { ticks: { stepSize: 1, autoSkip: false }, title: {display:true, text:'Y'} } }");
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
        xAxisLabels.ShouldBeEmpty();
        yAxisLabels.ShouldBeEmpty();

        dict.ShouldBeEmpty();

        // Poem with two categories
        poem = new() { Categories = [new() { SubCategories = ["A", "B"] }] };
        ChartDataFileHelper.FillCategoriesBubbleChartDataDict(dict, xAxisLabels, yAxisLabels, poem);

        var expectedKey = new KeyValuePair<string, string>("A", "B");
        dict.TryGetValue(expectedKey, out var counter).ShouldBeTrue();
        counter.ShouldBe(1);

        var unExpectedKey = new KeyValuePair<string, string>("B", "A");
        dict.TryGetValue(unExpectedKey, out var _).ShouldBeFalse();

        xAxisLabels.ToList().ShouldBeEquivalentTo(new List<string> { "A" });
        yAxisLabels.ToList().ShouldBeEquivalentTo(new List<string> { "B" });

        // Poem with three categories
        poem = new() { Categories = [new() { SubCategories = ["A", "B", "C"] }] };
        ChartDataFileHelper.FillCategoriesBubbleChartDataDict(dict, xAxisLabels, yAxisLabels, poem);

        expectedKey = new("A", "B");
        dict.TryGetValue(expectedKey, out var counter2).ShouldBeTrue();
        counter2.ShouldBe(2);

        unExpectedKey = new("B", "A");
        dict.TryGetValue(unExpectedKey, out var _).ShouldBeFalse();

        expectedKey = new("A", "C");
        dict.TryGetValue(expectedKey, out var counter3).ShouldBeTrue();
        counter3.ShouldBe(1);

        expectedKey = new("B", "C");
        dict.TryGetValue(expectedKey, out var counter4).ShouldBeTrue();
        counter4.ShouldBe(1);

        unExpectedKey = new("C", "B");
        dict.TryGetValue(unExpectedKey, out var _).ShouldBeFalse();

        unExpectedKey = new("C", "A");
        dict.TryGetValue(unExpectedKey, out var _).ShouldBeFalse();

        xAxisLabels.ToList().ShouldBeEquivalentTo(new List<string> { "A", "B" });
        yAxisLabels.ToList().ShouldBeEquivalentTo(new List<string> { "B", "C" });

        // Poem with two categories, one per category
        poem = new() { Categories = [new() { SubCategories = ["A"] }, new() { SubCategories = ["B"] }] };
        ChartDataFileHelper.FillCategoriesBubbleChartDataDict(dict, xAxisLabels, yAxisLabels, poem);

        expectedKey = new("A", "B");
        dict.TryGetValue(expectedKey, out var counter5).ShouldBeTrue();
        counter5.ShouldBe(3);

        unExpectedKey = new("B", "A");
        dict.TryGetValue(unExpectedKey, out var _).ShouldBeFalse();

        xAxisLabels.ToList().ShouldBeEquivalentTo(new List<string> { "A", "B" });
        yAxisLabels.ToList().ShouldBeEquivalentTo(new List<string> { "B", "C" });
    }
}