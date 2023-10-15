using System.Globalization;
using Toolbox.Domain;

namespace Toolbox;

public class YamlMetadataProcessor : IMetadataProcessor
{
    private bool _isProcessingCategories;
    private readonly List<string> _categories = new();

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
        return line.Substring(6);
    }
    
    public string? GetType(string line)
    {
        return line.Substring(5);
    }

    public string? GetAcrostiche(string line)
    {
        return line.Substring(12).CleanedContent();
    }

    public string GetVerseLength(string line)
    {
        return line.Substring(13);
    }

    public DoubleAcrostiche GetDoubleAcrostiche(string line)
    {
        var splitted = line.Substring(18).Split('|');
        return new DoubleAcrostiche { First = splitted[0].Trim(), Second = splitted[1].Trim() };
    }

    public void BuildCategories(string line)
    {
        _isProcessingCategories = true;
    }

    public void AddValue(string line)
    {
        if (_isProcessingCategories)
        {
            _categories.Add(line.Substring(6));
        }
    }

    public List<string> GetCategories()
    {
        return _categories;
    }
}