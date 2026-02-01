using Shouldly;
using Toolbox.Domain;
using Xunit;

namespace Tests.Domain;

public class SeasonTest
{
    [Theory]
    [Trait("UnitTest", "Computation")]
    [InlineData("01.01.1994", "01.09.1996", "1994-96")]
    [InlineData("01.09.1996", "01.12.1996", "1996")]
    [InlineData("01.09.1997", "01.03.1998", "1997-98")]
    [InlineData("01.11.1999", "01.03.2001", "1999-2001")]
    [InlineData("01.03.2002", "01.11.2004", "2002-04")]
    public void ShouldComputeYears(string firstDate, string secondDate, string expectedYears)
    {
        var season = new Season
        {
            Poems =
            [
                new() { TextDate = firstDate },
                new() { TextDate = secondDate }
            ]
        };

        season.Years.ShouldBe(expectedYears);
    }

    [Theory]
    [Trait("UnitTest", "Computation")]
    [InlineData("01.01.1994", "01.09.1996", "janvier 1994 à septembre 1996")]
    [InlineData("01.09.1996", "01.12.1996", "septembre à décembre 1996")]
    [InlineData("01.09.1997", "01.03.1998", "septembre 1997 à mars 1998")]
    [InlineData("01.11.1999", "01.03.2001", "novembre 1999 à mars 2001")]
    [InlineData("01.03.2002", "01.11.2004", "mars 2002 à novembre 2004")]
    [InlineData("01.06.2025", "15.06.2025", "juin 2025")]
    public void ShouldComputePeriod(string firstDate, string secondDate, string expectedPeriod)
    {
        Thread.CurrentThread.CurrentCulture = new("fr");
        var season = new Season
        {
            Poems =
            [
                new() { TextDate = firstDate },
                new() { TextDate = secondDate }
            ]
        };

        season.Period.ShouldBe(expectedPeriod);
    }
}