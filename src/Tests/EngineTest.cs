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
