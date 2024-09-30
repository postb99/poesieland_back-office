using FluentAssertions;
using Toolbox.Domain;

namespace Tests;

public class SeasonTest
{
    [Theory]
    [Trait("UnitTest", "Computation")]
    [InlineData("Philosophie et nature. De 1994 à septembre 1996", "De 1994 à septembre 1996")]
    [InlineData("Depuis septembre 2024", "Depuis septembre 2024")]
    public void ShouldComputePeriod(string summary, string expectedPeriod)
    {
        var season = new Season
        {
            Summary = summary
        };

        season.Period.Should().Be(expectedPeriod);
    }
}