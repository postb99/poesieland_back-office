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
    public void AllPoemsShouldHaveTitle()
    {
        _data.Seasons.SelectMany(x => x.Poems).All(x => !string.IsNullOrEmpty(x.Title)).Should().BeTrue();
    }

    [Fact]
    public void CountPoemsWithShortDate()
    {
        var result = _data.Seasons.SelectMany(x => x.Poems).Where(x => x.TextDate.Length < 10);
        _testOutputHelper.WriteLine($"With short date: {result.Count()}");

    }
    
    [Fact]
    public void AllPoemsShouldHaveDate()
    {
        _data.Seasons.SelectMany(x => x.Poems).All(x => !string.IsNullOrEmpty(x.TextDate)).Should().BeTrue();
    }

    [Fact]
    public void AllPoemsShouldHaveCategory()
    {
        _data.Seasons.SelectMany(x => x.Poems).All(x => x.Categories.Count > 0).Should().BeTrue();
    }

    [Fact]
    public void CountPoemsWithoutSubCategory()
    {
        var result = _data.Seasons.SelectMany(x => x.Poems).SelectMany(x => x.Categories).Where(x => x.SubCategory.Count == 0);
        _testOutputHelper.WriteLine($"Without subcategory: {result.Count()}");
    }
    
    [Fact]
    public void ListCategorySubCategory()
    {
        var result = _data.Seasons.SelectMany(x => x.Poems).SelectMany(x => x.Categories).Select(x =>
            new
            {
                Category = x.Name,
                SubCategory = x.SubCategory //.Join(' ')

            });
        // TODO finish this
    }
}