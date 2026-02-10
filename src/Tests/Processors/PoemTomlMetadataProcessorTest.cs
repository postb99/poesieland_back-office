using Shouldly;
using Toolbox.Importers;
using Toolbox.Processors;
using Toolbox.Settings;
using Xunit;

namespace Tests.Processors;

public class PoemTomlMetadataProcessorTest(BasicFixture fixture) : IClassFixture<BasicFixture>
{
    [Fact]
    [Trait("UnitTest", "ContentImport")]
    private void ShouldImportTomlMetadata()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            fixture.Configuration[Constants.CONTENT_ROOT_DIR]!,
            "1_premiere_saison/j_avais_l_heur_de_m_asseoir.md");
        var poemContentImporter = new PoemImporter(fixture.Configuration);
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
        poemContentImporter.VerifyAnomaliesAfterImport();
        // TODO put back anomalies.ShouldBeEmpty();
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    private void ShouldImportSingleLineInfoTomlMetadata()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            fixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "12_douzieme_saison/barcarolle.md");
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var (poem, _) = poemContentImporter.Import(poemContentFilePath);
        poemContentImporter.HasTomlMetadata.ShouldBeTrue();
        poemContentImporter.HasYamlMetadata.ShouldBeFalse();
        poem.Categories.Count.ShouldBe(1);
        poem.Categories.First().SubCategories.Count.ShouldBe(1);
        poem.Categories.First().SubCategories.First().ShouldBe("Musique et chant");
        poem.Info.ShouldBe("Inspiré par l'air homonyme d'Offenbach.");
        poemContentImporter.VerifyAnomaliesAfterImport();
        // TODO put back anomalies.ShouldBeEmpty();
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    private void ShouldImportMultiLineInfoTomlMetadata()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            fixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "11_onzieme_saison/rester.md");
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var (poem, _) = poemContentImporter.Import(poemContentFilePath);
        poemContentImporter.HasTomlMetadata.ShouldBeTrue();
        poemContentImporter.HasYamlMetadata.ShouldBeFalse();
        /*
        "Tu es beau" en italien.

        {{% include "../../includes/trop_de_choses_auront_change" hidefirstheading %}}
        */
        poem.Info.ShouldStartWith($"\"Tu es beau\" en italien.");
        poem.Info.ShouldEndWith("hidefirstheading %}}");
        poem.Info.ShouldBe(
            $"\"Tu es beau\" en italien.{Environment.NewLine}{Environment.NewLine}{{{{% include \"../../includes/trop_de_choses_auront_change\" hidefirstheading %}}}}");
        poemContentImporter.VerifyAnomaliesAfterImport();
        // TODO put back anomalies.ShouldBeEmpty();
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    private void ShouldImportDoubleAcrosticheTomlMetadata()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            fixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "10_dixieme_saison/cathedrale_de_lumieres.md");
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var (poem, _) = poemContentImporter.Import(poemContentFilePath);
        poemContentImporter.HasTomlMetadata.ShouldBeTrue();
        poemContentImporter.HasYamlMetadata.ShouldBeFalse();
        poem.DoubleAcrostiche!.First.ShouldBe("Cathédrale");
        poem.DoubleAcrostiche.Second.ShouldBe("de lumières");
        poemContentImporter.VerifyAnomaliesAfterImport();
        // TODO put back anomalies.ShouldBeEmpty();
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    private void ShouldImportMultipleCategoriesTomlMetadata()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            fixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "15_quinzieme_saison/du_gris_au_noir.md");
        var poemContentImporter = new PoemImporter(fixture.Configuration);
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
        poemContentImporter.VerifyAnomaliesAfterImport();
        // TODO put back anomalies.ShouldBeEmpty();
    }
    
    [Fact]
    [Trait("UnitTest", "ContentImport")]
    private void ShouldImportLovecatExtraTag()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            fixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "16_seizieme_saison/un_chat_voisin.md");
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var (poem, _) = poemContentImporter.Import(poemContentFilePath);
        poemContentImporter.HasTomlMetadata.ShouldBeTrue();
        poemContentImporter.HasYamlMetadata.ShouldBeFalse();
        poem.ExtraTags.ShouldBe(["lovecat"]);
        poemContentImporter.VerifyAnomaliesAfterImport();
        // TODO put back anomalies.ShouldBeEmpty();
    }
    
    [Fact]
    [Trait("UnitTest", "ContentImport")]
    private void ShouldImportLocations()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            fixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "12_douzieme_saison/pelerinage.md");
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var (poem, _) = poemContentImporter.Import(poemContentFilePath);
        poemContentImporter.HasTomlMetadata.ShouldBeTrue();
        poemContentImporter.HasYamlMetadata.ShouldBeFalse();
        poem.Locations.ShouldBe(["Reims"]);
        poemContentImporter.VerifyAnomaliesAfterImport();
        // TODO put back anomalies.ShouldBeEmpty();
    }

    [Theory]
    [Trait("UnitTest", "Computation")]
    [InlineData("categories = [\"First\", \"Here and there\"]")]
    [InlineData("categories = [ \"First\", \"Here and there\" ]")]
    private void ShouldProperlyParseCategories(string categoriesLine)
    {
        var processor = new PoemTomlMetadataProcessor();
        processor.BuildCategories(categoriesLine);
        processor.GetCategories().ShouldBeEquivalentTo(new List<string> { "First", "Here and there" });
    }

    [Fact]
    [Trait("UnitTest", "Computation")]
    private void ShouldProperlyParseInfoWithQuotes()
    {
        var info = "info = \"It is a \\\"quoted text\\\"\"";
        var processor = new PoemTomlMetadataProcessor();
        var readInfo = processor.GetInfo(info);
        readInfo.ShouldBe("It is a \"quoted text\"");
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    private void ShouldImportPictures()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            fixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "17_dix_septieme_saison/une_derniere_visite.md");
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var (poem, _) = poemContentImporter.Import(poemContentFilePath);
        poemContentImporter.HasTomlMetadata.ShouldBeTrue();
        poemContentImporter.HasYamlMetadata.ShouldBeFalse();
        poem.Pictures.ShouldNotBeNull();
        poem.Pictures.Count.ShouldBe(4);
        poem.Pictures[0].ShouldBe("Le puits du château de Ham-sous-Varsberg");
        poemContentImporter.VerifyAnomaliesAfterImport();
        // TODO put back anomalies.ShouldBeEmpty();
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    private void ShouldImportVariableVerseTomlMetadata()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            fixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "17_dix_septieme_saison/a_quai.md");
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var (poem, _) = poemContentImporter.Import(poemContentFilePath);
        poemContentImporter.HasTomlMetadata.ShouldBeTrue();
        poemContentImporter.HasYamlMetadata.ShouldBeFalse();
        poem.Info.ShouldBe("Métrique variable : 5, 2");
        poem.DetailedMetric.ShouldBe("5, 2");
        // Because it has been copied from DetailedVerseLength by poemContentImporter.
        poem.VerseLength.ShouldBe("5, 2");
        poemContentImporter.VerifyAnomaliesAfterImport();
        // TODO put back anomalies.ShouldBeEmpty();
    }
    
    [Fact]
    [Trait("UnitTest", "ContentImport")]
    private void ShouldImportMultilineTags()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            fixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "21_vingt_et_unieme_saison/le_jour_decroit.md");
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var (poem, _) = poemContentImporter.Import(poemContentFilePath);
        poemContentImporter.HasTomlMetadata.ShouldBeTrue();
        poemContentImporter.HasYamlMetadata.ShouldBeFalse();
        poem.ExtraTags.ShouldBeEquivalentTo(new List<string>{"refrain", "les mois"});
        poemContentImporter.VerifyAnomaliesAfterImport();
        // TODO put back anomalies.ShouldBeEmpty();
    }
    
    [Fact]
    [Trait("UnitTest", "ContentImport")]
    private void ShouldImportDescription()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            fixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "3_troisieme_saison/est_ce_un_automne.md");
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var (poem, _) = poemContentImporter.Import(poemContentFilePath);
        poemContentImporter.HasTomlMetadata.ShouldBeTrue();
        poemContentImporter.HasYamlMetadata.ShouldBeFalse();
        poem.Description.ShouldBe("Est-ce un automne, est-ce un printemps / Qui dans mon cœur se renouvelle");
        poemContentImporter.VerifyAnomaliesAfterImport();
        // TODO put back anomalies.ShouldBeEmpty();
    }

}
