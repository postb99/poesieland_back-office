using System.Xml.Serialization;

namespace Toolbox.Domain;

public class Category
{
    [XmlElement("c")] public string Name { get; set; } = string.Empty;

    [XmlElement("sous-cat")] public List<string> SubCategories { get; set; } = [];
}