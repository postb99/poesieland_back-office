using FluentAssertions;

namespace Tests;

public class EngineTest
{
    [Fact]
    public void ShouldLoad()
    {
        var engine = Helpers.CreateEngine();
        engine.Data.Should().NotBeNull();
    }

    [Fact]
    public void ShouldLoadAcrostiche()
    {
        var engine = Helpers.CreateEngine();
        var poemWithAcrostiche = engine.Data.Seasons[13].Poems.FirstOrDefault(x => x.Id == "resurrection_14");
        poemWithAcrostiche.Should().NotBeNull();
        poemWithAcrostiche!.Acrostiche.Should().Be("Résurrection");
    }

    [Fact]
    public void ShouldLoadDoubleAcrostiche()
    {
        var engine = Helpers.CreateEngine();
        var poemWithFirstAndSecondAcrostiche = engine.Data.Seasons[13].Poems.FirstOrDefault(x => x.Id == "laircree_14");
        poemWithFirstAndSecondAcrostiche.Should().NotBeNull();
        poemWithFirstAndSecondAcrostiche!.DoubleAcrostiche.Should().NotBeNull();
        poemWithFirstAndSecondAcrostiche!.DoubleAcrostiche!.First.Should().Be("L'air");
        poemWithFirstAndSecondAcrostiche!.DoubleAcrostiche!.Second.Should().Be("créé");
    }

    [Fact]
    public void ShouldBeSeasonContentDirectoryName()
    {
        var engine = Helpers.CreateEngine();
        engine.Data.Seasons[0].ContentDirectoryName.Should().Be("1_premiere_saison");
    }

    [Fact]
    public void ShouldCreateFirstSeasonIndexFile()
    {
        var engine = Helpers.CreateEngine();
        engine.GenerateSeasonIndexFile(1);
    }
    
    [Fact]
    public void ShouldBePoemContentFileName()
    {
        var engine = Helpers.CreateEngine();
        engine.Data.Seasons[0].Poems[0].ContentFileName.Should().Be("j_avais_l_heur_de_m_asseoir.md");
    }
    
    [Fact]
    public void ShouldBePoemSeasonId()
    {
        var engine = Helpers.CreateEngine();
        engine.Data.Seasons[0].Poems[0].SeasonId.Should().Be(1);
    }
    
    [Fact]
    public void ShouldCreateFirstPoemFile()
    {
        var engine = Helpers.CreateEngine();
        engine.GeneratePoemFile(engine.Data.Seasons[0].Poems[0]);
    }
}