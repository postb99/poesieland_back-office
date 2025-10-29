using System.Globalization;
using Toolbox.Domain;

namespace Toolbox.Modules.Processors;

public class PoemTomlMetadataProcessor : IPoemMetadataProcessor
{
    public MultilineMetadataProcessingType MultilineMetadataProcessingType { get; private set; }

    private List<string> _categories = [];
    private List<string> _tags = [];
    private List<string> _pictures = [];
    private List<string> _infoLines = [];
    private List<string> _locations = [];

    public string GetTitle(string line)
    {
        return line.Substring(8).CleanedContent()!;
    }

    public string GetId(string line)
    {
        return line.Substring(5).CleanedContent()!;
    }

    public string GetTextDate(string line)
    {
        return DateTime.ParseExact(line.Substring(7), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None)
            .ToString("dd.MM.yyyy");
    }

    public string? GetInfo(string line)
    {
        return line.Substring(7).CleanedContent();
    }

    public string? GetAcrostiche(string line)
    {
        return line.Substring(13).CleanedContent();
    }

    public string GetVerseLength(string line)
    {
        return line.Substring(14);
    }

    public int GetWeight(string line)
    {
        return int.Parse(line.Substring(9));
    }

    public string? GetType(string line)
    {
        return line.Substring(11).CleanedContent();
    }

    public DoubleAcrostiche GetDoubleAcrostiche(string line)
    {
        var splitted = line.Substring(19).CleanedContent().Split('|');
        return new() { First = splitted[0].Trim(), Second = splitted[1].Trim() };
    }

    public void BuildCategories(string line)
    {
        _categories = line.Substring(13).Trim('[').Trim(']').Trim(' ').Split('"').Where(x => x != string.Empty && x != ", ")
            .ToList();
    }

    public void BuildTags(string line)
    {
        switch (line)
        {
            case "tags = [":
                MultilineMetadataProcessingType = MultilineMetadataProcessingType.Tags;
                break;
            default:
                _tags = line.Substring(7).Trim('[').Trim(']').Trim(' ').Split('"')
                    .Where(x => x != string.Empty && x != ", ")
                    .ToList();
                break;
        }
    }

    public void BuildPictures(string line)
    {
        // Always single-line
        _pictures = line.Substring(11).Trim('[').Trim(']').Trim(' ').Split('"')
            .Where(x => x != "" && x != ", ")
            .ToList();
    }

    public void BuildInfoLines(string line)
    {
        MultilineMetadataProcessingType = MultilineMetadataProcessingType.InfoLines;
        var inlineInfo = GetInfo(line);
        if (inlineInfo != null && inlineInfo != "\"")
        {
            AddValue(inlineInfo, -2);
            MultilineMetadataProcessingType = MultilineMetadataProcessingType.None;
        }
    }
    
    public void BuildLocations(string line)
    {
        _locations = line.Substring(12).Trim('[').Trim(']').Trim(' ').Split('"').Where(x => x != string.Empty && x != ", ")
            .ToList();
    }

    public void AddValue(string line, int nbSpaces)
    {
        var lineValue = line == "" ? line : line.Substring(nbSpaces + 2);
        switch (MultilineMetadataProcessingType)
        {
            case MultilineMetadataProcessingType.InfoLines:

                if (lineValue.EndsWith("\"\"\""))
                {
                    // Encountered """ end marker
                    _infoLines.Add(lineValue.Substring(0, lineValue.Length - 3));
                    MultilineMetadataProcessingType = MultilineMetadataProcessingType.None;
                }
                else
                {
                    _infoLines.Add(lineValue);
                }

                break;
            case MultilineMetadataProcessingType.Tags:
                if (lineValue == "]")
                {
                    // Encountered ] end marker
                    MultilineMetadataProcessingType = MultilineMetadataProcessingType.None;
                }
                else
                {
                    _tags.Add(lineValue.TrimStart(' ').TrimEnd(',').Trim('"'));
                }

                break;
        }
    }

    public List<string> GetCategories()
    {
        return _categories;
    }

    public List<string> GetTags()
    {
        return _tags;
    }

    public List<string> GetPictures()
    {
        return _pictures;
    }

    public List<string> GetInfoLines()
    {
        return _infoLines;
    }
    
    public List<string> GetLocations()
    {
        return _locations;
    }
}