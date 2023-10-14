using System.Globalization;
using Toolbox.Domain;

namespace Toolbox;

public class TomlMetadataProcessor : IMetadataProcessor
{
    private List<string> _categories = new();
    public string GetTitle(string line)
    {
        return line.Substring(8).ContentCleanString()!;
    }

    public string GetId(string line)
    {
        return line.Substring(5).ContentCleanString()!;
    }

    public string GetTextDate(string line)
    {
        return DateTime.ParseExact(line.Substring(7), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None)
            .ToString("dd.MM.yyyy");
    }

    public string? GetInfo(string line)
    {
        return line.Substring(7);
    }

    public string? GetAcrostiche(string line)
    {
        return line.Substring(13).ContentCleanString();
    }

    public string GetVerseLength(string line)
    {
        return line.Substring(14);
    }

    public string? GetType(string line)
    {
        return line.Substring(7);
    }

    public DoubleAcrostiche GetDoubleAcrostiche(string line)
    {
        var splitted = line.Substring(19).Split('|');
        return new DoubleAcrostiche { First = splitted[0].Trim(), Second = splitted[1].Trim() };
    }

    public void BuildCategories(string line)
    {
        _categories = line.Substring(13).Trim('[').Trim(']').Split(',').Select(x => x.ContentCleanString()).ToList();
    }

    public void AddValue(string line)
    {
        
    }

    public List<string> GetCategories()
    {
        return _categories;
    }
}