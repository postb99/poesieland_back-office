using Xunit;

namespace Tests.Persistence;

public class DataManagerTest(WithRealDataFixture fixture, ITestOutputHelper testOutputHelper)
    : IClassFixture<WithRealDataFixture>
{
    [Fact]
    [Trait("UnitTest", "Persistence")]
    public void ShouldLoad()
    {
        Assert.NotNull(fixture.Data);
        Assert.NotNull(fixture.DataEn);
    }
    
     [Fact]
    [Trait("UnitTest", "XmlRead")]
    public void ShouldLoadAcrostiche()
    {
        var poemWithAcrostiche = fixture.Data.Seasons[13].Poems.FirstOrDefault(x => x.Id == "resurrection_14");
        Assert.NotNull(poemWithAcrostiche);
        Assert.Equal("Résurrection", poemWithAcrostiche!.Acrostiche);
    }

    [Fact]
    [Trait("UnitTest", "XmlRead")]
    public void ShouldLoadDoubleAcrostiche()
    {
        var poemWithFirstAndSecondAcrostiche =
            fixture.Data.Seasons[13].Poems.FirstOrDefault(x => x.Id == "l_air_cree_14");
        Assert.NotNull(poemWithFirstAndSecondAcrostiche);
        Assert.NotNull(poemWithFirstAndSecondAcrostiche!.DoubleAcrostiche);
        Assert.Equal("L'air", poemWithFirstAndSecondAcrostiche!.DoubleAcrostiche!.First);
        Assert.Equal("créé", poemWithFirstAndSecondAcrostiche!.DoubleAcrostiche!.Second);
    }

    [Theory]
    [Trait("UnitTest", "XmlRead")]
    [InlineData("j_avais_l_heur_de_m_asseoir_1", 1, 14)]
    [InlineData("grand_sud_1", 1, 12)]
    [InlineData("illusion_1", 1, 8)]
    public void ShouldHaveVersesCount(string poemId, int seasonId, int expectedCount)
    {
        var poem = fixture.Data.Seasons[seasonId - 1].Poems.FirstOrDefault(x => x.Id == poemId);
        Assert.Equal(expectedCount, poem.VersesCount);
    }

    [Fact]
    [Trait("UnitTest", "XmlRead")]
    public void ShouldBePoemSeasonId()
    {
        Assert.Equal(1, fixture.Data.Seasons[0].Poems[0].SeasonId);
    }
}