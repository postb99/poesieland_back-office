namespace Toolbox.Processors;

public class SeasonIndexTomlMetadataProcessor
{
    public MultilineMetadataProcessingType MultilineMetadataProcessingType { get; private set; }
    public List<string> DescriptionLines { get; } = [];
    
    private int NumberOfCharsToIgnore(string metadataFieldName) => metadataFieldName.Length + 3;
    
    private string RemoveTrailingDelimiter(string lineValue) => lineValue.Substring(0, lineValue.Length - 3);
    
    public string GetTitle(string line)
    {
        return line.Substring(NumberOfCharsToIgnore(MetadataConstants.TITLE)).CleanedContent()!;
    }
    
    public int GetWeight(string line)
    {
        return int.Parse(line.Substring(NumberOfCharsToIgnore(MetadataConstants.WEIGHT)));
    }

    private string? GetDescription(string line)
    {
        return line.Substring(NumberOfCharsToIgnore(MetadataConstants.DESCRIPTION)).CleanedContent();
    }
    
    public void BuildDescriptionLines(string line)
    {
        MultilineMetadataProcessingType = MultilineMetadataProcessingType.InfoLines;
        var inlineInfo = GetDescription(line);
        if (inlineInfo != null && inlineInfo != MetadataConstants.DOUBLE_QUOTE)
        {
            AddValue(inlineInfo, -2);
            MultilineMetadataProcessingType = MultilineMetadataProcessingType.None;
        }
    }

    public void AddValue(string line, int nbSpaces)
    {
        var lineValue = line == "" ? line : line.Substring(nbSpaces + 2);
        switch (MultilineMetadataProcessingType)
        {
            case MultilineMetadataProcessingType.InfoLines:

                if (lineValue.EndsWith(MetadataConstants.TRIPLE_DOUBLE_QUOTE))
                {
                    // Encountered """ end marker
                    DescriptionLines.Add(RemoveTrailingDelimiter(lineValue));
                    MultilineMetadataProcessingType = MultilineMetadataProcessingType.None;
                }
                else
                {
                    DescriptionLines.Add(lineValue);
                }
                break;
        }
    }
}