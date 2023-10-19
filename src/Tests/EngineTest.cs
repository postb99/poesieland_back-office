using AutoFixture;
using FluentAssertions;
using Toolbox.Domain;
using Xunit.Abstractions;

namespace Tests;

public class EngineTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    public EngineTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

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

    [Theory(Skip = "Validated")]
    [InlineData("simplest", false, null, false, false)]
    [InlineData("only_info", false, null, false, true)]
    [InlineData("only_type", false, PoemType.Sonnet, false, false)]
    [InlineData("type_info", false, PoemType.Sonnet, false, true)]
    [InlineData("only_acrostiche", true, null, false, false)]
    [InlineData("acrostiche_type", true, PoemType.Sonnet, false, false)]
    [InlineData("acrostiche_info", true, null, false, true)]
    [InlineData("acrostiche_type_info", true, PoemType.Sonnet, false, true)]
    [InlineData("only_double_acrostiche", false, null, true, false)]
    [InlineData("type_double_acrostiche", false, PoemType.Sonnet, true, false)]
    [InlineData("double_acrostiche_info", false, null, true, true)]
    [InlineData("double_acrostiche_type_info", false, PoemType.Sonnet, true, true)]
    public void ShouldCreatePoemFileWhateverContent(string fileName, bool isAcrostiche, PoemType? poemType,
        bool isDoubleAcrostiche, bool hasInfo)
    {
        var poem = new Fixture().Build<Poem>()
            .With(x => x.TextDate, "01.01.1900")
            .With(x => x.Title, fileName)
            .With(x => x.Id, $"{fileName}_99")
            .With(x => x.Acrostiche, isAcrostiche ? "Acrostiche" : null)
            .With(x => x.PoemType, poemType?.ToString())
            .With(x => x.DoubleAcrostiche,
                isDoubleAcrostiche ? new DoubleAcrostiche { First = "Double", Second = "Acrostiche" } : null)
            .With(x => x.Info, hasInfo ? "Some info text" : null)
            .With(x => x.VerseLength, "8")
            .Create();

        var fictiveSeason = new Season
            { Id = 99, Name = "Test", NumberedName = "Test", Summary = "Test", Poems = new List<Poem> { poem } };

        var engine = Helpers.CreateEngine();
        engine.Data.Seasons.Add(fictiveSeason);
        engine.GenerateSeasonIndexFile(fictiveSeason.Id);
        engine.GeneratePoemFile(poem);
    }

    [Fact]
    public void CheckMissingYearTagInYamlMetadata()
    {
        var engine = Helpers.CreateEngine();
        var anomalies = engine.CheckMissingYearTagInYamlMetadata();
        _testOutputHelper.WriteLine(string.Join(Environment.NewLine, anomalies));
        anomalies.Count().Should().Be(0);
    }

    [Fact]
    public void ShouldImportPoem()
    {
        var engine = Helpers.CreateEngine();
        engine.ImportPoem("j_avais_l_heur_de_m_asseoir_1");
    }
}