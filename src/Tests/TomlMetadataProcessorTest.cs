using FluentAssertions;
using Toolbox;
using Toolbox.Settings;

namespace Tests;

public class TomlMetadataProcessorTest
{
    [Fact]
    private void ShouldImportTomlMetadata()
    {
        var configuration = Helpers.GetConfiguration();
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            configuration[Constants.CONTENT_ROOT_DIR], "1_premiere_saison\\j_avais_l_heur_de_m_asseoir.md");
        var poemContentImporter = new PoemContentImporter();
        var (poem, position) = poemContentImporter.Import(poemContentFilePath, configuration);
        poemContentImporter.HasTomlMetadata.Should().BeTrue();
        poemContentImporter.HasYamlMetadata.Should().BeFalse();
        poem.Title.Should().Be("J'avais l'heur de m'asseoir...");
        poem.Id.Should().Be("j_avais_l_heur_de_m_asseoir_1");
        poem.TextDate.Should().Be("07.12.1995");
        poem.Categories.Count.Should().Be(1);
        poem.Categories.First().Name.Should().Be("Amour");
        poem.Categories.First().SubCategories.Count.Should().Be(1);
        poem.Categories.First().SubCategories.First().Should().Be("Femme");
        poem.PoemType.Should().Be("sonnet");
        poem.VerseLength.Should().Be("12");
        position.Should().Be(0);
    }

    [Fact]
    private void ShouldImportInfoTomlMetadata()
    {
        var configuration = Helpers.GetConfiguration();
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            configuration[Constants.CONTENT_ROOT_DIR], "12_douzieme_saison\\barcarolle.md");
        var poemContentImporter = new PoemContentImporter();
        var (poem, position) = poemContentImporter.Import(poemContentFilePath, configuration);
        poemContentImporter.HasTomlMetadata.Should().BeTrue();
        poemContentImporter.HasYamlMetadata.Should().BeFalse();
        poem.Categories.Count.Should().Be(1);
        poem.Categories.First().SubCategories.Count.Should().Be(1);
        poem.Categories.First().SubCategories.First().Should().Be("Musique et chant");
        poem.Info.Should().Be("Inspiré par l'air homonyme d'Offenbach.");
    }

    [Fact]
    private void ShouldImportDoubleAcrosticheTomlMetadata()
    {
        var configuration = Helpers.GetConfiguration();
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            configuration[Constants.CONTENT_ROOT_DIR], "10_dixieme_saison\\cathedrale_de_lumieres.md");
        var poemContentImporter = new PoemContentImporter();
        var (poem, position) = poemContentImporter.Import(poemContentFilePath, configuration);
        poemContentImporter.HasTomlMetadata.Should().BeTrue();
        poemContentImporter.HasYamlMetadata.Should().BeFalse();
        poem.DoubleAcrostiche.First.Should().Be("Cathédrale");
        poem.DoubleAcrostiche.Second.Should().Be("de lumières");
    }

    [Fact]
    private void ShouldImportMultipleCategoriesTomlMetadata()
    {
        var configuration = Helpers.GetConfiguration();
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            configuration[Constants.CONTENT_ROOT_DIR], "15_quinzieme_saison\\du_gris_au_noir.md");
        var poemContentImporter = new PoemContentImporter();
        var (poem, position) = poemContentImporter.Import(poemContentFilePath, configuration);
        poemContentImporter.HasTomlMetadata.Should().BeTrue();
        poemContentImporter.HasYamlMetadata.Should().BeFalse();
        poem.Acrostiche.Should().Be("Du gris au noir");
        poem.Categories.Count.Should().Be(2);
        poem.Categories.FirstOrDefault(x => x.Name == "Saisons").SubCategories.Count.Should().Be(1);
        poem.Categories.FirstOrDefault(x => x.Name == "Ombres et lumières").SubCategories.Count.Should().Be(2);
        poem.Categories.FirstOrDefault(x => x.Name == "Saisons").SubCategories.FirstOrDefault().Should()
            .Be("Automne");
        poem.Categories.FirstOrDefault(x => x.Name == "Ombres et lumières").SubCategories.Should().Contain("Ville");
        poem.Categories.FirstOrDefault(x => x.Name == "Ombres et lumières").SubCategories.Should()
            .Contain("Crépuscule");
    }

    [Fact]
    private void ShouldProperlyParseCategories()
    {
        var categories = "categories = [\"First\", \"Here and there\"]";
        var processor = new TomlMetadataProcessor();
        processor.BuildCategories(categories);
        processor.GetCategories().Should().BeEquivalentTo(new List<string> { "First", "Here and there" });
    }

    [Fact]
    private void ShouldProperlyParseInfoWithQuotes()
    {
        var info = "info = \"It is a \\\"quoted text\\\"\"";
        var processor = new TomlMetadataProcessor();
        var readInfo = processor.GetInfo(info);
        readInfo.Should().Be("It is a \"quoted text\"");
    }
}