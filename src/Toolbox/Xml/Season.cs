using System.Text;
using System.Xml.Serialization;

namespace Toolbox.Xml;

public class Season
{
    [XmlAttribute("id")] public int Id { get; set; }

    [XmlAttribute("name")] public string Name { get; set; }

    [XmlAttribute("nombre")] public string NumberedName { get; set; }

    [XmlElement("summary")] public string Summary { get; set; }
    
    private string EscapedSummary => Summary.Replace("\"", "\\\"");
        

    [XmlElement("info")] public string Introduction { get; set; }

    [XmlElement("poeme")] public List<Poem> Poems { get; set; }

    public string ContentDirectoryName =>
        $"{Id}_{System.Text.Encoding.UTF8.GetString(System.Text.Encoding.GetEncoding("ISO-8859-8").GetBytes(NumberedName.ToLowerInvariant()))}_saison";

    public string IndexFileContent()
    {
        var s = new StringBuilder("+++");
        s.Append(Environment.NewLine);
        s.Append($"title = \"{NumberedName} Saison : {Name}\"");
        s.Append(Environment.NewLine);
        s.Append($"summary = \"{EscapedSummary}\"");
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