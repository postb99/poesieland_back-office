using System.Text;
using Toolbox.Domain;

namespace Toolbox;

public class SeasonIndexImporter()
{
    private Season _season;
    private bool _isInMetadata;
    private bool _doneImportingDescription;
    private SeasonIndexTomlMetadataProcessor _metadataProcessor = new();

    public const string YamlMarker = "---";
    public const string TomlMarker = "+++";

    private List<string> _descriptionLines = [];

    public Season Import(string contentFilePath)
    {
        _season = new();
        _isInMetadata = false;
        _doneImportingDescription = false;

        using var streamReader = new StreamReader(contentFilePath);
        string line;
        do
        {
            line = streamReader.ReadLine();
            ProcessLine(line);
        } while (line is not null);

        var sb = new StringBuilder();
        foreach (var descriptionLine in _descriptionLines)
        {
            sb.Append(descriptionLine).Append(Environment.NewLine);
        }

        _season.Introduction = sb.ToString().TrimStart('\r').TrimStart('\n');
        while (_season.Introduction[^1] == '\n')
            _season.Introduction = _season.Introduction.TrimEnd('\n').TrimEnd('\r');
        return _season;
    }

    private void ProcessLine(string? line)
    {
        if (line == null || _doneImportingDescription)
            return;

        if (line.StartsWith(TomlMarker))
        {
            _isInMetadata = !_isInMetadata;
        }
        else if (line.StartsWith(YamlMarker))
        {
            _doneImportingDescription = true;
        }

        if (_isInMetadata)
            ProcessMetadataLine(line);
        else if (line != TomlMarker && !_doneImportingDescription)
        {
            _descriptionLines.Add(line);
        }
    }

    private void ProcessMetadataLine(string line)
    {
        if (line.StartsWith("title"))
        {
            var longTitleParts = _metadataProcessor.GetTitle(line).Split(':');
            _season.NumberedName = longTitleParts[0][..^8];
            _season.Name = longTitleParts[1].TrimStart();
        }
        else if (line.StartsWith("summary"))
        {
            _season.Summary = _metadataProcessor.GetSummary(line);
        }
        else if (line.StartsWith("weight"))
        {
            _season.Id = _metadataProcessor.GetWeight(line);
        }
    }
}