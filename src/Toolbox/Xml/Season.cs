using System.Xml.Serialization;

namespace Toolbox.Xml;

public class Season
{
    [XmlAttribute("id")]
    public int Id { get; set; }
    
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
    
    public string ContentDir => $"{Id}_{System.Text.Encoding.UTF8.GetString(System.Text.Encoding.GetEncoding("ISO-8859-8").GetBytes(NumberedName.ToLowerInvariant()))}_saison";
}