using FluentAssertions;
using Toolbox;

namespace Tests;

public class ChartDataFileHelperTest
{
    [Fact]
    [Trait("UnitTest", "Computation")]
    public void ShouldComputeBubbleChartScaleOptions()
    {
        var formattedString =
            new ChartDataFileHelper(default, default).FormatCategoriesBubbleChartLabelOptions(["A", "B", "D"],
                ["A", "B", "C"]);
        formattedString.Should()
            .Be(
                "scales: {{ x: {{ type: 'category', labels: ['A','B','D'] }}, y: {{ type: 'category', labels: ['A','B','C'] }} }}");

        // scales: {
        //     x: {
        //         type: 'category',
        //         labels: ["Mon", "Tue", "wed", "Thu"]
        //     },
        //     y: {
        //         type: 'category',
        //         labels: ["Mon", "Tue", "wed", "Thu"]
        //     }
        // }
    }
}