using System.Text;
using System.Xml.Serialization;

namespace Toolbox.Domain;

public class Season
{
    [XmlAttribute("id")] public int Id { get; set; }

    [XmlAttribute("name")] public string Name { get; set; }

    [XmlAttribute("nombre")] public string NumberedName { get; set; }

    [XmlElement("summary")] public string Summary { get; set; }

    [XmlElement("info")] public string Introduction { get; set; }

    [XmlElement("poeme")] public List<Poem> Poems { get; set; }

    [XmlIgnore]
    public string ContentDirectoryName => $"{Id}_{NumberedName.UnaccentedCleaned()}_saison";
    
    public string IndexFileContent()
    {
        var s = new StringBuilder("+++");
        s.Append(Environment.NewLine);
        s.Append($"title = \"{NumberedName} Saison : {Name}\"");
        s.Append(Environment.NewLine);
        s.Append($"summary = \"{Summary.Escaped()}\"");
        s.Append(Environment.NewLine);
        s.Append($"weight = {Id}");
        s.Append(Environment.NewLine);
        s.Append("+++");
        s.Append(Environment.NewLine);
        s.Append(Environment.NewLine);
        s.Append(Introduction);
        s.Append(Environment.NewLine);
        s.Append(Environment.NewLine);
        s.Append("---");
        s.Append(Environment.NewLine);
        s.Append("{{% children  %}}");
        s.Append(Environment.NewLine);
        return s.ToString();
    }
}