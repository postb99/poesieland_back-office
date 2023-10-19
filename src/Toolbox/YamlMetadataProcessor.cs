using System.Globalization;
using Toolbox.Domain;

namespace Toolbox;

public class YamlMetadataProcessor : IMetadataProcessor
{
    private enum IsProcessingList
    {
        Categories,
        Tags
    }

    private IsProcessingList _isProcessingList;
    private readonly List<string> _categories = new();
    private readonly List<string> _tags = new();

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
        return line?.Substring(6).CleanedContent();
    }

    public string? GetType(string line)
    {
        return line?.Substring(6).CleanedContent();
    }

    public string? GetAcrostiche(string line)
    {
        return line?.Substring(12).CleanedContent();
    }

    public string GetVerseLength(string line)
    {
        return line.Substring(13);
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
        _isProcessingList = IsProcessingList.Categories;
    }

    public void BuildTags()
    {
        _isProcessingList = IsProcessingList.Tags;
    }

    public void AddValue(string line, int nbSpaces)
    {
        switch (_isProcessingList)
        {
            case IsProcessingList.Categories:
                _categories.Add(line.Substring(nbSpaces + 2));
                break;
            case IsProcessingList.Tags:
                _tags.Add(line.Substring(nbSpaces + 2).CleanedContent());
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
}