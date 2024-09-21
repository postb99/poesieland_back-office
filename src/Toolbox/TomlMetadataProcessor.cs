using System.Globalization;
using Toolbox.Domain;

namespace Toolbox;

public class TomlMetadataProcessor : IMetadataProcessor
{
    public ProcessingListType ProcessingListType { get; private set; }

    private List<string> _categories = new();
    private List<string> _pictures = new();
    private List<string> _infoLines = new();

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
        return line.Substring(7).CleanedContent();
    }

    public DoubleAcrostiche GetDoubleAcrostiche(string line)
    {
        var splitted = line.Substring(19).CleanedContent().Split('|');
        return new DoubleAcrostiche { First = splitted[0].Trim(), Second = splitted[1].Trim() };
    }

    public void BuildCategories(string line)
    {
        _categories = line.Substring(13).Trim('[').Trim(']').Trim(' ').Split('"').Where(x => x != "" && x != ", ")
            .ToList();
    }

    public void BuildTags()
    {
        // Nothing to implement
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
        ProcessingListType = ProcessingListType.InfoLines;
        var inlineInfo = GetInfo(line);
        if (inlineInfo != null && inlineInfo != "\"")
        {
            AddValue(inlineInfo, -2);
            ProcessingListType = ProcessingListType.None;
        }
    }

    public void AddValue(string line, int nbSpaces)
    {
        var lineValue = line == "" ? line : line.Substring(nbSpaces + 2);
        switch (ProcessingListType)
        {
            case ProcessingListType.InfoLines:

                if (lineValue.EndsWith("\"\"\""))
                {
                    // Encountered """ end marker
                    _infoLines.Add(lineValue.Substring(0, lineValue.Length - 3));
                    ProcessingListType = ProcessingListType.None;
                }
                else
                {
                    _infoLines.Add(lineValue);
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
        return new List<string>();
    }

    public List<string> GetPictures()
    {
        return _pictures;
    }

    public List<string> GetInfoLines()
    {
        return _infoLines;
    }
}