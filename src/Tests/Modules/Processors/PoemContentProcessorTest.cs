using Shouldly;
using Toolbox.Modules.Importers;
using Toolbox.Settings;
using Xunit;

namespace Tests.Modules.Processors;

public class PoemContentProcessorTest(BasicFixture basicFixture) : IClassFixture<BasicFixture>
{
    [Theory]
    [Trait("UnitTest", "ContentImport")]
    [InlineData("16_seizieme_saison/oiseaux_de_juillet.md", 3, 4)]
    [InlineData("16_seizieme_saison/sur_les_toits_la_pluie.md", 1, 18)]
    public void ShouldImportParagraphs(string poemContentPath, int paragraphs, int verses)
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            basicFixture.Configuration[Constants.CONTENT_ROOT_DIR]!, poemContentPath);
        var poemContentImporter = new PoemImporter(basicFixture.Configuration);
        var (poem, _) = poemContentImporter.Import(poemContentFilePath);
        poem.Paragraphs.Count.ShouldBe(paragraphs);
        poem.Paragraphs.ForEach(p => p.Verses.Count.ShouldBe(verses));
        var anomalies = poemContentImporter.CheckAnomaliesAfterImport();
        anomalies.ShouldBeEmpty();
    }
}