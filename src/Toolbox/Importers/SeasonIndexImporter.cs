using System.Text;
using Toolbox.Domain;
using Toolbox.Processors;

namespace Toolbox.Importers;

public class SeasonIndexImporter()
{
    private Season _season;
    private bool _isInMetadata;
    private bool _doneImportingDescription;
    private readonly SeasonIndexTomlMetadataProcessor _metadataProcessor = new();

    private const string YamlMarker = "---";
    private const string TomlMarker = "+++";

    private readonly List<string> _descriptionLines = [];

    /// <summary>
    /// Imports season metadata from a specified content file into a Season object.
    /// </summary>
    /// <param name="contentFilePath">The file path of the content to be imported.</param>
    /// <returns>An instance of the <see cref="Season"/> class populated with the imported metadata.</returns>
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

    /// <summary>
    /// Processes a single line of content, determining metadata, description, or other relevant data for a Season object.
    /// </summary>
    /// <param name="line">The line of content to be processed. Can be null, metadata, or description line.</param>
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

    /// <summary>
    /// Processes a single line of metadata from the content file and updates the corresponding properties of a Season object.
    /// </summary>
    /// <param name="line">The metadata line to be processed.</param>
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