using Toolbox.Domain;

namespace Toolbox;

public class ContentProcessor
{
    private List<Paragraph> _paragraphs = new();
    private bool _isNewParagraph = true;

    public void AddLine(string line)
    {
        if (line == PoemContentImporter.YamlMarker || line == PoemContentImporter.TomlMarker)
        {
            return;
        }

        if (line == " \\")
        {
            _isNewParagraph = true;
            return;
        }

        if (_isNewParagraph)
        {
            var paragraph = new Paragraph { Verses = new List<string>() };
            _paragraphs.Add(paragraph);
            _isNewParagraph = false;
        }

        if (!string.IsNullOrWhiteSpace(line))
        {
            _paragraphs.Last().Verses.Add(line);
        }
    }

    public List<Paragraph> Paragraphs => _paragraphs;
}