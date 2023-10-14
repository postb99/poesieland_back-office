using Toolbox.Domain;

namespace Toolbox;

public class PoemContentImporter
{
    private Poem _poem;
    private bool _isInMetadata;
    private IMetadataProcessor _metadataProcessor;

    private const string YamlMarker = "---";
    private const string TomlMarker = "+++";

    private Dictionary<string, List<string>> _tagsAndCategories;

    public Poem Import(string contentFilePath)
    {
        _poem = new Poem();
        _tagsAndCategories = new Dictionary<string, List<string>>();

        using var streamReader = new StreamReader(contentFilePath);
        string line;
        do
        {
            line = streamReader.ReadLine();
            ProcessLine(line);
        } while (line != null);

        return _poem;
    }

    private void ProcessLine(string? line)
    {
        if (line == null)
            return;

        if (line.StartsWith(TomlMarker))
        {
            _metadataProcessor = new TomlMetadataProcessor();
            _isInMetadata = !_isInMetadata;
        }
        else if (line.StartsWith(YamlMarker))
        {
            _metadataProcessor = new YamlMetadataProcessor();
            _isInMetadata = !_isInMetadata;
        }

        if (_isInMetadata)
            ProcessMetadataLine(line);
        else
            ProcessVerses(line);
    }

    private void ProcessMetadataLine(string line)
    {
        if (line.StartsWith("title"))
        {
            _poem.Title = _metadataProcessor.GetTitle(line);
        }
        else if (line.StartsWith("id"))
        {
            _poem.Id = _metadataProcessor.GetId(line);
        }
        else if (line.StartsWith("date"))
        {
            _poem.TextDate = _metadataProcessor.GetTextDate(line);
        }
        else if (line.StartsWith("categories"))
        {
            _metadataProcessor.BuildCategories(line);
        }
        else if (line.StartsWith("    - "))
        {
            _metadataProcessor.AddValue(line);
        }
        else if (line.StartsWith("info"))
        {
            _poem.Info = _metadataProcessor.GetInfo(line);
        }
        else if (line.StartsWith("acrostiche"))
        {
            _poem.Acrostiche = _metadataProcessor.GetAcrostiche(line);
        }
        else if (line.StartsWith("doubleAcrostiche"))
        {
            _poem.DoubleAcrostiche = _metadataProcessor.GetDoubleAcrostiche(line);
        }
        else if (line.StartsWith("verseLength"))
        {
            _poem.VerseLength = _metadataProcessor.GetVerseLength(line);
        }
        else if (line.StartsWith("type"))
        {
            _poem.PoemType = _metadataProcessor.GetType(line);
        }

        // TODO add parent to categories then assign to poem
        //_poem.Categories = _contentProcessor.GetCategories()
    }

    private void ProcessVerses(string line)
    {
        // TODO create a ContentProcessor.
    }
}