using Shouldly;
using Toolbox.Domain;
using Xunit;

namespace Tests;

public class DataQualityTest(WithRealDataFixture fixture, ITestOutputHelper testOutputHelper)
    : IClassFixture<WithRealDataFixture>
{
    private readonly Root _data = fixture.Engine.Data;

    [Fact]
    [Trait("UnitTest", "Quality")]
    public void SeasonShouldHaveCorrectPoemCount()
    {
        var notEmptySeasons = _data.Seasons.Where(x => x.Poems.Count > 0).ToList();
        foreach (var season in notEmptySeasons)
        {
            testOutputHelper.WriteLine($"[{season.Id} - {season.Name}]: {season.Poems.Count}");
        }

        notEmptySeasons.Take(notEmptySeasons.Count - 1).All(x => x.Poems.Count == 50).ShouldBeTrue();
        notEmptySeasons.Skip(notEmptySeasons.Count - 1).Single(x => x.Poems.Count <= 50).ShouldNotBeNull();
    }

    [Fact]
    [Trait("UnitTest", "Quality")]
    public void SeasonShouldHaveNotTooLongSummary()
    {
        var seasons = _data.Seasons.ToList();
        foreach (var season in seasons)
        {
            testOutputHelper.WriteLine("[{0}]: {1} words (info: {2} words)", season.Name,
                season.Summary.Split(' ').Length, season.Introduction.Split(' ').Length);
        }

        seasons.All(x => x.Summary.Split(' ').Length <= 70).ShouldBeTrue();
    }

    [Fact]
    [Trait("UnitTest", "Quality")]
    public void SeasonShouldHaveInfo()
    {
        _data.Seasons.Count(x => string.IsNullOrEmpty(x.Introduction)).ShouldBe(0);
    }

    [Fact]
    [Trait("UnitTest", "Quality")]
    public void PoemShouldHaveTitle()
    {
        _data.Seasons.SelectMany(x => x.Poems).All(x => !string.IsNullOrEmpty(x.Title)).ShouldBeTrue();
    }

    [Fact]
    [Trait("UnitTest", "Quality")]
    public void PoemShouldHaveDate()
    {
        _data.Seasons.SelectMany(x => x.Poems).All(x => !string.IsNullOrEmpty(x.TextDate)).ShouldBeTrue();
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
        _data.Seasons.SelectMany(x => x.Poems).All(x => x.Categories.Count > 0).ShouldBeTrue();
    }

    [Fact]
    [Trait("UnitTest", "Quality")]
    public void PoemShouldHaveParagraphs()
    {
        _data.Seasons.SelectMany(x => x.Poems).All(x => x.Paragraphs.Count > 0).ShouldBeTrue();
    }

    [Fact]
    [Trait("UnitTest", "Quality")]
    public void PoemIdShouldEndWithSeasonId()
    {
        _data.Seasons.SelectMany(x => x.Poems).All(x => x.SeasonId > 0).ShouldBeTrue();
    }

    [Fact]
    [Trait("UnitTest", "Quality")]
    public void ParagraphShouldHaveVerses()
    {
        _data.Seasons.SelectMany(x => x.Poems).SelectMany(x => x.Paragraphs).All(x => x.Verses.Count > 0).ShouldBeTrue();
    }

    [Fact]
    [Trait("UnitTest", "Quality")]
    public void CategoryShouldHaveSubCategory()
    {
        var poems = _data.Seasons.SelectMany(x => x.Poems).Where(x => x.Categories.Any(y => y.SubCategories.Count == 0))
            .ToList();
        poems.ForEach(x => testOutputHelper.WriteLine("[{0}] {1}", x.Id,
            string.Join(',', x.Categories.Where(x => x.SubCategories.Count == 0).Select(x => x.Name))));

        poems.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("UnitTest", "Quality")]
    public void SpecialAcrosticheShouldBeConsistent()
    {
        _data.Seasons.SelectMany(x => x.Poems).Where(x => x.DoubleAcrostiche is not null).All(x =>
                !string.IsNullOrEmpty(x.DoubleAcrostiche!.First) &&
                !string.IsNullOrEmpty(x.DoubleAcrostiche.Second)).ShouldBeTrue();
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
        season.Years.ShouldBe(expectedValue);
    }
}