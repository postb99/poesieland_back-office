﻿using FluentAssertions;

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
        var poemWithFirstAndSecondAcrostiche = engine.Data.Seasons[13].Poems.FirstOrDefault(x => x.Id == "laircree_14");
        poemWithFirstAndSecondAcrostiche.Should().NotBeNull();
        poemWithFirstAndSecondAcrostiche!.CrossingAcrostiche.Should().NotBeNull();
        poemWithFirstAndSecondAcrostiche!.CrossingAcrostiche!.First.Should().Be("L'air");
        poemWithFirstAndSecondAcrostiche!.CrossingAcrostiche!.Second.Should().Be("créé");
    }
}