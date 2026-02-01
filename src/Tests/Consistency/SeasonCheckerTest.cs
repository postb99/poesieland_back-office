using Shouldly;
using Tests.Customizations;
using Toolbox.Consistency;
using Toolbox.Domain;
using Xunit;

namespace Tests.Consistency;

public class SeasonCheckerTest : IClassFixture<BasicFixture>
{
    [Theory]
    [Trait("UnitTest", "ConsistencyCheck")]
    [InlineAutoDomainData(50, 50)]
    [InlineAutoDomainData(50, 49)]
    [InlineAutoDomainData(50, 0)]
    [InlineAutoDomainData(49, 0)]
    public void ShouldNotThrowWhenSeasonPoemCountsAreBelowLimit(int firstSeasonPoemCount, int secondSeasonPoemCount)
    {
        var data = new Root
        {
            Seasons =
            {
                new Season { Poems = Get(50).ToList() },
                new Season { Poems = Get(50).ToList() }
            }
        };
        data.Seasons[0].Poems = data.Seasons[0].Poems.Take(firstSeasonPoemCount).ToList();
        data.Seasons[1].Poems = data.Seasons[1].Poems.Take(secondSeasonPoemCount).ToList();

        SeasonChecker.VerifySeasonHaveCorrectPoemCount(data);
    }

    [Theory]
    [Trait("UnitTest", "ConsistencyCheck")]
    [InlineAutoDomainData(50, 51, 1, "Last season. More than 50 poems for {desc}!")]
    [InlineAutoDomainData(51, 50, 0, "Not last season. Not 50 poems for {desc}!")]
    public void ShouldThrowWhenSeasonPoemCountsAreAboveLimit(int firstSeasonPoemCount, int secondSeasonPoemCount,
        int expectedInErrorSeasonIndex, string expectedErrorMessage)
    {
        var data = new Root
        {
            Seasons =
            {
                new Season { Poems = Get(51).ToList() },
                new Season { Poems = Get(51).ToList() }
            }
        };
        data.Seasons[0].Poems = data.Seasons[0].Poems.Take(firstSeasonPoemCount).ToList();
        data.Seasons[1].Poems = data.Seasons[1].Poems.Take(secondSeasonPoemCount).ToList();

        var act = () => SeasonChecker.VerifySeasonHaveCorrectPoemCount(data);
        var ex = act.ShouldThrow<Exception>();
        ex.Message.ShouldBe(expectedErrorMessage.Replace("{desc}",
            $"[{data.Seasons[expectedInErrorSeasonIndex].Id} - {data.Seasons[expectedInErrorSeasonIndex].Name}]: {data.Seasons[expectedInErrorSeasonIndex].Poems.Count}"));
    }

    private static IEnumerable<Poem> Get(int count)
    {
        for (int i = 0; i < count; i++)
        {
            yield return new Poem();
        }
    }
}