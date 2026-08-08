using System.Text;
using Shouldly;
using Tests.Customizations;
using Toolbox.Consistency;
using Toolbox.Domain;
using Toolbox.Importers;
using Toolbox.Settings;
using Xunit;

namespace Tests.Importers;

public class PoemImporterTest(BasicFixture fixture) : IClassFixture<BasicFixture>
{
    [Theory]
    [Trait("UnitTest", "ContentImport")]
    [InlineAutoDomainData("some_poem")]
    [InlineAutoDomainData("somepoem")]
    public void ShouldNotImportPoemWithIdNotEndingWithSeasonId(string poemId, Root data)
    {
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var act = () => poemContentImporter.ImportPoem(poemId, data);
        var ex = act.ShouldThrow<MetadataConsistencyException>();
        ex.Message.ShouldBe($"'{poemId}' does not end with season id");
    }

    [Theory]
    [Trait("UnitTest", "ContentImport")]
    [AutoDomainData]
    public void ShouldNotImportPoemWhoseSeasonDirectoryDoesNotExist(Root data)
    {
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var act = () => poemContentImporter.ImportPoem("some_poem_99", data);
        var ex = act.ShouldThrow<MetadataConsistencyException>();
        ex.Message.ShouldBe(
            $"No such season content directory for id '99'. Create season directory before importing poem");
    }

    [Theory]
    [Trait("UnitTest", "ContentImport")]
    [AutoDomainData]
    public void ShouldNotImportPoemWhoseContentFileDoesNotExist(Root data)
    {
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var act = () => poemContentImporter.ImportPoem("some_poem_16", data);
        var ex = act.ShouldThrow<MetadataConsistencyException>();
        ex.Message.ShouldStartWith($"Poem content file not found: ");
    }

    [Theory]
    [Trait("UnitTest", "ContentImport")]
    [AutoDomainData]
    public void ShouldImportPoemToANewSeason(Root data, Poem poem)
    {
        var seasonCount = data.Seasons.Count;
        var seasonId = seasonCount + 1;
        poem.Id = $"some_title_{seasonId}";

        var poemContentImporter = new PoemImporter(fixture.Configuration);
        poemContentImporter.ImportPoemToSeason(data, poem);

        data.Seasons.Count.ShouldBe(seasonCount + 1);
        data.Seasons.First(x => x.Id == seasonId).Poems.Count.ShouldBe(1);
    }

    [Theory]
    [Trait("UnitTest", "ContentImport")]
    [AutoDomainData]
    public void ShouldImportPoemToASeason(Root data, Poem poem, params List<Poem> existingPoems)
    {
        var season = data.Seasons.Last();
        season.Poems.AddRange(existingPoems);
        var poemCount = season.Poems.Count;
        poem.Id = $"some_title_{season.Id}";

        var poemContentImporter = new PoemImporter(fixture.Configuration);
        poemContentImporter.ImportPoemToSeason(data, poem);

        data.Seasons.First(x => x.Id == season.Id).Poems.Count.ShouldBe(poemCount + 1);
        data.Seasons.First(x => x.Id == season.Id).Poems.Last().Id.ShouldBe(poem.Id);
    }
    
    [Theory]
    [Trait("UnitTest", "ContentImport")]
    [AutoDomainData]
    public void ShouldImportPoemToAPositionInSeason(Root data, Poem poem, params List<Poem> existingPoems)
    {
        var season = data.Seasons.Last();
        season.Poems.AddRange(existingPoems);
        var poemCount = season.Poems.Count;
        poem.Id = $"some_title_{season.Id}";
        var contentFileIndex = 1;
        foreach (var existingPoem in data.Seasons.Last().Poems)
        {
            existingPoem.ContentFileIndex = contentFileIndex++;
        }

        poem.ContentFileIndex = 2;

        var poemContentImporter = new PoemImporter(fixture.Configuration);
        poemContentImporter.ImportPoemToSeason(data, poem);

        data.Seasons.First(x => x.Id == season.Id).Poems.Count.ShouldBe(poemCount + 1);
        data.Seasons.First(x => x.Id == season.Id).Poems[1].Id.ShouldBe(poem.Id);
    }

