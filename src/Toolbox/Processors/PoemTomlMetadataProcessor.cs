using System.Globalization;
using Toolbox.Domain;

namespace Toolbox.Processors;

public class PoemTomlMetadataProcessor : IPoemMetadataProcessor
{
    public MultilineMetadataProcessingType MultilineMetadataProcessingType { get; private set; }

    private List<string> _categories = [];
    private List<string> _tags = [];
    private List<string> _pictures = [];
    private List<string> _infoLines = [];
    private List<string> _descriptionLines = [];
    private List<string> _locations = [];
    private List<string> _wordCloudLines = [];

    private int NumberOfCharsToIgnore(string metadataFieldName) => metadataFieldName.Length + 3;
    
    private string ToArrayDeclaration(string metadataFieldName) => $"{metadataFieldName} = [";
    
    private string RemoveTrailingDelimiter(string lineValue) => lineValue.Substring(0, lineValue.Length - 3);

    private string[] ExtractArrayValues(string lineValue) => lineValue.Trim('[').Trim(']').Trim(' ').Split('"');

    public string GetTitle(string line)
    {
        return line.Substring(NumberOfCharsToIgnore(MetadataConstants.TITLE)).CleanedContent()!;
    }

    public string GetId(string line)
    {
        return line.Substring(NumberOfCharsToIgnore(MetadataConstants.ID)).CleanedContent()!;
    }

    public string GetTextDate(string line)
    {
        return DateTime.ParseExact(line.Substring(NumberOfCharsToIgnore(MetadataConstants.DATE)), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None)
            .ToString("dd.MM.yyyy");
    }

    public string? GetInfo(string line)
    {
        return line.Substring(NumberOfCharsToIgnore(MetadataConstants.INFO)).CleanedContent();
    }

    public string? GetDescription(string line)
    {
        return line.Substring(NumberOfCharsToIgnore(MetadataConstants.DESCRIPTION)).CleanedContent();
    }
    
    public string? GetWordCloud(string line)
    {
        return line.Substring(NumberOfCharsToIgnore(MetadataConstants.WORDCLOUD)).CleanedContent();
    }

    public string? GetAcrostiche(string line)
    {
        return line.Substring(NumberOfCharsToIgnore(MetadataConstants.ACROSTICHE)).CleanedContent();
    }

    public string GetVerseLength(string line)
    {
        return line.Substring(NumberOfCharsToIgnore(MetadataConstants.VERSE_LENGTH));
    }

    public int GetWeight(string line)
    {
        return int.Parse(line.Substring(NumberOfCharsToIgnore(MetadataConstants.WEIGHT)));
    }

    public string? GetType(string line)
    {
        return line.Substring(NumberOfCharsToIgnore(MetadataConstants.POEM_TYPE)).CleanedContent();
    }

    public DoubleAcrostiche GetDoubleAcrostiche(string line)
    {
        var splitted = line.Substring(NumberOfCharsToIgnore(MetadataConstants.DOUBLE_ACROSTICHE)).CleanedContent().Split('|');
        return new() { First = splitted[0].Trim(), Second = splitted[1].Trim() };
    }

    public void BuildCategories(string line)
    {
        _categories =
        [
            .. ExtractArrayValues(line.Substring(NumberOfCharsToIgnore(MetadataConstants.CATEGORIES)))
                .Where(x => x != string.Empty && x != ", ")
        ];
    }

    public void BuildTags(string line)
    {
        if (line == ToArrayDeclaration(MetadataConstants.TAGS))
            MultilineMetadataProcessingType = MultilineMetadataProcessingType.Tags;
        else
            _tags =
            [
                .. ExtractArrayValues(line.Substring(NumberOfCharsToIgnore(MetadataConstants.TAGS)))
                    .Where(x => x != string.Empty && x != ", ")
            ];
    }

    public void BuildPictures(string line)
    {
        if (line == ToArrayDeclaration(MetadataConstants.PICTURES))
            MultilineMetadataProcessingType = MultilineMetadataProcessingType.Pictures;
        else
            _pictures =
            [
                .. ExtractArrayValues(line.Substring(NumberOfCharsToIgnore(MetadataConstants.PICTURES)))
                    .Where(x => x != string.Empty && x != ", ")
            ];
    }

