using System.Xml.Serialization;

namespace Toolbox.Xml;

public class Season
{
    [XmlAttribute("id")]
    public string Id { get; set; }
    
    [XmlAttribute("name")]
    public string Name { get; set; }
    
    [XmlAttribute("nombre")]
    public string NumberedName { get; set; }
    
    [XmlElement("summary")]
    public string Summary { get; set; }
    
    [XmlElement("info")]
    public string Introduction { get; set; }
    
    [XmlElement("poeme")]
    public List<Poem> Poems { get; set; }
}