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

    [XmlIgnore] public string LongTitle => $"{NumberedName} Saison : {Name}";
    
    public string IndexFileContent()
    {
        var s = new StringBuilder("+++");
        s.Append(Environment.NewLine);
        s.Append($"title = \"{LongTitle}\"");
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
        s.Append(Environment.NewLine);
        s.Append("---");
        s.Append(Environment.NewLine);
        s.Append($"{{{{< chartjs id=\"season{Id}Pie\" width=\"75%\" jsFile=\"../../charts/season-{Id}-pie.js\" />}}}}");
        s.Append(Environment.NewLine);
        s.Append($"{{{{< chartjs id=\"season{Id}VerseLengthBar\" width=\"75%\" jsFile=\"../../charts/season-{Id}-verse-length-bar.js\" />}}}}");
        s.Append(Environment.NewLine);
        return s.ToString();
    }
}