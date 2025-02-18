using FluentAssertions;
using Toolbox;
using Toolbox.Settings;

namespace Tests;

public class TomlMetadataProcessorTest(BasicFixture basicFixture) : IClassFixture<BasicFixture>
{
    [Fact]
    [Trait("UnitTest", "ContentImport")]
    private void ShouldImportTomlMetadata()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            basicFixture.Configuration[Constants.CONTENT_ROOT_DIR]!,
            "1_premiere_saison/j_avais_l_heur_de_m_asseoir.md");
        var poemContentImporter = new PoemContentImporter();
        var (poem, position) = poemContentImporter.Import(poemContentFilePath, basicFixture.Configuration);
        poemContentImporter.HasTomlMetadata.Should().BeTrue();
        poemContentImporter.HasYamlMetadata.Should().BeFalse();
        poem.Title.Should().Be("J'avais l'heur de m'asseoir...");
        poem.Id.Should().Be("j_avais_l_heur_de_m_asseoir_1");
        poem.TextDate.Should().Be("07.12.1995");
        poem.Categories.Count.Should().Be(1);
        poem.Categories.First().Name.Should().Be("Amour");
        poem.Categories.First().SubCategories.Count.Should().Be(1);
        poem.Categories.First().SubCategories.First().Should().Be("Amour platonique");
        poem.PoemType.Should().Be("sonnet");
        poem.VerseLength.Should().Be("12");
        poem.Info.Should().BeNull();
        position.Should().Be(0);
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    private void ShouldImportInfoTomlMetadata()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            basicFixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "12_douzieme_saison/barcarolle.md");
        var poemContentImporter = new PoemContentImporter();
        var (poem, position) = poemContentImporter.Import(poemContentFilePath, basicFixture.Configuration);
        poemContentImporter.HasTomlMetadata.Should().BeTrue();
        poemContentImporter.HasYamlMetadata.Should().BeFalse();
        poem.Categories.Count.Should().Be(1);
        poem.Categories.First().SubCategories.Count.Should().Be(1);
        poem.Categories.First().SubCategories.First().Should().Be("Musique et chant");
        poem.Info.Should().Be("Inspiré par l'air homonyme d'Offenbach.");
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    private void ShouldImportMultiLineInfoTomlMetadata()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            basicFixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "3_troisieme_saison/est_ce_un_automne.md");
        var poemContentImporter = new PoemContentImporter();
        var (poem, position) = poemContentImporter.Import(poemContentFilePath, basicFixture.Configuration);
        poemContentImporter.HasTomlMetadata.Should().BeTrue();
        poemContentImporter.HasYamlMetadata.Should().BeFalse();
        /*
        {{% notice style="primary" %}}
        Encore une variation sur cette question que j'adore...
        {{% include "../../includes/est_ce_un_automne" hidefirstheading %}}
        {{% /notice %}}
        */
        poem.Info.Should().StartWith("{{% notice style=\"primary\" %}}");
        poem.Info.Should().EndWith("{{% /notice %}}");
        poem.Info.Should().Be(
            $"{{{{% notice style=\"primary\" %}}}}{Environment.NewLine}Encore une variation sur cette question que j'adore...{Environment.NewLine}{{{{% include \"../../includes/est_ce_un_automne\" hidefirstheading %}}}}{Environment.NewLine}{{{{% /notice %}}}}");
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    private void ShouldImportDoubleAcrosticheTomlMetadata()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            basicFixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "10_dixieme_saison/cathedrale_de_lumieres.md");
        var poemContentImporter = new PoemContentImporter();
        var (poem, position) = poemContentImporter.Import(poemContentFilePath, basicFixture.Configuration);
        poemContentImporter.HasTomlMetadata.Should().BeTrue();
        poemContentImporter.HasYamlMetadata.Should().BeFalse();
        poem.DoubleAcrostiche!.First.Should().Be("Cathédrale");
        poem.DoubleAcrostiche.Second.Should().Be("de lumières");
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    private void ShouldImportMultipleCategoriesTomlMetadata()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            basicFixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "15_quinzieme_saison/du_gris_au_noir.md");
        var poemContentImporter = new PoemContentImporter();
        var (poem, position) = poemContentImporter.Import(poemContentFilePath, basicFixture.Configuration);
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

    [Theory]
    [Trait("UnitTest", "Computation")]
    [InlineData("categories = [\"First\", \"Here and there\"]")]
    [InlineData("categories = [ \"First\", \"Here and there\" ]")]
    private void ShouldProperlyParseCategories(string categoriesLine)
    {
        var processor = new TomlMetadataProcessor();
        processor.BuildCategories(categoriesLine);
        processor.GetCategories().Should().BeEquivalentTo(new List<string> { "First", "Here and there" });
    }

    [Fact]
    [Trait("UnitTest", "Computation")]
    private void ShouldProperlyParseInfoWithQuotes()
    {
        var info = "info = \"It is a \\\"quoted text\\\"\"";
        var processor = new TomlMetadataProcessor();
        var readInfo = processor.GetInfo(info);
        readInfo.Should().Be("It is a \"quoted text\"");
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    private void ShouldImportPictures()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            basicFixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "17_dix_septieme_saison/une_derniere_visite.md");
        var poemContentImporter = new PoemContentImporter();
        var (poem, position) = poemContentImporter.Import(poemContentFilePath, basicFixture.Configuration);
        poemContentImporter.HasTomlMetadata.Should().BeTrue();
        poemContentImporter.HasYamlMetadata.Should().BeFalse();
        poem.Pictures.Count.Should().Be(4);
        poem.Pictures[0].Should().Be("Le puits du château de Ham-sous-Varsberg");
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    private void ShouldImportVariableVerseTomlMetadata()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            basicFixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "17_dix_septieme_saison/a_quai.md");
        var poemContentImporter = new PoemContentImporter();
        var (poem, position) = poemContentImporter.Import(poemContentFilePath, basicFixture.Configuration);
        poemContentImporter.HasTomlMetadata.Should().BeTrue();
        poemContentImporter.HasYamlMetadata.Should().BeFalse();
        poem.Info.Should().Be("Vers variable : 5, 2");
        poem.DetailedVerseLength.Should().Be("5, 2");
        // Because it has been copied from DetailedVerseLength by poemContentImporter.
        poem.VerseLength.Should().Be("5, 2");
    }
}