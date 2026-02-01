using Shouldly;
using Toolbox;
using Toolbox.Domain;
using Xunit;

namespace Tests;

public class DataDependantEngineTest(WithRealDataFixture fixture, ITestOutputHelper testOutputHelper)
    : IClassFixture<WithRealDataFixture>
{
    private readonly Engine _engine = fixture.Engine;

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


}

public class DataIndependantEngineTest(BasicFixture fixture, ITestOutputHelper testOutputHelper)
    : IClassFixture<BasicFixture>
{
    [Fact]
    [Trait("UnitTest", "Computation")]
    public void ShouldCorrectlyFillCategoryMetricBubbleChartDataDict()
    {
        Dictionary<KeyValuePair<string, int>, int> dict = new();
        var xAxisLabels = new SortedSet<string>();
        
        var engine = new Engine(fixture.Configuration, fixture.DataManager);

        // Poem with single subcategory
        var poem = new Poem { VerseLength = "6", Categories = [new() { SubCategories = ["A"] }] };
        engine.FillCategoryMetricBubbleChartDataDict(dict, xAxisLabels, poem);
        var expectedKey = new KeyValuePair<string, int>("A", 6);
        dict.TryGetValue(expectedKey, out var counter).ShouldBeTrue();
        counter.ShouldBe(1);
        xAxisLabels.ToList().ShouldBeEquivalentTo(new List<string> { "A" });
        
        // Poem with two categories
        poem = new() { VerseLength = "8", Categories = [new() { SubCategories = ["A", "B"] }] };
        engine.FillCategoryMetricBubbleChartDataDict(dict, xAxisLabels, poem);
        expectedKey = new("A", 8);
        dict.TryGetValue(expectedKey, out var counter2).ShouldBeTrue();
        counter2.ShouldBe(1);
        xAxisLabels.ToList().ShouldBeEquivalentTo(new List<string> { "A", "B" });
        
        expectedKey = new("B", 8);
        dict.TryGetValue(expectedKey, out var counter3).ShouldBeTrue();
        counter3.ShouldBe(1);
        xAxisLabels.ToList().ShouldBeEquivalentTo(new List<string> { "A", "B" });
        
        // Poem with reused metric
        poem = new() { VerseLength = "8", Categories = [new() { SubCategories = ["A"] }] };
        engine.FillCategoryMetricBubbleChartDataDict(dict, xAxisLabels, poem);
        expectedKey = new("A", 8);
        dict.TryGetValue(expectedKey, out var counter4).ShouldBeTrue();
        counter4.ShouldBe(2);
        xAxisLabels.ToList().ShouldBeEquivalentTo(new List<string> { "A", "B" });
        
        // Variable metric
        poem = new() { VerseLength = "-1", Categories = [new() { SubCategories = ["A"] }] };
        engine.FillCategoryMetricBubbleChartDataDict(dict, xAxisLabels, poem);
        expectedKey = new("A", 0);
        dict.TryGetValue(expectedKey, out var counter5).ShouldBeTrue();
        counter5.ShouldBe(1);
        xAxisLabels.ToList().ShouldBeEquivalentTo(new List<string> { "A", "B" });
    }
}