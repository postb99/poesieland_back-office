using System.Xml.Serialization;

namespace Toolbox.Xml;

public class Paragraph
{
    [XmlElement("vers")]
    public List<string> Verses { get; set; }
}