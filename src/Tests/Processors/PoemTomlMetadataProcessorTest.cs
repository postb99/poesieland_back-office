using Toolbox.Importers;
using Toolbox.Processors;
using Toolbox.Settings;
using Xunit;

namespace Tests.Processors;

public class PoemTomlMetadataProcessorTest(BasicFixture fixture) : IClassFixture<BasicFixture>
{
    [Fact]
    [Trait("UnitTest", "ContentImport")]
    public void ShouldImportMetadata()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            fixture.Configuration[Constants.CONTENT_ROOT_DIR]!,
            "1_premiere_saison/j_avais_l_heur_de_m_asseoir.md");
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var poem = poemContentImporter.Import(poemContentFilePath);
        Assert.True(poemContentImporter.HasTomlMetadata);
        Assert.False(poemContentImporter.HasYamlMetadata);
        Assert.Equal("J'avais l'heur de m'asseoir...", poem.Title);
        Assert.Equal("j_avais_l_heur_de_m_asseoir_1", poem.Id);
        Assert.Equal("07.12.1995", poem.TextDate);
        Assert.Equal(1, poem.Categories.Count);
        Assert.Equal("Amour", poem.Categories.First().Name);
        Assert.Equal(1, poem.Categories.First().SubCategories.Count);
        Assert.Equal("Amour platonique", poem.Categories.First().SubCategories.First());
        Assert.Equal("sonnet", poem.PoemType);
        Assert.Equal("12", poem.VerseLength);
        Assert.Null(poem.Info);
        Assert.Equal(0, poem.ContentFileIndex);
        poemContentImporter.VerifyAnomaliesAfterImport();
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    public void ShouldImportDoubleAcrostiche()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            fixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "10_dixieme_saison/cathedrale_de_lumieres.md");
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var poem = poemContentImporter.Import(poemContentFilePath);
        Assert.True(poemContentImporter.HasTomlMetadata);
        Assert.False(poemContentImporter.HasYamlMetadata);
        Assert.Equal("Cathédrale", poem.DoubleAcrostiche!.First);
        Assert.Equal("de lumières", poem.DoubleAcrostiche.Second);
        poemContentImporter.VerifyAnomaliesAfterImport();
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    public void ShouldImportMultipleCategories()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            fixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "15_quinzieme_saison/du_gris_au_noir.md");
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var poem = poemContentImporter.Import(poemContentFilePath);
        Assert.True(poemContentImporter.HasTomlMetadata);
        Assert.False(poemContentImporter.HasYamlMetadata);
        Assert.Equal("Du gris au noir", poem.Acrostiche);
        Assert.Equal(2, poem.Categories.Count);
        Assert.Equal(1, poem.Categories.FirstOrDefault(x => x.Name == "Saisons").SubCategories.Count);
        Assert.Equal(2, poem.Categories.FirstOrDefault(x => x.Name == "Ombres et lumières").SubCategories.Count);
        Assert.Equal("Automne", poem.Categories.FirstOrDefault(x => x.Name == "Saisons").SubCategories.FirstOrDefault());
        Assert.Contains("Ville", poem.Categories.FirstOrDefault(x => x.Name == "Ombres et lumières").SubCategories);
        Assert.Contains("Crépuscule", poem.Categories.FirstOrDefault(x => x.Name == "Ombres et lumières").SubCategories);
        poemContentImporter.VerifyAnomaliesAfterImport();
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    public void ShouldImportLocations()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            fixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "12_douzieme_saison/pelerinage.md");
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var poem = poemContentImporter.Import(poemContentFilePath);
        Assert.True(poemContentImporter.HasTomlMetadata);
        Assert.False(poemContentImporter.HasYamlMetadata);
        Assert.Equal(["Reims"], poem.Locations);
        poemContentImporter.VerifyAnomaliesAfterImport();
    }

    [Theory]
    [Trait("UnitTest", "Computation")]
    [InlineData("categories = [\"First\", \"Here and there\"]")]
    [InlineData("categories = [ \"First\", \"Here and there\" ]")]
    public void ShouldProperlyParseCategories(string categoriesLine)
    {
        var processor = new PoemTomlMetadataProcessor();
        processor.BuildCategories(categoriesLine);
        Assert.Equal(new List<string> { "First", "Here and there" }, processor.GetCategories());
    }

    [Fact]
    [Trait("UnitTest", "Computation")]
    public void ShouldProperlyParseInfoWithQuotes()
    {
        var info = "info = \"It is a \\\"quoted text\\\"\"";
        var processor = new PoemTomlMetadataProcessor();
        var readInfo = processor.GetInfo(info);
        Assert.Equal("It is a \"quoted text\"", readInfo);
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    public void ShouldImportPictures()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            fixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "17_dix_septieme_saison/une_derniere_visite.md");
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var poem = poemContentImporter.Import(poemContentFilePath);
        Assert.True(poemContentImporter.HasTomlMetadata);
        Assert.False(poemContentImporter.HasYamlMetadata);
        Assert.NotNull(poem.Pictures);
        Assert.Equal(4, poem.Pictures.Count);
        Assert.Equal("Le puits du château de Ham-sous-Varsberg", poem.Pictures[0]);
        poemContentImporter.VerifyAnomaliesAfterImport();
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    public void ShouldImportVariableVerse()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            fixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "17_dix_septieme_saison/a_quai.md");
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var poem = poemContentImporter.Import(poemContentFilePath);
        Assert.True(poemContentImporter.HasTomlMetadata);
        Assert.False(poemContentImporter.HasYamlMetadata);
        Assert.Equal("Métrique variable : 5, 2", poem.Info);
        Assert.Equal("5, 2", poem.DetailedMetric);
        // Because it has been copied from DetailedVerseLength by poemContentImporter.
        Assert.Equal("5, 2", poem.VerseLength);
        poemContentImporter.VerifyAnomaliesAfterImport();
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    public void ShouldImportMultilineTags()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            fixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "21_vingt_et_unieme_saison/le_jour_decroit.md");
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var poem = poemContentImporter.Import(poemContentFilePath);
        Assert.True(poemContentImporter.HasTomlMetadata);
        Assert.False(poemContentImporter.HasYamlMetadata);
        Assert.Equal(new List<string> { "refrain", "les mois", "août" }, poem.ExtraTags);
        poemContentImporter.VerifyAnomaliesAfterImport();
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    public void ShouldImportEmptyInfo()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            fixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "22_vingt_deuxieme_saison/est_ce_un_automne.md");
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var poem = poemContentImporter.Import(poemContentFilePath);
        Assert.True(poemContentImporter.HasTomlMetadata);
        Assert.False(poemContentImporter.HasYamlMetadata);
        Assert.Null(poem.Info);
        poemContentImporter.VerifyAnomaliesAfterImport();
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    public void ShouldImportSingleLineInfo()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            fixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "12_douzieme_saison/barcarolle.md");
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var poem = poemContentImporter.Import(poemContentFilePath);
        Assert.True(poemContentImporter.HasTomlMetadata);
        Assert.False(poemContentImporter.HasYamlMetadata);
        Assert.Equal(1, poem.Categories.Count);
        Assert.Equal(1, poem.Categories.First().SubCategories.Count);
        Assert.Equal("Musique et chant", poem.Categories.First().SubCategories.First());
        Assert.Equal("Inspiré par l'air homonyme d'Offenbach.", poem.Info);
        poemContentImporter.VerifyAnomaliesAfterImport();
    }
    
    [Fact]
    [Trait("UnitTest", "ContentImport")]
    public void ShouldImportMultiLineInfo()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            fixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "19_dix_neuvieme_saison/je_te_promets_les_quatre_saisons.md");
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var poem = poemContentImporter.Import(poemContentFilePath);
        Assert.True(poemContentImporter.HasTomlMetadata);
        Assert.False(poemContentImporter.HasYamlMetadata);
        /*
        [Cette chanson de Johnny Hallyday](https://www.google.com/search?q=je+te+promets+johnny+hallyday) dans ma tête ce matin... Deux grands artistes qui me sont chers.

        Vers [les poèmes qui évoquent les quatre saisons](../../tags/saisons/_index#les-quatre-saisons-)
        */
        Assert.StartsWith($"[Cette chanson de Johnny Hallyday]", poem.Info);
        Assert.EndsWith("(../../tags/saisons/_index#les-quatre-saisons-)", poem.Info);
        Assert.Equal($"[Cette chanson de Johnny Hallyday](https://www.google.com/search?q=je+te+promets+johnny+hallyday) dans ma tête ce matin... Deux grands artistes qui me sont chers.{Environment.NewLine}{Environment.NewLine}Vers [les poèmes qui évoquent les quatre saisons](../../tags/saisons/_index#les-quatre-saisons-)", poem.Info);
        poemContentImporter.VerifyAnomaliesAfterImport();
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    public void ShouldImportSingleLineDescription()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            fixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "3_troisieme_saison/est_ce_un_automne.md");
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var poem = poemContentImporter.Import(poemContentFilePath);
        Assert.True(poemContentImporter.HasTomlMetadata);
        Assert.False(poemContentImporter.HasYamlMetadata);
        Assert.Equal("Est-ce un automne, est-ce un printemps / Qui dans mon cœur se renouvelle", poem.Description);
        poemContentImporter.VerifyAnomaliesAfterImport();
    }

    [Fact]
    [Trait("UnitTest", "ContentImport")]
    public void ShouldImportMultiLineDescriptionAndWordCloud()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            fixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "21_vingt_et_unieme_saison/juillet_aout.md");
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var poem = poemContentImporter.Import(poemContentFilePath);
        Assert.True(poemContentImporter.HasTomlMetadata);
        Assert.False(poemContentImporter.HasYamlMetadata);
        Assert.Equal($"Aux portes de juillet, c'est le mois des langueurs / [...] avant qu'août le sauvage", poem.Description);
        Assert.Equal($"Aux portes de juillet, c'est le mois des langueurs{Environment.NewLine}C'est le mois le plus pur avant qu'août le sauvage{Environment.NewLine}juillet tout neuf a comme un air fragile", poem.WordCloud);
        poemContentImporter.VerifyAnomaliesAfterImport();
    }
}