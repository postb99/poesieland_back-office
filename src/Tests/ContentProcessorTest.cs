using FluentAssertions;
using Toolbox;
using Toolbox.Settings;

namespace Tests;

public class ContentProcessorTest
{
    [Theory]
    [InlineData("16_seizieme_saison\\oiseaux_de_juillet.md", 3, 4)]
    [InlineData("16_seizieme_saison\\sur_les_toits_la_pluie_16.md", 1, 16)]
    public void ShouldImportParagraphs(string poemContentPath, int paragraphs, int verses)
    {
        var configuration = Helpers.GetConfiguration();
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            configuration[Constants.CONTENT_ROOT_DIR], poemContentPath);
        var poemContentImporter = new PoemContentImporter();
        var (poem, position) = poemContentImporter.Import(poemContentFilePath, configuration);
        poem.Paragraphs.Count.Should().Be(paragraphs);
        poem.Paragraphs.ForEach(p => p.Verses.Count.Should().Be(verses));
    }
}
