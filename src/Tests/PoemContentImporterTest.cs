using FluentAssertions;
using Toolbox;
using Toolbox.Settings;

namespace Tests;

public class PoemContentImporterTest(BasicFixture basicFixture): IClassFixture<BasicFixture>
{
    [Fact]
    [Trait("UnitTest", "ContentImport")]
    public void ShouldImportVariableVerseLength()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            basicFixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "3_troisieme_saison\\jeux_de_nuits.md");
        var poemContentImporter = new PoemContentImporter();
        var (poem, position) = poemContentImporter.Import(poemContentFilePath, basicFixture.Configuration);
        poem.Info.Should().Be("Vers variable : 8, 6, 4, 2");
        poem.DetailedVerseLength.Should().Be("8, 6, 4, 2");
        // Because it has been copied from DetailedVerseLength by poemContentImporter.
        poem.VerseLength.Should().Be("8, 6, 4, 2");
    }
}