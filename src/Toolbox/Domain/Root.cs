using System.Xml.Serialization;

namespace Toolbox.Domain;

[XmlRoot("Saisons", Namespace = XML_NAMESPACE)]
public class Root
{
    public const string XML_NAMESPACE = "https://github.com/postb99/poesieland/ns";

    [XmlElement("Saison")] public List<Season> Seasons { get; set; } = [];
}