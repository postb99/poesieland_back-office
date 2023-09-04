using System.Xml.Serialization;

namespace Toolbox.Xml;

public class CrossingAcrostiche
{
    [XmlAttribute("premier")]
    public string First { get; set; }
    [XmlAttribute("deuxieme")]
    public string Second { get; set; }
}