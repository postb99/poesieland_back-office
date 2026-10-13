using Toolbox.Importers;
using Toolbox.Settings;
using Xunit;

namespace Tests.Processors;

public class PoemContentProcessorTest(BasicFixture fixture) : IClassFixture<BasicFixture>
{
    [Theory]
    [Trait("UnitTest", "ContentImport")]
    [InlineData("16_seizieme_saison/oiseaux_de_juillet.md", 3, 4)]
    [InlineData("16_seizieme_saison/sur_les_toits_la_pluie.md", 1, 18)]
    public void ShouldImportParagraphs(string poemContentPath, int paragraphs, int verses)
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            fixture.Configuration[Constants.CONTENT_ROOT_DIR]!, poemContentPath);
        var poemContentImporter = new PoemImporter(fixture.Configuration);
        var poem = poemContentImporter.Import(poemContentFilePath);
        Assert.Equal(paragraphs, poem.Paragraphs.Count);
        poem.Paragraphs.ForEach(p => Assert.Equal(verses, p.Verses.Count));
        poemContentImporter.VerifyAnomaliesAfterImport();
    }
}
