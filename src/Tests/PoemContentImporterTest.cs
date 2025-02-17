using FluentAssertions;
using Toolbox;
using Toolbox.Settings;

namespace Tests;

public class PoemContentImporterTest
{
    [Fact]
    [Trait("UnitTest", "ContentImport")]
    public void ShouldImportVariableVerseLength()
    {
        var configuration = Helpers.GetConfiguration();
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            configuration[Constants.CONTENT_ROOT_DIR]!, "3_troisieme_saison\\jeux_de_nuits.md");
        var poemContentImporter = new PoemContentImporter();
        var (poem, position) = poemContentImporter.Import(poemContentFilePath, configuration);
        poem.Info.Should().Be("Vers variable : 8, 6, 4, 2");
        poem.DetailedVerseLength.Should().Be("8, 6, 4, 2");
        // Because it has been copied from DetailedVerseLength by poemContentImporter.
        poem.VerseLength.Should().Be("8, 6, 4, 2");
    }
}