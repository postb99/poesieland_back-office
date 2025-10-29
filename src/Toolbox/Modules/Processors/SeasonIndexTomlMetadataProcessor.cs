using Toolbox.Domain;

namespace Toolbox.Modules.Processors;

public class SeasonIndexTomlMetadataProcessor
{
    public string GetTitle(string line)
    {
        return line.Substring(8).CleanedContent()!;
    }
    
    public string GetSummary(string line)
    {
        return line.Substring(10).CleanedContent()!;
    }
    
    public int GetWeight(string line)
    {
        return int.Parse(line.Substring(9));
    }
}