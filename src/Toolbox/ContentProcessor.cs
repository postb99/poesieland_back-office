using Toolbox.Domain;

namespace Toolbox;

public class ContentProcessor
{
    private List<Paragraph> _paragraphs = new();
    private bool _isNewParagraph = true;
    private bool _done = false;

    public void AddLine(string line)
    {
        if (_done)
        {
            return;
        }

        if (line == PoemContentImporter.YamlMarker || line == PoemContentImporter.TomlMarker)
        {
            return;
        }

        if (line.StartsWith("{{% notice") || line.StartsWith("<!-- FM:Snippet:") || line.StartsWith("![") || line.StartsWith("{{<"))
        {
            _done = true;
            return;
        }
        
        if (line.Trim() == "\\")
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

    /// <summary>
    /// Getting the paragraphs resets the _done internal state so that the processor is ready again.
    /// </summary>
    public List<Paragraph> Paragraphs
    {
        get
        {
            _done = false;
            return _paragraphs;
        }
    }
}