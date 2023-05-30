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
    public void ShouldLoadSpecialAcrostiche()
    {
        var engine = Helpers.CreateEngine();
        var SpecialpoemWithAcrostiche = engine.Data.Seasons[13].Poems.FirstOrDefault(x => x.Id == "laircree_14");
        SpecialpoemWithAcrostiche.Should().NotBeNull();
        SpecialpoemWithAcrostiche!.SpecialAcrostiche.Should().NotBeNull();
        SpecialpoemWithAcrostiche!.SpecialAcrostiche!.First.Should().Be("L'air");
        SpecialpoemWithAcrostiche!.SpecialAcrostiche!.Second.Should().Be("créé");
    }
}