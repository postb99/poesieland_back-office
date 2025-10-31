using Toolbox.Domain;
using Toolbox.Importers;

namespace Toolbox.Processors;

public class PoemContentProcessor
{
    private List<Paragraph> _paragraphs = new();
    private bool _isNewParagraph = true;
    private bool _done;

    public void AddLine(string line)
    {
        if (_done)
        {
            return;
        }

        if (line == PoemImporter.YamlMarker || line == PoemImporter.TomlMarker)
        {
            return;
        }

        if (line.StartsWith("{{% notice") || line.StartsWith("<!-- FM:Snippet:") || line.StartsWith("![") || line.StartsWith("{{<") || line.StartsWith("[^"))
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
            var paragraph = new Paragraph { Verses = new() };
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