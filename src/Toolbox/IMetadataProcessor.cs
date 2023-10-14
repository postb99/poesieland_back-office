using Toolbox.Domain;

namespace Toolbox;

public interface IMetadataProcessor
{
    string GetTitle(string line);
    string GetId(string line);
    string GetTextDate(string line);
    string? GetInfo(string line);
    string? GetAcrostiche(string line);
    string? GetVerseLength(string line);
    string? GetType(string line);
    DoubleAcrostiche GetDoubleAcrostiche(string line);
    void BuildCategories(string line);
    /// <summary>
    /// When YAML, add a value to a list of value determined by current context.
    /// </summary>
    /// <param name="line"></param>
    void AddValue(string line);   
    List<string> GetCategories();
}