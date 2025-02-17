using System.Xml.Serialization;

namespace Toolbox.Domain;

public class DoubleAcrostiche
{
    [XmlAttribute("premier")] public string First { get; set; } = string.Empty;
    [XmlAttribute("deuxieme")] public string Second { get; set; } = string.Empty;
}