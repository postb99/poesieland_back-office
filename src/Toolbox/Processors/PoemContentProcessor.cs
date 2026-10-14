using Toolbox.Domain;

namespace Toolbox.Processors;

public class PoemContentProcessor
{
    private List<Paragraph> _paragraphs = [];
    private bool _isNewParagraph = true;
    private bool _done;

    public void AddLine(string line)
    {
        if (_done)
        {
            return;
        }

        if (line == MetadataConstants.YAML_MARKER || line == MetadataConstants.TOML_MARKER)
        {
            return;
        }

        if (line.StartsWith(MetadataConstants.CALLOUT_START)
            || line.StartsWith(MetadataConstants.FM_SNIPPET_START)
            || line.StartsWith(MetadataConstants.MARKDOWN_IMAGE_START)
            || line.StartsWith(MetadataConstants.HUGO_SHORTCODE_START)
            || line.StartsWith(MetadataConstants.FOOTNOTE_REFERENCE_START))
        {
            _done = true;
            return;
        }
        
        if (line.Trim() == MetadataConstants.PARAGRAPH_BREAK_MARKER)
        {
            _isNewParagraph = true;
            return;
        }

        if (_isNewParagraph)
        {
            var paragraph = new Paragraph { Verses = [] };
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