using System.Xml.Serialization;

namespace Toolbox.Xml;

public class DoubleAcrostiche
{
    [XmlAttribute("premier")]
    public string First { get; set; }
    [XmlAttribute("deuxieme")]
    public string Second { get; set; }
}