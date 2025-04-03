using Shouldly;
using Toolbox;
using Toolbox.Settings;
using Xunit;

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
        var poemContentImporter = new PoemContentImporter(basicFixture.Configuration);
        var (poem, position) = poemContentImporter.Import(poemContentFilePath);
        poemContentImporter.HasTomlMetadata.ShouldBeTrue();
        poemContentImporter.HasYamlMetadata.ShouldBeFalse();
        poem.Title.ShouldBe("J'avais l'heur de m'asseoir...");
        poem.Id.ShouldBe("j_avais_l_heur_de_m_asseoir_1");
        poem.TextDate.ShouldBe("07.12.1995");
        poem.Categories.Count.ShouldBe(1);
        poem.Categories.First().Name.ShouldBe("Amour");
        poem.Categories.First().SubCategories.Count.ShouldBe(1);
        poem.Categories.First().SubCategories.First().ShouldBe("Amour platonique");
        poem.PoemType.ShouldBe("sonnet");
        poem.VerseLength.ShouldBe("12");
        poem.Info.ShouldBeNull();
        position.ShouldBe(0);
        var anomalies = poemContentImporter.CheckAnomaliesAfterImport();
        anomalies.ShouldBeEmpty();
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    private void ShouldImportInfoTomlMetadata()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            basicFixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "12_douzieme_saison/barcarolle.md");
        var poemContentImporter = new PoemContentImporter(basicFixture.Configuration);
        var (poem, _) = poemContentImporter.Import(poemContentFilePath);
        poemContentImporter.HasTomlMetadata.ShouldBeTrue();
        poemContentImporter.HasYamlMetadata.ShouldBeFalse();
        poem.Categories.Count.ShouldBe(1);
        poem.Categories.First().SubCategories.Count.ShouldBe(1);
        poem.Categories.First().SubCategories.First().ShouldBe("Musique et chant");
        poem.Info.ShouldBe("Inspiré par l'air homonyme d'Offenbach.");
        var anomalies = poemContentImporter.CheckAnomaliesAfterImport();
        anomalies.ShouldBeEmpty();
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    private void ShouldImportMultiLineInfoTomlMetadata()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            basicFixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "3_troisieme_saison/est_ce_un_automne.md");
        var poemContentImporter = new PoemContentImporter(basicFixture.Configuration);
        var (poem, _) = poemContentImporter.Import(poemContentFilePath);
        poemContentImporter.HasTomlMetadata.ShouldBeTrue();
        poemContentImporter.HasYamlMetadata.ShouldBeFalse();
        /*
        {{% notice style="primary" %}}
        Encore une variation sur cette question que j'adore...
        {{% include "../../includes/est_ce_un_automne" hidefirstheading %}}
        {{% /notice %}}
        */
        poem.Info.ShouldStartWith("{{% notice style=\"primary\" %}}");
        poem.Info.ShouldEndWith("{{% /notice %}}");
        poem.Info.ShouldBe(
            $"{{{{% notice style=\"primary\" %}}}}{Environment.NewLine}Encore une variation sur cette question que j'adore...{Environment.NewLine}{{{{% include \"../../includes/est_ce_un_automne\" hidefirstheading %}}}}{Environment.NewLine}{{{{% /notice %}}}}");
        var anomalies = poemContentImporter.CheckAnomaliesAfterImport();
        anomalies.ShouldBeEmpty();
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    private void ShouldImportDoubleAcrosticheTomlMetadata()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            basicFixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "10_dixieme_saison/cathedrale_de_lumieres.md");
        var poemContentImporter = new PoemContentImporter(basicFixture.Configuration);
        var (poem, _) = poemContentImporter.Import(poemContentFilePath);
        poemContentImporter.HasTomlMetadata.ShouldBeTrue();
        poemContentImporter.HasYamlMetadata.ShouldBeFalse();
        poem.DoubleAcrostiche!.First.ShouldBe("Cathédrale");
        poem.DoubleAcrostiche.Second.ShouldBe("de lumières");
        var anomalies = poemContentImporter.CheckAnomaliesAfterImport();
        anomalies.ShouldBeEmpty();
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    private void ShouldImportMultipleCategoriesTomlMetadata()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            basicFixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "15_quinzieme_saison/du_gris_au_noir.md");
        var poemContentImporter = new PoemContentImporter(basicFixture.Configuration);
        var (poem, _) = poemContentImporter.Import(poemContentFilePath);
        poemContentImporter.HasTomlMetadata.ShouldBeTrue();
        poemContentImporter.HasYamlMetadata.ShouldBeFalse();
        poem.Acrostiche.ShouldBe("Du gris au noir");
        poem.Categories.Count.ShouldBe(2);
        poem.Categories.FirstOrDefault(x => x.Name == "Saisons").SubCategories.Count.ShouldBe(1);
        poem.Categories.FirstOrDefault(x => x.Name == "Ombres et lumières").SubCategories.Count.ShouldBe(2);
        poem.Categories.FirstOrDefault(x => x.Name == "Saisons").SubCategories.FirstOrDefault().ShouldBe("Automne");
        poem.Categories.FirstOrDefault(x => x.Name == "Ombres et lumières").SubCategories.ShouldContain("Ville");
        poem.Categories.FirstOrDefault(x => x.Name == "Ombres et lumières").SubCategories.ShouldContain("Crépuscule");
        var anomalies = poemContentImporter.CheckAnomaliesAfterImport();
        anomalies.ShouldBeEmpty();
    }
    
    [Fact]
    [Trait("UnitTest", "ContentImport")]
    private void ShouldImportLovecatExtraTag()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            basicFixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "16_seizieme_saison/un_chat_voisin.md");
        var poemContentImporter = new PoemContentImporter(basicFixture.Configuration);
        var (poem, _) = poemContentImporter.Import(poemContentFilePath);
        poemContentImporter.HasTomlMetadata.ShouldBeTrue();
        poemContentImporter.HasYamlMetadata.ShouldBeFalse();
        poem.ExtraTags.ShouldBe(["lovecat"]);
        var anomalies = poemContentImporter.CheckAnomaliesAfterImport();
        anomalies.ShouldBeEmpty();
    }
    
    [Fact]
    [Trait("UnitTest", "ContentImport")]
    private void ShouldImportLocations()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            basicFixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "12_douzieme_saison/pelerinage.md");
        var poemContentImporter = new PoemContentImporter(basicFixture.Configuration);
        var (poem, _) = poemContentImporter.Import(poemContentFilePath);
        poemContentImporter.HasTomlMetadata.ShouldBeTrue();
        poemContentImporter.HasYamlMetadata.ShouldBeFalse();
        poem.Locations.ShouldBe(["Reims"]);
        var anomalies = poemContentImporter.CheckAnomaliesAfterImport();
        anomalies.ShouldBeEmpty();
    }

    [Theory]
    [Trait("UnitTest", "Computation")]
    [InlineData("categories = [\"First\", \"Here and there\"]")]
    [InlineData("categories = [ \"First\", \"Here and there\" ]")]
    private void ShouldProperlyParseCategories(string categoriesLine)
    {
        var processor = new TomlMetadataProcessor();
        processor.BuildCategories(categoriesLine);
        processor.GetCategories().ShouldBeEquivalentTo(new List<string> { "First", "Here and there" });
    }

    [Fact]
    [Trait("UnitTest", "Computation")]
    private void ShouldProperlyParseInfoWithQuotes()
    {
        var info = "info = \"It is a \\\"quoted text\\\"\"";
        var processor = new TomlMetadataProcessor();
        var readInfo = processor.GetInfo(info);
        readInfo.ShouldBe("It is a \"quoted text\"");
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    private void ShouldImportPictures()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            basicFixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "17_dix_septieme_saison/une_derniere_visite.md");
        var poemContentImporter = new PoemContentImporter(basicFixture.Configuration);
        var (poem, _) = poemContentImporter.Import(poemContentFilePath);
        poemContentImporter.HasTomlMetadata.ShouldBeTrue();
        poemContentImporter.HasYamlMetadata.ShouldBeFalse();
        poem.Pictures.ShouldNotBeNull();
        poem.Pictures.Count.ShouldBe(4);
        poem.Pictures[0].ShouldBe("Le puits du château de Ham-sous-Varsberg");
        var anomalies = poemContentImporter.CheckAnomaliesAfterImport();
        anomalies.ShouldBeEmpty();
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    private void ShouldImportVariableVerseTomlMetadata()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            basicFixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "17_dix_septieme_saison/a_quai.md");
        var poemContentImporter = new PoemContentImporter(basicFixture.Configuration);
        var (poem, _) = poemContentImporter.Import(poemContentFilePath);
        poemContentImporter.HasTomlMetadata.ShouldBeTrue();
        poemContentImporter.HasYamlMetadata.ShouldBeFalse();
        poem.Info.ShouldBe("Métrique variable : 5, 2");
        poem.DetailedMetric.ShouldBe("5, 2");
        // Because it has been copied from DetailedVerseLength by poemContentImporter.
        poem.VerseLength.ShouldBe("5, 2");
        var anomalies = poemContentImporter.CheckAnomaliesAfterImport();
        anomalies.ShouldBeEmpty();
    }
}