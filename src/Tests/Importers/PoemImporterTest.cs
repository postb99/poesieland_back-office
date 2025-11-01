using Shouldly;
using Tests.Customizations;
using Toolbox.Domain;
using Toolbox.Importers;
using Toolbox.Settings;
using Xunit;

namespace Tests.Importers;

public class PoemImporterTest(BasicFixture basicFixture): IClassFixture<BasicFixture>
{
    [Theory]
    [Trait("UnitTest", "ContentImport")]
    [InlineAutoDomainData("some_poem")]
    [InlineAutoDomainData("somepoem")]
    public void ShouldNotImportPoemWithIdNotEndingWithSeasonId(string poemId, Root data)
    {
        var poemContentImporter = new PoemImporter(basicFixture.Configuration);
        var act = () => poemContentImporter.ImportPoem(poemId, data);
        var ex = act.ShouldThrow<ArgumentException>();
        ex.Message.ShouldBe($"'{poemId}' does not end with season id");
    }
    
    [Theory]
    [Trait("UnitTest", "ContentImport")]
    [AutoDomainData]
    public void ShouldNotImportPoemWhoseSeasonDirectoryDoesNotExist(Root data)
    {
        var poemContentImporter = new PoemImporter(basicFixture.Configuration);
        var act = () => poemContentImporter.ImportPoem("some_poem_99", data);
        var ex = act.ShouldThrow<ArgumentException>();
        ex.Message.ShouldBe($"No such season content directory for id '99'. Create season directory before importing poem");
    }
    
    [Theory]
    [Trait("UnitTest", "ContentImport")]
    [AutoDomainData]
    public void ShouldNotImportPoemWhoseContentFileDoesNotExist(Root data)
    {
        var poemContentImporter = new PoemImporter(basicFixture.Configuration);
        var act = () => poemContentImporter.ImportPoem("some_poem_16", data);
        var ex = act.ShouldThrow<ArgumentException>();
        ex.Message.ShouldStartWith($"Poem content file not found: ");
    }
    
    [Theory]
    [Trait("UnitTest", "ContentImport")]
    [AutoDomainData]
    public void ShouldImportPoemsOfSeason(Root data)
    {
        var poemContentImporter = new PoemImporter(basicFixture.Configuration);
        poemContentImporter.ImportPoemsOfSeason(16, data);
        data.Seasons.FirstOrDefault(x => x.Id == 16).ShouldNotBeNull();
        data.Seasons.FirstOrDefault(x => x.Id == 16).Poems.ShouldNotBeEmpty();
    }
    
    [Fact]
    [Trait("UnitTest", "ContentImport")]
    public void ShouldImportVariableVerseLength()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            basicFixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "3_troisieme_saison/jeux_de_nuits.md");
        var poemContentImporter = new PoemImporter(basicFixture.Configuration);
        var (poem, _) = poemContentImporter.Import(poemContentFilePath);
        poem.Info.ShouldBe("Métrique variable : 8, 6, 4, 2");
        poem.DetailedMetric.ShouldBe("8, 6, 4, 2");
        // Because it has been copied from DetailedVerseLength by poemContentImporter.
        poem.VerseLength.ShouldBe("8, 6, 4, 2");
        var anomalies = poemContentImporter.CheckAnomaliesAfterImport();
        anomalies.ShouldBeEmpty();
    }

    [Fact]
    [Trait("UnitTest", "Computation")]
    public void ShouldFindExtraTags()
    {
        var poemContentImporter = new PoemImporter(basicFixture.Configuration);
        poemContentImporter.FindExtraTags(["lovecat", "2025", "nature", "sonnet", "métrique variable", "other", "octosyllabe"]).ShouldBe(["lovecat", "other"]);
    }
}