using System.Text;
using Toolbox.Domain;
using Xunit.Abstractions;

namespace Tests;

public class DataMiningTests : IClassFixture<LoadDataFixture>
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly Root _data;

    public DataMiningTests(LoadDataFixture data, ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _data = data.Engine.Data;
    }

    [Fact]
    public void PoemsWithSpecifiedVerseLength()
    {
        var poems = _data.Seasons.SelectMany(x => x.Poems);
        var poemsWithVerseLength = poems.Count(x => x.VerseLength != null);
        int percentage = poemsWithVerseLength * 100 / poems.Count();
        _testOutputHelper.WriteLine("{0}/{1} poems ({2} %) with verse length specified",
            poemsWithVerseLength, poems.Count(), percentage);
        _testOutputHelper.WriteLine("First poem without verse length specified: {0}",
            poems.FirstOrDefault(x => x.VerseLength == null)?.Id);
    }

    [Fact]
    public void PoemsWithAdditionalData()
    {
        using var outputFileStream = File.Open("PoemsWithAdditionalData.txt", FileMode.Create);
        using var streamWriter = new StreamWriter(outputFileStream);
        var poems = _data.Seasons.SelectMany(x => x.Poems).Where(x =>
            !string.IsNullOrEmpty(x.Acrostiche)
            || x.DoubleAcrostiche != null
            || !string.IsNullOrEmpty(x.PoemType)
            || !string.IsNullOrEmpty(x.Info)).ToList();

        foreach (var poem in poems)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{0}", poem.Title);
            if (!string.IsNullOrEmpty(poem.Info))
                sb.AppendFormat(" [info] {0}", poem.Info);
            if (!string.IsNullOrEmpty(poem.PoemType))
                sb.AppendFormat(" [type] {0}", poem.PoemType);
            if (!string.IsNullOrEmpty(poem.Acrostiche))
            {
                sb.AppendFormat(" [acrostiche] {0}", poem.Acrostiche);
            }

            if (poem.DoubleAcrostiche != null)
            {
                sb.AppendFormat(" [crossing acrostiche] {0}/{1}", poem.DoubleAcrostiche.First,
                    poem.DoubleAcrostiche.Second);
            }

            streamWriter.WriteLine(sb.ToString());
        }
    }

    [Fact]
    public void CategoriesAndSubcategories()
    {
        using var outputFileStream = File.Open("CategoriesAndSubcategories.txt", FileMode.Create);
        using var streamWriter = new StreamWriter(outputFileStream);

        var categories = _data.Seasons.SelectMany(x => x.Poems).SelectMany(x => x.Categories).ToList();

        var dic = new Dictionary<string, HashSet<string>>();

        foreach (var categoryName in categories.Select(x => x.Name).Distinct())
        {
            dic.Add(categoryName, new HashSet<string>());
        }

        foreach (var category in categories)
        {
            category.SubCategories.ForEach(sc => dic[category.Name].Add(sc));
        }

        foreach (var categoryName in dic.Keys)
        {
            streamWriter.WriteLine(categoryName);
            foreach (var subCategory in dic[categoryName])
            {
                streamWriter.WriteLine("  - {0} ", subCategory);
            }

            streamWriter.WriteLine(Environment.NewLine);
        }
    }

    [Fact]
    public void PoemType()
    {
        using var outputFileStream = File.Open("PoemTypes.txt", FileMode.Create);
        using var streamWriter = new StreamWriter(outputFileStream);

        var types = _data.Seasons.SelectMany(x => x.Poems).Select(x => x.PoemType).ToList();

        foreach (var type in types.Distinct())
        {
            streamWriter.WriteLine(type);
            streamWriter.WriteLine(Environment.NewLine);
        }
    }

    [Fact]
    public void SeasonDatesAndLastPoem()
    {
        var seasons = _data.Seasons.ToList();
        foreach (var season in seasons)
        {
            var poems = season.Poems.OrderBy(x => x.Date).ToList();
            var lastPoem = poems.LastOrDefault();
            _testOutputHelper.WriteLine("[{0} - {4}]: {1} - {2} ({3})", season.Name, poems.FirstOrDefault()?.TextDate,
                lastPoem?.TextDate,
                lastPoem?.Id, poems.Count);
        }
    }

    [Fact]
    public void PoemWithMultipleSubcategory()
    {
        var poems = _data.Seasons.SelectMany(x => x.Poems).Where(x => x.Categories.Any(x => x.SubCategories.Count > 1));
        foreach (var poem in poems)
            _testOutputHelper.WriteLine(poem.Id);
    }

    [Fact]
    public void PoemWithMultipleSameCategory()
    {
        foreach (var poem in _data.Seasons.SelectMany(x => x.Poems))
        {
            var categoryNames = poem.Categories.Select(x => x.Name);
            if (categoryNames.Count() > categoryNames.Distinct().Count())
            {
                _testOutputHelper.WriteLine(poem.Id);
            }
        }
    }

    [Fact]
    public void PoemsWithStrangeVerseCount()
    {
        foreach (var poem in _data.Seasons.SelectMany(x => x.Poems))
        {
            if (poem.VersesCount % 2 != 0)
            {
                _testOutputHelper.WriteLine(poem.Id);
            }
        }
    }
}