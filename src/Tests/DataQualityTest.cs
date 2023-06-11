using System.Globalization;
using FluentAssertions;
using Toolbox.Xml;
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
    public void SeasonShouldHaveCorrectPoemCount()
    {
        var seasons = _data.Seasons.ToList();
        foreach (var season in seasons)
        {
            _testOutputHelper.WriteLine("[{0}]: {1}", season.Name, season.Poems.Count);
        }
        seasons.Take(seasons.Count - 1).All(x => x.Poems.Count == 50).Should().BeTrue();
        seasons.Skip(seasons.Count - 1).Single(x => x.Poems.Count <= 50).Should().NotBeNull();
    }

    [Fact]
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
    public void SeasonShouldHaveInfo()
    {
        _data.Seasons.Count(x => string.IsNullOrEmpty(x.Introduction)).Should().Be(0);
    }

    [Fact]
    public void PoemShouldHaveTitle()
    {
        _data.Seasons.SelectMany(x => x.Poems).All(x => !string.IsNullOrEmpty(x.Title)).Should().BeTrue();
    }

    [Fact]
    public void PoemShouldHaveDate()
    {
        _data.Seasons.SelectMany(x => x.Poems).All(x => !string.IsNullOrEmpty(x.TextDate)).Should().BeTrue();
    }

    [Fact]
    public void PoemShouldHaveCorrectFullDate()
    {
        _data.Seasons.SelectMany(x => x.Poems).Where(x => x.TextDate.Length == 10).All(x =>
                DateTime.TryParseExact(x.TextDate, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None,
                    out _))
            .Should().BeTrue();
    }

    [Fact]
    public void PoemShouldHaveCorrectShortDate()
    {
        _data.Seasons.SelectMany(x => x.Poems).Where(x => x.TextDate.Length == 4)
            .All(x => int.Parse(x.TextDate) > 1991 && int.Parse(x.TextDate) < 1996).Should().BeTrue();
    }

    [Fact]
    public void PoemShouldHaveCategory()
    {
        _data.Seasons.SelectMany(x => x.Poems).All(x => x.Categories.Count > 0).Should().BeTrue();
    }
    
    [Fact]
    public void PoemShouldHaveParagraphs()
    {
        _data.Seasons.SelectMany(x => x.Poems).All(x => x.Paragraphs.Count > 0).Should().BeTrue();
    }
    
    [Fact]
    public void ParagraphShouldHaveVerses()
    {
        _data.Seasons.SelectMany(x => x.Poems).SelectMany(x => x.Paragraphs).All(x => x.Verses.Count > 0).Should().BeTrue();
    }

    [Fact]
    public void SpecialAcrosticheShouldBeConsistent()
    {
        _data.Seasons.SelectMany(x => x.Poems).Where(x => x.SpecialAcrostiche != null).All(x =>
                !string.IsNullOrEmpty(x.SpecialAcrostiche!.First) &&
                !string.IsNullOrEmpty(x.SpecialAcrostiche.Second)).Should()
            .BeTrue();
    }
}