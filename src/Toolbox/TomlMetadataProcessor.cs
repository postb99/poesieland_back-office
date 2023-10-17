using System.Globalization;
using Toolbox.Domain;

namespace Toolbox;

public class TomlMetadataProcessor : IMetadataProcessor
{
    private List<string> _categories = new();
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
        return DateTime.ParseExact(line.Substring(7).CleanedContent(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None)
            .ToString("dd.MM.yyyy");
    }

    public string? GetInfo(string line)
    {
        return line.Substring(7).CleanedContent();
        ;
    }

    public string? GetAcrostiche(string line)
    {
        return line.Substring(13).CleanedContent();
    }

    public string GetVerseLength(string line)
    {
        return line.Substring(14);
    }

    public string? GetType(string line)
    {
        return line.Substring(7).CleanedContent();
    }

    public DoubleAcrostiche GetDoubleAcrostiche(string line)
    {
        var splitted = line.Substring(19).CleanedContent().Split('|');
        return new DoubleAcrostiche { First = splitted[0].Trim(), Second = splitted[1].Trim() };
    }

    public void BuildCategories(string line)
    {
        _categories = line.Substring(13).Trim('[').Trim(']').Split('"').Select(x => x.CleanedContent()).Where(x => x != null && x != ", ").ToList();
    }

    public void StopBuildCategories()
    {
    }

    public void AddValue(string line)
    {
        
    }

    public List<string> GetCategories()
    {
        return _categories;
    }
}