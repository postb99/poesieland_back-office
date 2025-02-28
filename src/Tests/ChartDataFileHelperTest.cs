using Shouldly;
using Toolbox;
using Xunit;

namespace Tests;

public class ChartDataFileHelperTest
{
    [Fact]
    [Trait("UnitTest", "Computation")]
    public void ShouldComputeBubbleChartScaleOptions()
    {
        // https://www.chartjs.org/docs/latest/axes/labelling.html
        var formattedString =
            new ChartDataFileHelper(default!, default).FormatCategoriesBubbleChartLabelOptions(["A", "B", "D"],
                ["A", "B", "C"]);

        formattedString.Count(x => x == '{').ShouldBe(formattedString.Count(x => x == '}'));
        formattedString.ShouldBe("scales: { x: { ticks: { stepSize: 1, autoSkip: false, callback: function(value, index, ticks) { return ['A','B','D'][index]; } } }, y: { ticks: { stepSize: 1, autoSkip: false, callback: function(value, index, ticks) { return ['A','B','C'][index]; } } } }");
    }
}