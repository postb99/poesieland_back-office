using Shouldly;
using Toolbox;
using Toolbox.Settings;
using Xunit;

namespace Tests;

public class YamlMetadataProcessorTest(BasicFixture basicFixture): IClassFixture<BasicFixture>
{
    [Fact]
    [Trait("UnitTest", "ContentImport")]
    private void ShouldImportYamlMetadata()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            basicFixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "18_dix_huitieme_saison/saisons.md");
        var poemContentImporter = new PoemContentImporter(basicFixture.Configuration);
        var (poem, position) = poemContentImporter.Import(poemContentFilePath);
        poemContentImporter.HasYamlMetadata.ShouldBeTrue();
        poemContentImporter.HasTomlMetadata.ShouldBeFalse();
        poem.Title.ShouldBe("Saisons");
        poem.Id.ShouldBe("saisons_18");
        poem.TextDate.ShouldBe("01.11.2023");
        poem.Categories.Count.ShouldBe(2);
        var saisonsCategorieIndex = poem.Categories.FindIndex(x => x.Name == "Saisons");
        var natureCategorieIndex = poem.Categories.FindIndex(x => x.Name == "Nature");
        poem.Categories[saisonsCategorieIndex].SubCategories.Count.ShouldBe(4);
        poem.Categories[saisonsCategorieIndex].SubCategories.First().ShouldBe("Automne");
        poem.Categories[natureCategorieIndex].SubCategories.First().ShouldBe("Climat");
        poem.VerseLength.ShouldBe("8");
        poem.PoemType.ShouldBeNull();
        position.ShouldBe(11);
        var anomalies = poemContentImporter.CheckAnomaliesAfterImport();
        anomalies.ShouldBeEmpty();
    }

    [Fact(Skip = "Metadata updated to TOML, no more test case available")]
    [Trait("UnitTest", "ContentImport")]
    private void ShouldImportDoubleAcrosticheYamlMetadata()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            basicFixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "16_seizieme_saison/les_chenes.md");
        var poemContentImporter = new PoemContentImporter(basicFixture.Configuration);
        var (poem, _) = poemContentImporter.Import(poemContentFilePath);
        poemContentImporter.HasYamlMetadata.ShouldBeTrue();
        poemContentImporter.HasTomlMetadata.ShouldBeFalse();
        poem.DoubleAcrostiche!.First.ShouldBe("Chênes");
        poem.DoubleAcrostiche.Second.ShouldBe("destin");
        var anomalies = poemContentImporter.CheckAnomaliesAfterImport();
        anomalies.ShouldBeEmpty();
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    private void ShouldImportTypeYamlMetadata()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            basicFixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "17_dix_septieme_saison/a_bacchus.md");
        var poemContentImporter = new PoemContentImporter(basicFixture.Configuration);
        var (poem, _) = poemContentImporter.Import(poemContentFilePath);
        poemContentImporter.HasYamlMetadata.ShouldBeTrue();
        poemContentImporter.HasTomlMetadata.ShouldBeFalse();
        poem.PoemType.ShouldBe("sonnet");
        var anomalies = poemContentImporter.CheckAnomaliesAfterImport();
        anomalies.ShouldBeEmpty();
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    private void ShouldImportSingleLineInfoYamlMetadata()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            basicFixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "17_dix_septieme_saison/a_bacchus.md");
        var poemContentImporter = new PoemContentImporter(basicFixture.Configuration);
        var (poem, _) = poemContentImporter.Import(poemContentFilePath);
        poemContentImporter.HasYamlMetadata.ShouldBeTrue();
        poemContentImporter.HasTomlMetadata.ShouldBeFalse();
        poem.Info.ShouldBe("Reprise d'un poème-chanson de 1994");
        var anomalies = poemContentImporter.CheckAnomaliesAfterImport();
        anomalies.ShouldBeEmpty();
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    private void ShouldImportMultilineInfoYamlMetadata()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            basicFixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "22_vingt_deuxieme_saison/l_automne_est_venu.md");
        var poemContentImporter = new PoemContentImporter(basicFixture.Configuration);
        var (poem, _) = poemContentImporter.Import(poemContentFilePath);
        poemContentImporter.HasYamlMetadata.ShouldBeTrue();
        poemContentImporter.HasTomlMetadata.ShouldBeFalse();
        /*
        {{% notice style="primary" %}}
        Acrostiche : l'automne venu.

        Les poèmes qui commencent par ce vers...
        {{% include "../../includes/l_automne_est_venu" hidefirstheading %}}
        {{% /notice %}}
        */
        poem.Info.ShouldStartWith($"{Environment.NewLine}Les poèmes");
        poem.Info.ShouldEndWith("hidefirstheading %}}");
        poem.Info.ShouldBe(
            $"{Environment.NewLine}Les poèmes qui commencent par ce vers...{Environment.NewLine}{{{{% include \"../../includes/l_automne_est_venu\" hidefirstheading %}}}}");
        var anomalies = poemContentImporter.CheckAnomaliesAfterImport();
        anomalies.ShouldBeEmpty();
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    private void ShouldImportNullInfoYamlMetadata()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            basicFixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "18_dix_huitieme_saison/novembre.md");
        var poemContentImporter = new PoemContentImporter(basicFixture.Configuration);
        var (poem, _) = poemContentImporter.Import(poemContentFilePath);
        poemContentImporter.HasYamlMetadata.ShouldBeTrue();
        poemContentImporter.HasTomlMetadata.ShouldBeFalse();
        poem.Info.ShouldBeNull();
        var anomalies = poemContentImporter.CheckAnomaliesAfterImport();
        anomalies.ShouldBeEmpty();
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    private void ShouldImportPicturesYamlMetadata()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            basicFixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "18_dix_huitieme_saison/present_simple.md");
        var poemContentImporter = new PoemContentImporter(basicFixture.Configuration);
        var (poem, _) = poemContentImporter.Import(poemContentFilePath);
        poemContentImporter.HasYamlMetadata.ShouldBeTrue();
        poemContentImporter.HasTomlMetadata.ShouldBeFalse();
        poem.VerseLength.ShouldBe("11");
        poem.Pictures.Count.ShouldBe(2);
        poem.Pictures[0].ShouldBe("17 décembre 2023");
        poem.Pictures[1].ShouldBe("Avec mon chien le 5 juillet 2022");
        var anomalies = poemContentImporter.CheckAnomaliesAfterImport();
        anomalies.ShouldBeEmpty();
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    private void ShouldImportMultipleCategoriesWithMoreSpacesYamlMetadata()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            basicFixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "21_vingt_et_unieme_saison/soir_parfait.md");
        var poemContentImporter = new PoemContentImporter(basicFixture.Configuration);
        var (poem, _) = poemContentImporter.Import(poemContentFilePath);
        poemContentImporter.HasYamlMetadata.ShouldBeTrue();
        poemContentImporter.HasTomlMetadata.ShouldBeFalse();
        poem.Categories.Count.ShouldBe(2);
        poem.Categories.FirstOrDefault(x => x.Name == "Nature").SubCategories.Count.ShouldBe(1);
        poem.Categories.FirstOrDefault(x => x.Name == "Ombres et lumières").SubCategories.Count.ShouldBe(1);
        poem.Categories.FirstOrDefault(x => x.Name == "Nature").SubCategories.FirstOrDefault().ShouldBe("Ciel");
        poem.Categories.FirstOrDefault(x => x.Name == "Ombres et lumières").SubCategories.FirstOrDefault().ShouldBe("Crépuscule");
        var anomalies = poemContentImporter.CheckAnomaliesAfterImport();
        anomalies.ShouldBeEmpty();
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    private void ShouldExtractTags()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            basicFixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "18_dix_huitieme_saison/saisons.md");
        var poemContentImporter = new PoemContentImporter(basicFixture.Configuration);
        var partialImport = poemContentImporter.GetPartialImport(poemContentFilePath);
        poemContentImporter.HasYamlMetadata.ShouldBeTrue();
        poemContentImporter.HasTomlMetadata.ShouldBeFalse();
        partialImport.Tags.Count.ShouldBe(3);
        partialImport.Tags[0].ShouldBe("2023");
        partialImport.Tags[1].ShouldBe("saisons");
        partialImport.Tags[2].ShouldBe("nature");
    }
    
    [Fact]
    [Trait("UnitTest", "ContentImport")]
    private void ShouldImportLovecatExtraTag()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            basicFixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "21_vingt_et_unieme_saison/humeurs_de_chats.md");
        var poemContentImporter = new PoemContentImporter(basicFixture.Configuration);
        var (poem, _) = poemContentImporter.Import(poemContentFilePath);
        poemContentImporter.HasYamlMetadata.ShouldBeTrue();
        poemContentImporter.HasTomlMetadata.ShouldBeFalse();
        poem.ExtraTags.ShouldBe(["lovecat"]);
        var anomalies = poemContentImporter.CheckAnomaliesAfterImport();
        anomalies.ShouldBeEmpty();
    }
    
    [Fact]
    [Trait("UnitTest", "ContentImport")]
    private void ShouldImportLocations()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            basicFixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "21_vingt_et_unieme_saison/serenite_sylvaine.md");
        var poemContentImporter = new PoemContentImporter(basicFixture.Configuration);
        var (poem, _) = poemContentImporter.Import(poemContentFilePath);
        poemContentImporter.HasYamlMetadata.ShouldBeTrue();
        poemContentImporter.HasTomlMetadata.ShouldBeFalse();
        poem.Locations.ShouldBe(["Lorraine"]);
        var anomalies = poemContentImporter.CheckAnomaliesAfterImport();
        anomalies.ShouldBeEmpty();
    }
}