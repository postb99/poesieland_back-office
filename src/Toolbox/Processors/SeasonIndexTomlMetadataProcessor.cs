using Toolbox.Domain;

namespace Toolbox.Processors;

public class SeasonIndexTomlMetadataProcessor
{
    public MultilineMetadataProcessingType MultilineMetadataProcessingType { get; private set; }
    public List<string> DescriptionLines { get; private set; } = [];
    
    public string GetTitle(string line)
    {
        return line.Substring(8).CleanedContent()!;
    }
    
    public int GetWeight(string line)
    {
        return int.Parse(line.Substring(9));
    }
    
    public void BuildDescriptionLines(string line)
    {
        MultilineMetadataProcessingType = MultilineMetadataProcessingType.InfoLines;
        var inlineInfo = GetDescription(line);
        if (inlineInfo != null && inlineInfo != "\"")
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

                if (lineValue.EndsWith("\"\"\""))
                {
                    // Encountered """ end marker
                    DescriptionLines.Add(lineValue.Substring(0, lineValue.Length - 3));
                    MultilineMetadataProcessingType = MultilineMetadataProcessingType.None;
                }
                else
                {
                    DescriptionLines.Add(lineValue);
                }

                break;
        }
    }

    public string? GetDescription(string line)
    {
        return line.Substring(14).CleanedContent();
    }
}