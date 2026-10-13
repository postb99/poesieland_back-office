using Toolbox.Importers;
using Toolbox.Settings;
using Xunit;

namespace Tests.Processors;

public class PoemYamlMetadataProcessorTest(BasicFixture fixture) : IClassFixture<BasicFixture>
{
    [Fact]
    [Trait("UnitTest", "ContentImport")]
    public void ShouldImportMetadata()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            fixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "18_dix_huitieme_saison/saisons.md");
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var poem = poemContentImporter.Import(poemContentFilePath);
        Assert.True(poemContentImporter.HasYamlMetadata);
        Assert.False(poemContentImporter.HasTomlMetadata);
        Assert.Equal("Saisons", poem.Title);
        Assert.Equal("saisons_18", poem.Id);
        Assert.Equal("01.11.2023", poem.TextDate);
        Assert.Equal(2, poem.Categories.Count);
        var saisonsCategorieIndex = poem.Categories.FindIndex(x => x.Name == "Saisons");
        var natureCategorieIndex = poem.Categories.FindIndex(x => x.Name == "Nature");
        Assert.Equal(4, poem.Categories[saisonsCategorieIndex].SubCategories.Count);
        Assert.Equal("Automne", poem.Categories[saisonsCategorieIndex].SubCategories.First());
        Assert.Equal("Climat", poem.Categories[natureCategorieIndex].SubCategories.First());
        Assert.Equal("8", poem.VerseLength);
        Assert.Null(poem.PoemType);
        Assert.Equal(11, poem.ContentFileIndex);
        poemContentImporter.VerifyAnomaliesAfterImport();
    }

    [Fact(Skip = "Metadata updated to TOML, no more test case available")]
    [Trait("UnitTest", "ContentImport")]
    public void ShouldImportDoubleAcrostiche()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            fixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "16_seizieme_saison/les_chenes.md");
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var poem = poemContentImporter.Import(poemContentFilePath);
        Assert.True(poemContentImporter.HasYamlMetadata);
        Assert.False(poemContentImporter.HasTomlMetadata);
        Assert.Equal("Chênes", poem.DoubleAcrostiche!.First);
        Assert.Equal("destin", poem.DoubleAcrostiche.Second);
        poemContentImporter.VerifyAnomaliesAfterImport();
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    public void ShouldImportType()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            fixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "17_dix_septieme_saison/a_bacchus.md");
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var poem = poemContentImporter.Import(poemContentFilePath);
        Assert.True(poemContentImporter.HasYamlMetadata);
        Assert.False(poemContentImporter.HasTomlMetadata);
        Assert.Equal("sonnet", poem.PoemType);
        poemContentImporter.VerifyAnomaliesAfterImport();
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    public void ShouldImportEmptyInfo()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            fixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "18_dix_huitieme_saison/novembre.md");
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var poem = poemContentImporter.Import(poemContentFilePath);
        Assert.True(poemContentImporter.HasYamlMetadata);
        Assert.False(poemContentImporter.HasTomlMetadata);
        Assert.Null(poem.Info);
        poemContentImporter.VerifyAnomaliesAfterImport();
    }
    
    [Fact]
    [Trait("UnitTest", "ContentImport")]
    public void ShouldImportSingleLineInfo()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            fixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "17_dix_septieme_saison/a_bacchus.md");
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var poem = poemContentImporter.Import(poemContentFilePath);
        Assert.True(poemContentImporter.HasYamlMetadata);
        Assert.False(poemContentImporter.HasTomlMetadata);
        Assert.Equal("Reprise d'un poème-chanson de 1994", poem.Info);
        poemContentImporter.VerifyAnomaliesAfterImport();
    }
    
    [Fact]
    [Trait("UnitTest", "ContentImport")]
    public void ShouldImportMultilineInfo()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            fixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "26_vingt_sixieme_saison/quatre_soupirs.md");
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var poem = poemContentImporter.Import(poemContentFilePath);
        Assert.True(poemContentImporter.HasYamlMetadata);
        Assert.False(poemContentImporter.HasTomlMetadata);
        /*
        Reprise enjouée du début du premier vers d'un [poème de la dix-septième saison](../17_dix_septieme_saison/dormira).

        Vers [les poèmes qui évoquent les quatre saisons](../../tags/saisons/_index#les-quatre-saisons-).
        */
        Assert.StartsWith($"Reprise enjouée", poem.Info);
        Assert.EndsWith("#les-quatre-saisons-).", poem.Info);
        Assert.Equal($"Reprise enjouée du début du premier vers d'un [poème de la dix-septième saison](../17_dix_septieme_saison/dormira).{Environment.NewLine}{Environment.NewLine}Vers [les poèmes qui évoquent les quatre saisons](../../tags/saisons/_index#les-quatre-saisons-).", poem.Info);
        poemContentImporter.VerifyAnomaliesAfterImport();
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    public void ShouldImportPictures()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            fixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "18_dix_huitieme_saison/present_simple.md");
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var poem = poemContentImporter.Import(poemContentFilePath);
        Assert.True(poemContentImporter.HasYamlMetadata);
        Assert.False(poemContentImporter.HasTomlMetadata);
        Assert.Equal("11", poem.VerseLength);
        Assert.Equal(2, poem.Pictures.Count);
        Assert.Equal("17 décembre 2023", poem.Pictures[0]);
        Assert.Equal("Avec mon chien le 5 juillet 2022", poem.Pictures[1]);
        poemContentImporter.VerifyAnomaliesAfterImport();
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    public void ShouldImportMultipleCategories()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            fixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "21_vingt_et_unieme_saison/soir_parfait.md");
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var poem = poemContentImporter.Import(poemContentFilePath);
        Assert.True(poemContentImporter.HasYamlMetadata);
        Assert.False(poemContentImporter.HasTomlMetadata);
        Assert.Equal(2, poem.Categories.Count);
        Assert.Equal(1, poem.Categories.FirstOrDefault(x => x.Name == "Nature").SubCategories.Count);
        Assert.Equal(1, poem.Categories.FirstOrDefault(x => x.Name == "Ombres et lumières").SubCategories.Count);
        Assert.Equal("Ciel", poem.Categories.FirstOrDefault(x => x.Name == "Nature").SubCategories.FirstOrDefault());
        Assert.Equal("Crépuscule", poem.Categories.FirstOrDefault(x => x.Name == "Ombres et lumières").SubCategories.FirstOrDefault());
        poemContentImporter.VerifyAnomaliesAfterImport();
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    public void ShouldExtractTags()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            fixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "18_dix_huitieme_saison/saisons.md");
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var partialImport = poemContentImporter.GetPartialImport(poemContentFilePath);
        Assert.True(poemContentImporter.HasYamlMetadata);
        Assert.False(poemContentImporter.HasTomlMetadata);
        Assert.Equal(5, partialImport.Tags.Count);
        Assert.Contains("2023", partialImport.Tags);
        Assert.Contains("saisons", partialImport.Tags);
        Assert.Contains("nature", partialImport.Tags);
        Assert.Contains("octosyllabe", partialImport.Tags);
        Assert.Contains("saison incertaine", partialImport.Tags);
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    public void ShouldImportLocations()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            fixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "21_vingt_et_unieme_saison/serenite_sylvaine.md");
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var poem = poemContentImporter.Import(poemContentFilePath);
        Assert.True(poemContentImporter.HasYamlMetadata);
        Assert.False(poemContentImporter.HasTomlMetadata);
        Assert.Equal(["Lorraine", "Mont des Cats"], poem.Locations);
        poemContentImporter.VerifyAnomaliesAfterImport();
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    public void ShouldImportSingleLineDescription()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            fixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "27_vingt_septieme_saison/gare_au_chat.md");
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var poem = poemContentImporter.Import(poemContentFilePath);
        Assert.True(poemContentImporter.HasYamlMetadata);
        Assert.False(poemContentImporter.HasTomlMetadata);
        Assert.Equal("Gare au 🐈 qui **dort** / Car il te surveille", poem.Description);
        poemContentImporter.VerifyAnomaliesAfterImport();
    }
    
    [Fact]
    [Trait("UnitTest", "ContentImport")]
    public void ShouldImportDescription()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            fixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "28_vingt_huitieme_saison/equinoxes.md");
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var poem = poemContentImporter.Import(poemContentFilePath);
        Assert.True(poemContentImporter.HasYamlMetadata);
        Assert.False(poemContentImporter.HasTomlMetadata);
        Assert.Equal($"Septembre est comme mars, il ouvre une charnière", poem.Description);
        poemContentImporter.VerifyAnomaliesAfterImport();
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    public void ShouldImportMultilineWordcloud()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            fixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "28_vingt_huitieme_saison/aout.md");
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var poem = poemContentImporter.Import(poemContentFilePath);
        Assert.True(poemContentImporter.HasYamlMetadata);
        Assert.False(poemContentImporter.HasTomlMetadata);
        Assert.StartsWith($"Le mois d'août centrifuge{Environment.NewLine}Le mois d'août est spectacles", poem.WordCloud);
    }
}