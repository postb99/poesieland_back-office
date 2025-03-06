using Shouldly;
using Toolbox;
using Toolbox.Settings;
using Xunit;

namespace Tests;

public class PoemContentImporterTest(BasicFixture basicFixture): IClassFixture<BasicFixture>
{
    [Fact]
    [Trait("UnitTest", "ContentImport")]
    public void ShouldImportVariableVerseLength()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            basicFixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "3_troisieme_saison/jeux_de_nuits.md");
        var poemContentImporter = new PoemContentImporter();
        var (poem, _) = poemContentImporter.Import(poemContentFilePath, basicFixture.Configuration);
        poem.Info.ShouldBe("Métrique variable : 8, 6, 4, 2");
        poem.DetailedVerseLength.ShouldBe("8, 6, 4, 2");
        // Because it has been copied from DetailedVerseLength by poemContentImporter.
        poem.VerseLength.ShouldBe("8, 6, 4, 2");
    }
}