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
    public void PoemsWithSpecifiedLineLength()
    {
        _testOutputHelper.WriteLine("{0} poems with verse length specified",
            _data.Seasons.SelectMany(x => x.Poems).Count(x => x.LineLength > 0));
    }

    [Fact]
    public void PoemsWithAdditionalData()
    {
        using var outputFileStream = File.Open("PoemsWithAdditionalData.txt", FileMode.Create);
        using var streamWriter = new StreamWriter(outputFileStream);
        var poems = _data.Seasons.SelectMany(x => x.Poems).Where(x =>
            x.Acrostiche != null || !string.IsNullOrEmpty(x.PoemType) || !string.IsNullOrEmpty(x.Info)).ToList();

        foreach (var poem in poems)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{0}", poem.Title);
            if (!string.IsNullOrEmpty(poem.Info))
                sb.AppendFormat(" [info] {0}", poem.Info);
            if (!string.IsNullOrEmpty(poem.PoemType))
                sb.AppendFormat(" [type] {0}", poem.PoemType);
            if (poem.Acrostiche != null)
            {
                if (string.IsNullOrEmpty(poem.Acrostiche!.First))
                    sb.AppendFormat(" [acrostiche] {0}", poem.Acrostiche.Content);
                else
                {
                    sb.AppendFormat(" [special acrostiche] {0}", poem.Acrostiche.First, poem.Acrostiche.Second);
                }
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
        var dic = new Dictionary<string, List<string>>();
        foreach (var category in categories)
        {
            dic.TryAdd(category.Name, new List<string>());
            dic[category.Name].AddRange(category.SubCategory);
        }

        foreach (var categoryName in dic.Keys)
        {
            streamWriter.WriteLine(categoryName);
            foreach (var subCategory in dic[categoryName].Distinct())
            {
                streamWriter.WriteLine("  - {0} ", subCategory);
            }

            streamWriter.WriteLine(Environment.NewLine);
        }
    }
}