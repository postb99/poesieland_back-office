using System.Globalization;
using Toolbox.Domain;

namespace Toolbox;

public class YamlMetadataProcessor : IMetadataProcessor
{
    public ProcessingListType ProcessingListType { get; private set; }
    private readonly List<string> _categories = [];
    private readonly List<string> _tags = [];
    private readonly List<string> _pictures = [];
    private readonly List<string> _infoLines = [];
    private readonly List<string> _locations = [];

    public string GetTitle(string line)
    {
        return line.Substring(7);
    }

    public string GetId(string line)
    {
        return line.Substring(4);
    }

    public string GetTextDate(string line)
    {
        return DateTime.ParseExact(line.Substring(6), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None)
            .ToString("dd.MM.yyyy");
    }

    public string? GetInfo(string line)
    {
        var value = line?.Substring(6).Trim('"');
        return string.IsNullOrEmpty(value) ? null : value;
    }

    public string? GetPicture(string line)
    {
        var value = line?.Substring(9).Trim('"');
        return string.IsNullOrEmpty(value) ? null : value;
    }

    public string? GetType(string line)
    {
        var value = line?.Substring(10);
        return value == "\"\"" ? null : value;
    }

    public string? GetAcrostiche(string line)
    {
        var value = line?.Substring(12);
        return value == "\"\"" ? null : value;
    }

    public string GetVerseLength(string line)
    {
        var value = line.Substring(13);
        return value == "\"\"" ? null : value;
    }

    public int GetWeight(string line)
    {
        return int.Parse(line.Substring(8));
    }

    public DoubleAcrostiche GetDoubleAcrostiche(string line)
    {
        var splitted = line?.Substring(18).Split('|');
        return splitted.Length < 2
            ? null
            : new DoubleAcrostiche { First = splitted[0].Trim(), Second = splitted[1].Trim() };
    }

    public void BuildCategories(string line)
    {
        ProcessingListType = ProcessingListType.Categories;
    }

    public void BuildTags(string line)
    {
        ProcessingListType = ProcessingListType.Tags;
    }

    public void BuildInfoLines(string line)
    {
        ProcessingListType = ProcessingListType.InfoLines;
        var inlineInfo = GetInfo(line);
        if (inlineInfo != null && inlineInfo != "|-")
        {
            _infoLines.Add(inlineInfo);
            ProcessingListType = ProcessingListType.None;
        }
    }

    public void BuildPictures(string line)
    {
        ProcessingListType = ProcessingListType.Pictures;
    }
    
    public void BuildLocations(string line)
    {
        ProcessingListType = ProcessingListType.Locations;
    }

    public void AddValue(string line, int nbSpaces)
    {
        if (nbSpaces == -2 && line.Length > 0 && line[0] != ' ')
        { 
            // A value in YAML cannot start at beginning of line so ignore lines not starting with at least a space
            return;
        }
        var lineValue = line == "" ? line : line.Substring(nbSpaces + 2);
        switch (ProcessingListType)
        {
            case ProcessingListType.Categories:
                _categories.Add(lineValue);
                break;
            case ProcessingListType.Tags:
                _tags.Add(lineValue.StartsWith("\"") ? lineValue.CleanedContent() : lineValue);
                break;
            case ProcessingListType.Pictures:
                _pictures.Add(lineValue);
                break;
            case ProcessingListType.InfoLines:
                _infoLines.Add(lineValue.TrimStart(' '));
                break;
            case ProcessingListType.Locations:
                _locations.Add(lineValue.StartsWith("\"") ? lineValue.CleanedContent() : lineValue);
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