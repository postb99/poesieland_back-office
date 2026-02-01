using Shouldly;
using Toolbox.Charts;
using Toolbox.Domain;
using Xunit;

namespace Tests.Charts;

public class ChartDataFileHelperTestWithRealData(WithRealDataFixture fixture, ITestOutputHelper testOutputHelper)
    : IClassFixture<WithRealDataFixture>
{
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