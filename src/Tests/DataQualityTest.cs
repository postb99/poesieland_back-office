using System.Globalization;
using FluentAssertions;
using Toolbox;
using Toolbox.Domain;
using Xunit.Abstractions;

namespace Tests;

public class DataQualityTest : IClassFixture<LoadDataFixture>
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly Root _data;

    public DataQualityTest(LoadDataFixture data, ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _data = data.Engine.Data;
    }

    [Fact]
    [Trait("UnitTest", "Quality")]
    public void SeasonShouldHaveCorrectPoemCount()
    {
        var notEmptySeasons = _data.Seasons.Where(x => x.Poems.Count > 0).ToList();
        foreach (var season in notEmptySeasons)
        {
            _testOutputHelper.WriteLine($"[{season.Id} - {season.Name}]: {season.Poems.Count}");
        }

        notEmptySeasons.Take(notEmptySeasons.Count - 1).All(x => x.Poems.Count == 50).Should().BeTrue();
        notEmptySeasons.Skip(notEmptySeasons.Count - 1).Single(x => x.Poems.Count <= 50).Should().NotBeNull();
    }

    [Fact]
    [Trait("UnitTest", "Quality")]
    public void SeasonShouldHaveNotTooLongSummary()
    {
        var seasons = _data.Seasons.ToList();
        foreach (var season in seasons)
        {
            _testOutputHelper.WriteLine("[{0}]: {1} words (info: {2} words)", season.Name,
                season.Summary.Split(' ').Length, season.Introduction.Split(' ').Length);
        }

        seasons.All(x => x.Summary.Split(' ').Length <= 70).Should().BeTrue();
    }

    [Fact]
    [Trait("UnitTest", "Quality")]
    public void SeasonShouldHaveInfo()
    {
        _data.Seasons.Count(x => string.IsNullOrEmpty(x.Introduction)).Should().Be(0);
    }

    [Fact]
    [Trait("UnitTest", "Quality")]
    public void PoemShouldHaveTitle()
    {
        _data.Seasons.SelectMany(x => x.Poems).All(x => !string.IsNullOrEmpty(x.Title)).Should().BeTrue();
    }

    [Fact]
    [Trait("UnitTest", "Quality")]
    public void PoemShouldHaveDate()
    {
        _data.Seasons.SelectMany(x => x.Poems).All(x => !string.IsNullOrEmpty(x.TextDate)).Should().BeTrue();
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
        _data.Seasons.SelectMany(x => x.Poems).All(x => x.Categories.Count > 0).Should().BeTrue();
    }

    [Fact]
    [Trait("UnitTest", "Quality")]
    public void PoemShouldHaveParagraphs()
    {
        _data.Seasons.SelectMany(x => x.Poems).All(x => x.Paragraphs.Count > 0).Should().BeTrue();
    }

    [Fact]
    [Trait("UnitTest", "Quality")]
    public void PoemShouldHaveSeasonId()
    {
        _data.Seasons.SelectMany(x => x.Poems).All(x => x.SeasonId > 0).Should().BeTrue();
    }

    [Fact]
    [Trait("UnitTest", "Quality")]
    public void ParagraphShouldHaveVerses()
    {
        _data.Seasons.SelectMany(x => x.Poems).SelectMany(x => x.Paragraphs).All(x => x.Verses.Count > 0).Should()
            .BeTrue();
    }

    [Fact]
    [Trait("UnitTest", "Quality")]
    public void CategoryShouldHaveSubCategory()
    {
        var poems = _data.Seasons.SelectMany(x => x.Poems).Where(x => x.Categories.Any(y => y.SubCategories.Count == 0))
            .ToList();
        poems.ForEach(x => _testOutputHelper.WriteLine("[{0}] {1}", x.Id,
            string.Join(',', x.Categories.Where(x => x.SubCategories.Count == 0).Select(x => x.Name))));

        poems.Count.Should().Be(0);
    }

    [Fact]
    [Trait("UnitTest", "Quality")]
    public void SpecialAcrosticheShouldBeConsistent()
    {
        _data.Seasons.SelectMany(x => x.Poems).Where(x => x.DoubleAcrostiche != null).All(x =>
                !string.IsNullOrEmpty(x.DoubleAcrostiche!.First) &&
                !string.IsNullOrEmpty(x.DoubleAcrostiche.Second)).Should()
            .BeTrue();
    }

    [Theory]
    [Trait("UnitTest", "Quality")]
    [InlineData(1, "1994 - 1996")]
    [InlineData(2, "1996")]
    [InlineData(5, "1997 - 1998")]
    [InlineData(19, "2024")]
    public void ShouldGetYears(int seasonId, string expectedValue)
    {
        var season = _data.Seasons.First(x => x.Id == seasonId);
        season.Years.Should().Be(expectedValue);
    }
}