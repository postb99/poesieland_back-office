using System.Text;
using Toolbox.Xml;
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
        _testOutputHelper.WriteLine("{0} poems with verse length specified",
            _data.Seasons.SelectMany(x => x.Poems).Count(x => x.VerseLength != null));
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
    public void SeasonDatesAndLastPoem()
    {
        var seasons = _data.Seasons.ToList();
        foreach (var season in seasons)
        {
            var poems = season.Poems.OrderBy(x => x.Date).ToList();
            var lastPoem = poems.Last();
            _testOutputHelper.WriteLine("[{0} - {4}]: {1} - {2} ({3})", season.Name, poems[0].TextDate,
                lastPoem.TextDate,
                lastPoem.Id, poems.Count);
        }
    }
}