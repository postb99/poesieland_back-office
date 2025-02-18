using FluentAssertions;
using Toolbox;
using Toolbox.Settings;

namespace Tests;

public class ContentProcessorTest(BasicFixture basicFixture) : IClassFixture<BasicFixture>
{
    [Theory]
    [Trait("UnitTest", "ContentImport")]
    [InlineData("16_seizieme_saison/oiseaux_de_juillet.md", 3, 4)]
    [InlineData("16_seizieme_saison/sur_les_toits_la_pluie.md", 1, 18)]
    public void ShouldImportParagraphs(string poemContentPath, int paragraphs, int verses)
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            basicFixture.Configuration[Constants.CONTENT_ROOT_DIR]!, poemContentPath);
        var poemContentImporter = new PoemContentImporter();
        var (poem, position) = poemContentImporter.Import(poemContentFilePath, basicFixture.Configuration);
        poem.Paragraphs.Count.Should().Be(paragraphs);
        poem.Paragraphs.ForEach(p => p.Verses.Count.Should().Be(verses));
    }
}