    [Theory]
    [Trait("UnitTest", "ContentImport")]
    [AutoDomainData]
    public void ShouldUpdatePoemPositionToASeason(Root data, params List<Poem> existingPoems)
    {
        var season = data.Seasons.Last();
        season.Poems.AddRange(existingPoems);
        var poemCount = season.Poems.Count;
        var contentFileIndex = 1;
        foreach (var existingPoem in existingPoems)
        {
            existingPoem.ContentFileIndex = contentFileIndex++;
        }

        var poemToUpdate = season.Poems.Last();
        poemToUpdate.Id = $"some_title_{season.Id}";
        poemToUpdate.ContentFileIndex = 2;

        var poemContentImporter = new PoemImporter(fixture.Configuration);
        poemContentImporter.ImportPoemToSeason(data, poemToUpdate);

        data.Seasons.First(x => x.Id == season.Id).Poems.Count.ShouldBe(poemCount);
        data.Seasons.First(x => x.Id == season.Id).Poems[1].Id.ShouldBe(poemToUpdate.Id);
    }

    [Theory]
    [Trait("UnitTest", "ContentImport")]
    [AutoDomainData]
    public void ShouldImportPoemsOfSeason(Root data)
    {
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        poemContentImporter.ImportPoemsOfSeason(16, data);
        data.Seasons.FirstOrDefault(x => x.Id == 16).ShouldNotBeNull();
        data.Seasons.FirstOrDefault(x => x.Id == 16).Poems.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    public void ShouldImportVariableVerseLength()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            fixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "3_troisieme_saison/jeux_de_nuits.md");
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var (poem, _) = poemContentImporter.Import(poemContentFilePath);
        poem.Info.ShouldBe("Métrique variable : 8, 6, 4, 2");
        poem.DetailedMetric.ShouldBe("8, 6, 4, 2");
        // Because it has been copied from DetailedVerseLength by poemContentImporter.
        poem.VerseLength.ShouldBe("8, 6, 4, 2");
        poemContentImporter.VerifyAnomaliesAfterImport();
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    public void ShouldImportVariableVerseLengthWhenMoreTextAfterVerseLength()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            fixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "19_dix_neuvieme_saison/urgence.md");
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var (poem, _) = poemContentImporter.Import(poemContentFilePath);
        var expectedInfo = new StringBuilder("Métrique variable : 5, 2.").Append(Environment.NewLine)
            .Append(Environment.NewLine).Append("{{% include").ToString();
        poem.Info.ShouldStartWith(expectedInfo);
        poem.DetailedMetric.ShouldBe("5, 2");
        // Because it has been copied from DetailedVerseLength by poemContentImporter.
        poem.VerseLength.ShouldBe("5, 2");
        poemContentImporter.VerifyAnomaliesAfterImport();
    }

    [Fact]
    [Trait("UnitTest", "Computation")]
    public void ShouldFindExtraTags()
    {
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        poemContentImporter
            .FindExtraTags(["lovecat", "2025", "nature", "sonnet", "métrique variable", "other", "octosyllabe"])
            .ShouldBe(["lovecat", "other"]);
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    public void ShouldImportEnPoem()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            fixture.Configuration[Constants.CONTENT_ROOT_DIR_EN]!, "2024", "wisdom.md");
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var (poem, _) = poemContentImporter.ImportEnYaml(poemContentFilePath);
        poem.ShouldNotBeNull();
        poem.Categories.FirstOrDefault()?.Name.ShouldBe("Philosophie");
        poem.Categories.FirstOrDefault()?.SubCategories.FirstOrDefault().ShouldBe("Etre");
    }
}