using System.Xml.Serialization;

namespace Toolbox.Domain;

public class DoubleAcrostiche
{
    [XmlAttribute("premier")]
    public string First { get; set; }
    [XmlAttribute("deuxieme")]
    public string Second { get; set; }
}