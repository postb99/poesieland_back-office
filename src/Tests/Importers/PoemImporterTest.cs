using System.Text;
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
        var ex = Assert.Throws<MetadataConsistencyException>(act);
        Assert.Equal($"'{poemId}' does not end with season id", ex.Message);
    }

    [Theory]
    [Trait("UnitTest", "ContentImport")]
    [AutoDomainData]
    public void ShouldNotImportPoemWhoseSeasonDirectoryDoesNotExist(Root data)
    {
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var act = () => poemContentImporter.ImportPoem("some_poem_99", data);
        var ex = Assert.Throws<MetadataConsistencyException>(act);
        Assert.Equal($"No such season content directory for id '99'. Create season directory before importing poem", ex.Message);
    }

    [Theory]
    [Trait("UnitTest", "ContentImport")]
    [AutoDomainData]
    public void ShouldNotImportPoemWhoseContentFileDoesNotExist(Root data)
    {
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var act = () => poemContentImporter.ImportPoem("some_poem_16", data);
        var ex = Assert.Throws<MetadataConsistencyException>(act);
        Assert.StartsWith($"Poem content file not found: ", ex.Message);
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

        Assert.Equal(seasonCount + 1, data.Seasons.Count);
        Assert.Equal(1, data.Seasons.First(x => x.Id == seasonId).Poems.Count);
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

        Assert.Equal(poemCount + 1, data.Seasons.First(x => x.Id == season.Id).Poems.Count);
        Assert.Equal(poem.Id, data.Seasons.First(x => x.Id == season.Id).Poems.Last().Id);
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
        var contentFileIndex = 0;
        foreach (var existingPoem in data.Seasons.Last().Poems)
        {
            existingPoem.ContentFileIndex = contentFileIndex++;
        }

        poem.ContentFileIndex = 1;

        var poemContentImporter = new PoemImporter(fixture.Configuration);
        poemContentImporter.ImportPoemToSeason(data, poem);

        Assert.Equal(poemCount + 1, data.Seasons.First(x => x.Id == season.Id).Poems.Count);
        Assert.Equal(poem.Id, data.Seasons.First(x => x.Id == season.Id).Poems[1].Id);
    }

    [Theory]
    [Trait("UnitTest", "ContentImport")]
    [AutoDomainData]
    public void ShouldUpdatePoemPositionToASeason(Root data, params List<Poem> existingPoems)
    {
        var season = data.Seasons.Last();
        season.Poems.AddRange(existingPoems);
        var poemCount = season.Poems.Count;
        var contentFileIndex = 0;
        foreach (var existingPoem in existingPoems)
        {
            existingPoem.ContentFileIndex = contentFileIndex++;
        }

        var poemToUpdate = season.Poems.Last();
        poemToUpdate.Id = $"some_title_{season.Id}";
        poemToUpdate.ContentFileIndex = 1;

        var poemContentImporter = new PoemImporter(fixture.Configuration);
        poemContentImporter.ImportPoemToSeason(data, poemToUpdate);

        Assert.Equal(poemCount, data.Seasons.First(x => x.Id == season.Id).Poems.Count);
        Assert.Equal(poemToUpdate.Id, data.Seasons.First(x => x.Id == season.Id).Poems[1].Id);
    }

    [Theory]
    [Trait("UnitTest", "ContentImport")]
    [AutoDomainData]
    public void ShouldImportPoemsOfSeason(Root data)
    {
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        poemContentImporter.ImportPoemsOfSeason(16, data);
        Assert.NotNull(data.Seasons.FirstOrDefault(x => x.Id == 16));
        Assert.NotEmpty(data.Seasons.FirstOrDefault(x => x.Id == 16).Poems);
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    public void ShouldImportVariableVerseLength()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            fixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "3_troisieme_saison/jeux_de_nuits.md");
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var poem = poemContentImporter.Import(poemContentFilePath);
        Assert.Equal("Métrique variable : 8, 6, 4, 2", poem.Info);
        Assert.Equal("8, 6, 4, 2", poem.DetailedMetric);
        // Because it has been copied from DetailedVerseLength by poemContentImporter.
        Assert.Equal("8, 6, 4, 2", poem.VerseLength);
        poemContentImporter.VerifyAnomaliesAfterImport();
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    public void ShouldImportVariableVerseLengthWhenMoreTextAfterVerseLength()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            fixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "19_dix_neuvieme_saison/urgence.md");
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var poem = poemContentImporter.Import(poemContentFilePath);
        var expectedInfo = new StringBuilder("Métrique variable : 5, 2.").Append(Environment.NewLine)
            .Append(Environment.NewLine).Append("{{% include").ToString();
        Assert.StartsWith(expectedInfo, poem.Info);
        Assert.Equal("5, 2", poem.DetailedMetric);
        // Because it has been copied from DetailedVerseLength by poemContentImporter.
        Assert.Equal("5, 2", poem.VerseLength);
        poemContentImporter.VerifyAnomaliesAfterImport();
    }

    [Fact]
    [Trait("UnitTest", "Computation")]
    public void ShouldFindExtraTags()
    {
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        Assert.Equal(["lovecat", "other"], poemContentImporter.FindExtraTags(["lovecat", "2025", "nature", "sonnet", "métrique variable", "other", "octosyllabe"]));
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    public void ShouldImportEnPoem()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            fixture.Configuration[Constants.CONTENT_ROOT_DIR_EN]!, "2024", "wisdom.md");
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var poem = poemContentImporter.ImportEnYaml(poemContentFilePath);
        Assert.NotNull(poem);
        Assert.Equal("Philosophie", poem.Categories.FirstOrDefault()?.Name);
        Assert.Equal("Etre", poem.Categories.FirstOrDefault()?.SubCategories.FirstOrDefault());
    }
}