    public void BuildInfoLines(string line)
    {
        MultilineMetadataProcessingType = MultilineMetadataProcessingType.InfoLines;
        var inlineInfo = GetInfo(line);
        ProcessInlineValue(inlineInfo);
    }

    public void BuildDescriptionLines(string line)
    {
        MultilineMetadataProcessingType = MultilineMetadataProcessingType.DescriptionLines;
        var inlineDescription = GetDescription(line);
        ProcessInlineValue(inlineDescription);
    }
    
    public void BuildWordCloudLines(string line)
    {
        MultilineMetadataProcessingType = MultilineMetadataProcessingType.WordCloudLines;
        var inlineWordCloud = GetWordCloud(line);
        ProcessInlineValue(inlineWordCloud);
    }

    public void BuildLocations(string line)
    {
        _locations =
        [
            .. ExtractArrayValues(line.Substring(NumberOfCharsToIgnore(MetadataConstants.LOCATIONS)))
                .Where(x => x != string.Empty && x != ", ")
        ];
    }

    public void AddValue(string line, int nbSpaces)
    {
        var lineValue = line == "" ? line : line.Substring(nbSpaces + 2);
        switch (MultilineMetadataProcessingType)
        {
            case MultilineMetadataProcessingType.None :
                break;
            case MultilineMetadataProcessingType.InfoLines:
                if (lineValue.EndsWith(MetadataConstants.TRIPLE_DOUBLE_QUOTE))
                {
                    // Encountered """ end marker
                    _infoLines.Add(RemoveTrailingDelimiter(lineValue));
                    MultilineMetadataProcessingType = MultilineMetadataProcessingType.None;
                }
                else
                {
                    _infoLines.Add(lineValue);
                }
                break;
            case MultilineMetadataProcessingType.Tags:
                if (lineValue == MetadataConstants.CLOSING_BRACKET)
                {
                    // Encountered ] end marker
                    MultilineMetadataProcessingType = MultilineMetadataProcessingType.None;
                }
                else
                {
                    _tags.Add(lineValue.TrimStart(' ').TrimEnd(',').Trim('"'));
                }
                break;
            case MultilineMetadataProcessingType.DescriptionLines:
                if (lineValue.EndsWith(MetadataConstants.TRIPLE_DOUBLE_QUOTE))
                {
                    // Encountered """ end marker
                    _descriptionLines.Add(RemoveTrailingDelimiter(lineValue));
                    MultilineMetadataProcessingType = MultilineMetadataProcessingType.None;
                }
                else
                {
                    _descriptionLines.Add(lineValue);
                }
                break;
            case MultilineMetadataProcessingType.WordCloudLines:
                if (lineValue.EndsWith(MetadataConstants.TRIPLE_DOUBLE_QUOTE))
                {
                    // Encountered """ end marker
                    _wordCloudLines.Add(RemoveTrailingDelimiter(lineValue));
                    MultilineMetadataProcessingType = MultilineMetadataProcessingType.None;
                }
                else
                {
                    _wordCloudLines.Add(lineValue);
                }
                break;
            case MultilineMetadataProcessingType.Pictures:
                if (lineValue == MetadataConstants.CLOSING_BRACKET)
                {
                    // Encountered ] end marker
                    MultilineMetadataProcessingType = MultilineMetadataProcessingType.None;
                }
                else
                {
                    _pictures.Add(lineValue.TrimStart(' ').TrimEnd(',').Trim('"'));
                }
                break;
            default :
                throw new InvalidOperationException("AddValue not implemented for type " + MultilineMetadataProcessingType);
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
    
    public List<string> GetDescriptionLines()
    {
        return _descriptionLines;
    }
    
    public List<string> GetWordCloudLines()
    {
        return _wordCloudLines;
    }

    public List<string> GetLocations()
    {
        return _locations;
    }
    
    private void ProcessInlineValue(string? inlineValue)
    {
        if (inlineValue is null)
        {
            MultilineMetadataProcessingType = MultilineMetadataProcessingType.None;
        }
        else if (inlineValue != MetadataConstants.DOUBLE_QUOTE)
        {
            AddValue(inlineValue, -2);
            MultilineMetadataProcessingType = MultilineMetadataProcessingType.None;
        }
    }
}