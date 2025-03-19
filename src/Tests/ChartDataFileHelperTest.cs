using Shouldly;
using Toolbox;
using Xunit;

namespace Tests;

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
}