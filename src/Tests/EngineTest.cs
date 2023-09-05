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
    public void ShouldGetSeasonDirectoryName()
    {
        var engine = Helpers.CreateEngine();
        engine.Data.Seasons[0].ContentDir.Should().Be("1_premiere_saison");
    }

    [Fact]
    public void ShouldCreateFirstSeasonDirectory()
    {
        var engine = Helpers.CreateEngine();
        engine.GenerateSeasonIndexFile(1);
    }
}