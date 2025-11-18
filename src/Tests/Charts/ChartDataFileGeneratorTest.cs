using Shouldly;
using Toolbox.Charts;
using Xunit;

namespace Tests.Charts;

public class ChartDataFileGeneratorTest(BasicFixture fixture): IClassFixture<BasicFixture>
{
    [Fact]
    [Trait("UnitTest", "Computation")]
    public void ShouldCorrectlyGetTopMostMonths()
    {
        Dictionary<string, int> dict = new()
        {
            { "0502", 1 },
            { "0503", 1 },
            { "0302", 3 },
            { "0102", 1 }
        };

        var generator = new ChartDataFileGenerator(fixture.Configuration);
        generator.GetTopMostMonths(dict).ShouldBeEquivalentTo(new List<string> { "mars", "mai", "janvier" });
    }
}