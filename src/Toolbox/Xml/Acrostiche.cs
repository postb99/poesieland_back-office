using System.Xml.Serialization;

namespace Toolbox.Xml;

public class Acrostiche
{
    public string Content { get; set; }
    [XmlElement("premier")]
    public string First { get; set; }
    [XmlElement("deuxieme")]
    public string Second { get; set; }
}