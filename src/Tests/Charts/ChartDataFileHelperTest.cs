using Shouldly;
using Toolbox.Charts;
using Toolbox.Domain;
using Xunit;

namespace Tests.Charts;

public class ChartDataFileHelperTest(WithRealDataFixture fixture, ITestOutputHelper testOutputHelper): IClassFixture<WithRealDataFixture>
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
    [Trait("UnitTest", "Quality")]
    public void ShouldCorrectlyComputeVerseLengthDataDict()
    {
        var data = fixture.Data;
        var dataDict = ChartDataFileHelper.FillMetricDataDict(data, out var _);
        testOutputHelper.WriteLine(
            $"Last non-empty season poem count: {data.Seasons.Last(x => x.Poems.Count > 0).Poems.Count}");
        testOutputHelper.WriteLine(
            $"Computed values for last season: {string.Join('-', dataDict.Values.Select(x => x.Last()))}");
        var sum = dataDict.Values.Sum(x => x.Last());
        testOutputHelper.WriteLine($"Computed values sum: {sum}");
        sum.ShouldBeInRange(49m, 59m);
    }
}