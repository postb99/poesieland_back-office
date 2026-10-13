using Toolbox.Domain;
using Xunit;

namespace Tests;

public class DataQualityTest(WithRealDataFixture fixture, ITestOutputHelper testOutputHelper)
    : IClassFixture<WithRealDataFixture>
{
    private readonly Root _data = fixture.Data;

    [Fact]
    [Trait("UnitTest", "Quality")]
    public void SeasonShouldHaveCorrectPoemCount()
    {
        var notEmptySeasons = _data.Seasons.Where(x => x.Poems.Count > 0).ToList();
        foreach (var season in notEmptySeasons)
        {
            testOutputHelper.WriteLine($"[{season.Id} - {season.Name}]: {season.Poems.Count}");
        }

        Assert.True(notEmptySeasons.Take(notEmptySeasons.Count - 1).All(x => x.Poems.Count == 50));
        Assert.NotNull(notEmptySeasons.Skip(notEmptySeasons.Count - 1).Single(x => x.Poems.Count <= 50));
    }

    [Fact]
    [Trait("UnitTest", "Quality")]
    public void SeasonShouldHaveDescription()
    {
        Assert.Equal(0, _data.Seasons.Count(x => string.IsNullOrEmpty(x.Description)));
    }

    [Fact]
    [Trait("UnitTest", "Quality")]
    public void PoemShouldHaveTitle()
    {
        Assert.True(_data.Seasons.SelectMany(x => x.Poems).All(x => !string.IsNullOrEmpty(x.Title)));
    }

    [Fact]
    [Trait("UnitTest", "Quality")]
    public void PoemShouldHaveDate()
    {
        Assert.True(_data.Seasons.SelectMany(x => x.Poems).All(x => !string.IsNullOrEmpty(x.TextDate)));
    }

    [Fact]
    [Trait("UnitTest", "Quality")]
    public void PoemShouldHaveCorrectDateTime()
    {
        var _ = _data.Seasons.SelectMany(x => x.Poems).Select(x => x.Date);
    }

    [Fact]
    [Trait("UnitTest", "Quality")]
    public void PoemShouldHaveCategory()
    {
        Assert.True(_data.Seasons.SelectMany(x => x.Poems).All(x => x.Categories.Count > 0));
    }

    [Fact]
    [Trait("UnitTest", "Quality")]
    public void PoemShouldHaveParagraphs()
    {
        Assert.True(_data.Seasons.SelectMany(x => x.Poems).All(x => x.Paragraphs.Count > 0));
    }

    [Fact]
    [Trait("UnitTest", "Quality")]
    public void PoemIdShouldEndWithSeasonId()
    {
        Assert.True(_data.Seasons.SelectMany(x => x.Poems).All(x => x.SeasonId > 0));
    }

    [Fact]
    [Trait("UnitTest", "Quality")]
    public void ParagraphShouldHaveVerses()
    {
        Assert.True(_data.Seasons.SelectMany(x => x.Poems).SelectMany(x => x.Paragraphs).All(x => x.Verses.Count > 0));
    }

    [Fact]
    [Trait("UnitTest", "Quality")]
    public void CategoryShouldHaveSubCategory()
    {
        var poems = _data.Seasons.SelectMany(x => x.Poems).Where(x => x.Categories.Any(y => y.SubCategories.Count == 0))
            .ToList();
        poems.ForEach(x => testOutputHelper.WriteLine("[{0}] {1}", x.Id,
            string.Join(',', x.Categories.Where(x => x.SubCategories.Count == 0).Select(x => x.Name))));

        Assert.Equal(0, poems.Count);
    }

    [Fact]
    [Trait("UnitTest", "Quality")]
    public void SpecialAcrosticheShouldBeConsistent()
    {
        _data.Seasons.SelectMany(x => x.Poems).Where(x => x.DoubleAcrostiche is not null).All(x =>
                !string.IsNullOrEmpty(x.DoubleAcrostiche!.First) &&
                !string.IsNullOrEmpty(x.DoubleAcrostiche.Second));
    }

    [Theory]
    [Trait("UnitTest", "Quality")]
    [InlineData(1, "1994-96")]
    [InlineData(2, "1996")]
    [InlineData(5, "1997-98")]
    [InlineData(19, "2024")]
    public void ShouldGetYears(int seasonId, string expectedValue)
    {
        var season = _data.Seasons.First(x => x.Id == seasonId);
        Assert.Equal(expectedValue, season.Years);
    }
}