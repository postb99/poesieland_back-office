using System.Globalization;
using Toolbox.Domain;

namespace Toolbox;

public class YamlMetadataProcessor : IMetadataProcessor
{
    private enum IsProcessingList
    {
        Categories,
        Tags,
        Pictures
    }

    private IsProcessingList _isProcessingList;
    private readonly List<string> _categories = new();
    private readonly List<string> _tags = new();
    private readonly List<string> _pictures = new();

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
        var value = line?.Substring(6);
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
        _isProcessingList = IsProcessingList.Categories;
    }

    public void BuildTags()
    {
        _isProcessingList = IsProcessingList.Tags;
    }
    
    public void BuildPictures(string line)
    {
        _isProcessingList = IsProcessingList.Pictures;
    }

    public void AddValue(string line, int nbSpaces)
    {
        var lineValue = line.Substring(nbSpaces + 2);
        switch (_isProcessingList)
        {
            case IsProcessingList.Categories:
                _categories.Add(lineValue);
                break;
            case IsProcessingList.Tags:
                _tags.Add(lineValue.StartsWith("\"") ? lineValue.CleanedContent() : lineValue);
                break;
            case IsProcessingList.Pictures:
                _pictures.Add(lineValue);
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
}