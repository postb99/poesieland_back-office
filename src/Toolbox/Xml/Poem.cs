using System.Xml.Serialization;

namespace Toolbox.Xml;

public class Poem
{
    [XmlAttribute("id")]
    public string Id { get; set; }
    
    [XmlAttribute("type")]
    public string PoemType { get; set; }
    
    [XmlElement("titre")]
    public string Title { get; set; }
    
    [XmlElement("date")]
    public string TextDate { get; set; }
    
    [XmlElement("longueur-vers")]
    public string LineLength { get; set; }
    
    [XmlElement("para")]
    public List<Paragraph> Paragraphs { get; set; }
}