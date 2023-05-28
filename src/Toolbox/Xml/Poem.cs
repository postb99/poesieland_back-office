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
    
    [XmlElement("categorie")]
    public List<Category> Categories { get; set; }
    
    [XmlElement("longueur-vers")]
    public string LineLength { get; set; }
    
    [XmlElement("info")]
    public string Info { get; set; }
    
    [XmlElement("acrostiche")]
    public Acrostiche? Acrostiche { get; set; }
    
    [XmlElement("para")]
    public List<Paragraph> Paragraphs { get; set; }

    public DateTime Date => TextDate.Length == 10
        ? new DateTime(int.Parse(TextDate.Substring(6)), int.Parse(TextDate.Substring(3, 2)),
            int.Parse(TextDate.Substring(0, 2)))
        : new DateTime(int.Parse(TextDate), 1, 1);
}