using AutoFixture;
using Shouldly;
using Toolbox;
using Toolbox.Domain;
using Xunit;
using Xunit.Abstractions;

namespace Tests;

public class DataDependantEngineTest(LoadDataFixture fixture, ITestOutputHelper testOutputHelper)
    : IClassFixture<LoadDataFixture>
{
    private readonly Engine _engine = fixture.Engine;

    [Fact]
    [Trait("UnitTest", "Quality")]
    public void ShouldCorrectlyComputeVerseLengthDataDict()
    {
        var dataDict = _engine.FillVerseLengthDataDict(out var _);
        testOutputHelper.WriteLine(
            $"Last non-empty season poem count: {_engine.Data.Seasons.Last(x => x.Poems.Count > 0).Poems.Count}");
        testOutputHelper.WriteLine(
            $"Computed values for last season: {string.Join('-', dataDict.Values.Select(x => x.Last()))}");
        var sum = dataDict.Values.Sum(x => x.Last());
        testOutputHelper.WriteLine($"Computed values sum: {sum}");
        sum.ShouldBeInRange(99.6m, 100.4m);
    }

    [Fact]
    [Trait("UnitTest", "XmlRead")]
    public void ShouldLoad()
    {
        _engine.Data.ShouldNotBeNull();
    }

    [Fact]
    [Trait("UnitTest", "XmlRead")]
    public void ShouldLoadAcrostiche()
    {
        var poemWithAcrostiche = _engine.Data.Seasons[13].Poems.FirstOrDefault(x => x.Id == "resurrection_14");
        poemWithAcrostiche.ShouldNotBeNull();
        poemWithAcrostiche!.Acrostiche.ShouldBe("Résurrection");
    }

    [Fact]
    [Trait("UnitTest", "XmlRead")]
    public void ShouldLoadDoubleAcrostiche()
    {
        var poemWithFirstAndSecondAcrostiche =
            _engine.Data.Seasons[13].Poems.FirstOrDefault(x => x.Id == "l_air_cree_14");
        poemWithFirstAndSecondAcrostiche.ShouldNotBeNull();
        poemWithFirstAndSecondAcrostiche!.DoubleAcrostiche.ShouldNotBeNull();
        poemWithFirstAndSecondAcrostiche!.DoubleAcrostiche!.First.ShouldBe("L'air");
        poemWithFirstAndSecondAcrostiche!.DoubleAcrostiche!.Second.ShouldBe("créé");
    }

    [Theory]
    [Trait("UnitTest", "XmlRead")]
    [InlineData("j_avais_l_heur_de_m_asseoir_1", 1, 14)]
    [InlineData("grand_sud_1", 1, 12)]
    [InlineData("illusion_1", 1, 8)]
    public void ShouldHaveVersesCount(string poemId, int seasonId, int expectedCount)
    {
        var poem = _engine.Data.Seasons[seasonId - 1].Poems.FirstOrDefault(x => x.Id == poemId);
        poem.VersesCount.ShouldBe(expectedCount);
    }

    [Theory]
    [Trait("UnitTest", "XmlRead")]
    [InlineData("j_avais_l_heur_de_m_asseoir_1", 1, false)]
    [InlineData("grand_sud_1", 1, true)]
    [InlineData("illusion_1", 1, false)]
    [InlineData("matin_privilege_15", 15, false)]
    [InlineData("ombres_et_lumieres_15", 15, true)]
    [InlineData("les_chenes_16", 16, true)]
    public void ShouldHaveQuatrains(string poemId, int seasonId, bool expectedHasQuatrain)
    {
        var poem = _engine.Data.Seasons[seasonId - 1].Poems.FirstOrDefault(x => x.Id == poemId);
        poem.HasQuatrains.ShouldBe(expectedHasQuatrain);
        if (expectedHasQuatrain)
        {
            poem.Paragraphs.Count.ShouldBe(poem.VersesCount / 4);
        }

        testOutputHelper.WriteLine($"{poem.Paragraphs.Count} paragraphs, {poem.VersesCount} verses");
    }

    [Fact]
    [Trait("UnitTest", "XmlRead")]
    public void ShouldBePoemSeasonId()
    {
        _engine.Data.Seasons[0].Poems[0].SeasonId.ShouldBe(1);
    }

    [Fact]
    [Trait("UnitTest", "MetadataCheck")]
    public void CheckMissingYearTagInYamlMetadata()
    {
        var anomalies = _engine.CheckMissingTagsInYamlMetadata();
        testOutputHelper.WriteLine(string.Join(Environment.NewLine, anomalies));
        anomalies.Count().ShouldBe(0);
    }
}

