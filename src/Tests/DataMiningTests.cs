using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;
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

    [Theory]
    [Trait("DataMining", "Lookup")]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(14)]
    public void PoemsWithSpecifiedVerseLength(int verseLength)
    {
        var poems = _data.Seasons.SelectMany(x => x.Poems).Where(x => x.VerseLength == verseLength.ToString());

        _testOutputHelper.WriteLine("Verse length {0}: {1}",
            verseLength, string.Join(' ', poems.Select(x => x.Id).ToList()));
    }

    [Theory]
    [Trait("DataMining", "Lookup")]
    [InlineData(4)]
    [InlineData(28)]
    public void PoemsWithSpecifiedLength(int verseCount)
    {
        var poems = _data.Seasons.SelectMany(x => x.Poems).Where(x => x.VersesCount == verseCount);

        _testOutputHelper.WriteLine("Verse count {0}: {1}",
            verseCount, string.Join(' ', poems.Select(x => x.Id).ToList()));
    }

    [Fact]
    [Trait("DataMining", "Lookup")]
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
    [Trait("DataMining", "Lookup")]
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
    [Trait("DataMining", "Lookup")]
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
    [Trait("DataMining", "Lookup")]
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
    [Trait("DataMining", "Lookup")]
    public void PoemWithMultipleSubcategory()
    {
        var poems = _data.Seasons.SelectMany(x => x.Poems).Where(x => x.Categories.Any(x => x.SubCategories.Count > 1));
        foreach (var poem in poems)
            _testOutputHelper.WriteLine(poem.Id);
    }

    [Fact]
    [Trait("DataMining", "Lookup")]
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
    [Trait("DataMining", "Lookup")]
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

    [Fact]
    [Trait("DataMining", "Quality")]
    public void PoemsWithoutCapitalLetterAtVerseBeginning()
    {
        foreach (var poem in _data.Seasons.SelectMany(x => x.Poems))
        {
            foreach (var verse in poem.Paragraphs.SelectMany(x => x.Verses))
            {
                var letter = verse[0].ToString();
                if (letter != letter.ToUpperInvariant())
                {
                    _testOutputHelper.WriteLine(poem.Id);
                }
            }
        }
    }

    [Fact]
    [Trait("DataMining", "Quality")]
    public void PoemsWithoutMoreThanOneCapitalLetterInTitle()
    {
        foreach (var poem in _data.Seasons.SelectMany(x => x.Poems))
        {
            var partialTitle = poem.Title.Substring(1);
            if (partialTitle != partialTitle.ToLowerInvariant())
            {
                _testOutputHelper.WriteLine(poem.Id);
            }
        }
    }

    [Fact]
    [Trait("DataMining", "Quality")]
    public void PoemsThatCouldHaveQuatrainsButHaveNot()
    {
        foreach (var poem in _data.Seasons.SelectMany(x => x.Poems))
        {
            if (poem.VersesCount % 4 == 0 && !poem.HasQuatrains)
            {
                _testOutputHelper.WriteLine(poem.Id);
            }
        }
    }

    [Fact]
    [Trait("DataMining", "Output")]
    public void PossibleProperNouns()
    {
        foreach (var poem in _data.Seasons.SelectMany(x => x.Poems))
        {
            var poemIdPrinted = false;
            foreach (var verse in poem.Paragraphs.SelectMany(x => x.Verses))
            {
                if (poemIdPrinted)
                    break;
                var matches = Regex.Matches(verse, "\\b[A-Z].*?\\b");
                //Match match = Regex.Match(verse, "[A-Z]\\S*\\s");
                foreach (Match match in matches)
                {
                    if (match is { Success: true, Index: > 0 } && match.Index - 2 > 0)
                    {
                        var previousChar = verse[match.Index - 1];
                        var penultimateChar = verse[match.Index - 2];
                        if (previousChar == '"') continue;
                        if (penultimateChar == '.' || penultimateChar == '!') continue;
                        _testOutputHelper.WriteLine($"{poem.Id} : {match.Value}");
                        poemIdPrinted = true;
                        break;
                    }
                }
            }
        }
    }

    [Fact]
    [Trait("DataMining", "Lookup")]
    public void SeasonDuration()
    {
        foreach (var season in _data.Seasons)
        {
            var dates = season.Poems.Select(x => x.Date).OrderBy(x => x.Date).ToList();
            var duration = dates[dates.Count() - 1] - dates[0];
            decimal nbDays = int.Parse(duration.ToString("%d"));
            var value = nbDays;
            var unit = "days";
            if (value > 30)
            {
                value = value / 30;
                unit = "months";

                if (value > 12)
                {
                    value = value / 12;
                    unit = "years";
                }
            }

            _testOutputHelper.WriteLine($"{season.NumberedName} ({season.Period}): {value} {unit}");
        }
    }
}