public class DataIndependantEngineTest(BasicFixture basicFixture, ITestOutputHelper testOutputHelper)
    : IClassFixture<BasicFixture>
{
    [Fact]
    [Trait("UnitTest", "Computation")]
    public void ShouldCorrectlyFillCategoriesBubbleChartDataDict()
    {
        Dictionary<KeyValuePair<string, string>, int> dict = new();
        var xAxisLabels = new SortedSet<string>();
        var yAxisLabels = new SortedSet<string>();

        var engine = new Engine(basicFixture.Configuration);

        // Poem with single subcategory
        var poem = new Poem { Categories = [new Category { SubCategories = ["A"] }] };
        engine.FillCategoriesBubbleChartDataDict(dict, xAxisLabels, yAxisLabels, poem);
        xAxisLabels.ShouldBeEmpty();
        yAxisLabels.ShouldBeEmpty();

        dict.ShouldBeEmpty();

        // Poem with two categories
        poem = new Poem { Categories = [new Category { SubCategories = ["A", "B"] }] };
        engine.FillCategoriesBubbleChartDataDict(dict, xAxisLabels, yAxisLabels, poem);

        var expectedKey = new KeyValuePair<string, string>("A", "B");
        dict.TryGetValue(expectedKey, out var counter).ShouldBeTrue();
        counter.ShouldBe(1);

        var unExpectedKey = new KeyValuePair<string, string>("B", "A");
        dict.TryGetValue(unExpectedKey, out var _).ShouldBeFalse();

        xAxisLabels.ToList().ShouldBeEquivalentTo(new List<string> { "A" });
        yAxisLabels.ToList().ShouldBeEquivalentTo(new List<string> { "B" });

        // Poem with three categories
        poem = new Poem { Categories = [new Category { SubCategories = ["A", "B", "C"] }] };
        engine.FillCategoriesBubbleChartDataDict(dict, xAxisLabels, yAxisLabels, poem);

        expectedKey = new KeyValuePair<string, string>("A", "B");
        dict.TryGetValue(expectedKey, out var counter2).ShouldBeTrue();
        counter2.ShouldBe(2);

        unExpectedKey = new KeyValuePair<string, string>("B", "A");
        dict.TryGetValue(unExpectedKey, out var _).ShouldBeFalse();

        expectedKey = new KeyValuePair<string, string>("A", "C");
        dict.TryGetValue(expectedKey, out var counter3).ShouldBeTrue();
        counter3.ShouldBe(1);

        expectedKey = new KeyValuePair<string, string>("B", "C");
        dict.TryGetValue(expectedKey, out var counter4).ShouldBeTrue();
        counter4.ShouldBe(1);

        unExpectedKey = new KeyValuePair<string, string>("C", "B");
        dict.TryGetValue(unExpectedKey, out var _).ShouldBeFalse();

        unExpectedKey = new KeyValuePair<string, string>("C", "A");
        dict.TryGetValue(unExpectedKey, out var _).ShouldBeFalse();

        xAxisLabels.ToList().ShouldBeEquivalentTo(new List<string> { "A", "B" });
        yAxisLabels.ToList().ShouldBeEquivalentTo(new List<string> { "B", "C" });

        // Poem with two categories, one per category
        poem = new Poem
            { Categories = [new Category { SubCategories = ["A"] }, new Category() { SubCategories = ["B"] }] };
        engine.FillCategoriesBubbleChartDataDict(dict, xAxisLabels, yAxisLabels, poem);

        expectedKey = new KeyValuePair<string, string>("A", "B");
        dict.TryGetValue(expectedKey, out var counter5).ShouldBeTrue();
        counter5.ShouldBe(3);

        unExpectedKey = new KeyValuePair<string, string>("B", "A");
        dict.TryGetValue(unExpectedKey, out var _).ShouldBeFalse();

        xAxisLabels.ToList().ShouldBeEquivalentTo(new List<string> { "A", "B" });
        yAxisLabels.ToList().ShouldBeEquivalentTo(new List<string> { "B", "C" });
    }

    [Fact]
    [Trait("UnitTest", "Computation")]
    public void ShouldCorrectlyFillCategoryMetricBubbleChartDataDict()
    {
        Dictionary<KeyValuePair<string, int>, int> dict = new();
        var xAxisLabels = new SortedSet<string>();
        
        var engine = new Engine(basicFixture.Configuration);

        // Poem with single subcategory
        var poem = new Poem { VerseLength = "6", Categories = [new Category { SubCategories = ["A"] }] };
        engine.FillCategoryMetricBubbleChartDataDict(dict, xAxisLabels, poem);
        var expectedKey = new KeyValuePair<string, int>("A", 6);
        dict.TryGetValue(expectedKey, out var counter).ShouldBeTrue();
        counter.ShouldBe(1);
        xAxisLabels.ToList().ShouldBeEquivalentTo(new List<string> { "A" });
        
        // Poem with two categories
        poem = new Poem { VerseLength = "8", Categories = [new Category { SubCategories = ["A", "B"] }] };
        engine.FillCategoryMetricBubbleChartDataDict(dict, xAxisLabels, poem);
        expectedKey = new KeyValuePair<string, int>("A", 8);
        dict.TryGetValue(expectedKey, out var counter2).ShouldBeTrue();
        counter2.ShouldBe(1);
        xAxisLabels.ToList().ShouldBeEquivalentTo(new List<string> { "A", "B" });
        
        expectedKey = new KeyValuePair<string, int>("B", 8);
        dict.TryGetValue(expectedKey, out var counter3).ShouldBeTrue();
        counter3.ShouldBe(1);
        xAxisLabels.ToList().ShouldBeEquivalentTo(new List<string> { "A", "B" });
        
        // Poem with reused metric
        poem = new Poem { VerseLength = "8", Categories = [new Category { SubCategories = ["A"] }] };
        engine.FillCategoryMetricBubbleChartDataDict(dict, xAxisLabels, poem);
        expectedKey = new KeyValuePair<string, int>("A", 8);
        dict.TryGetValue(expectedKey, out var counter4).ShouldBeTrue();
        counter4.ShouldBe(2);
        xAxisLabels.ToList().ShouldBeEquivalentTo(new List<string> { "A", "B" });
        
        // Variable metric
        poem = new Poem { VerseLength = "-1", Categories = [new Category { SubCategories = ["A"] }] };
        engine.FillCategoryMetricBubbleChartDataDict(dict, xAxisLabels, poem);
        expectedKey = new KeyValuePair<string, int>("A", 0);
        dict.TryGetValue(expectedKey, out var counter5).ShouldBeTrue();
        counter5.ShouldBe(1);
        xAxisLabels.ToList().ShouldBeEquivalentTo(new List<string> { "A", "B" });
    }

    [Fact]
    [Trait("UnitTest", "Computation")]
    public void ShouldCorrectlyGetTopMostMonths()
    {
        Dictionary<string, int> dict = new();
        dict.Add("0502", 1);
        dict.Add("0503", 1);
        dict.Add("0302", 3);
        dict.Add("0102", 1);

        var engine = new Engine(basicFixture.Configuration);
        engine.GetTopMostMonths(dict).ShouldBeEquivalentTo(new List<string> { "mars", "mai", "janvier" });
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    public void ShouldImportSeason()
    {
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "github")
            return;
        var engine = new Engine(basicFixture.Configuration);
        engine.ImportSeason(16);
    }
}