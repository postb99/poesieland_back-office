using System.Text;
using System.Text.RegularExpressions;
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

    [XmlIgnore] public string ContentDirectoryName => $"{Id}_{NumberedName.UnaccentedCleaned()}_saison";

    [XmlIgnore] public string LongTitle => $"{NumberedName} Saison : {Name}";

    [XmlIgnore] public string EscapedLongTitle => $"{NumberedName} Saison : {Name.Replace("'", "\\'")}";

    [XmlIgnore]
    public string Years
    {
        get
        {
            var infoWords = Summary.Split(' ');
            var infoLastWords = infoWords.Skip(infoWords.Length - 6).Take(6).ToList();
            var regex = new Regex("^\\d+$");
            var years = new List<string>();
            foreach (var word in infoLastWords)
            {
                var matches = regex.Matches(word);
                foreach (var match in matches)
                {
                    years.Add(match.ToString());
                }
            }

            switch (years.Count)
            {
                case 1:
                    return years[0];
                case 2 when years[0] == years[1]:
                    return years[0];
                case 2:
                    return $"{years[0]} - {years[1]}";
            }

            return null;
        }
    }

    [XmlIgnore]
    public string Period
    {
        get
        {
            var summaryLastDot = Summary.LastIndexOf('.');
            return Summary.Substring(summaryLastDot == -1 ? 0 : summaryLastDot + 2);
        }
    }

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
        s.Append($"{{{{% include \"./includes/season_{Id}.md\" true %}}}}");
        s.Append(Environment.NewLine);
        s.Append(Environment.NewLine);
        s.Append("---");
        s.Append(Environment.NewLine);
        s.Append("## Catégories");
        s.Append(Environment.NewLine);
        s.Append($"{{{{< chartjs id=\"season{Id}Pie\" width=\"75%\" jsFile=\"../../charts/season-{Id}/categories-pie.js\" />}}}}");
        s.Append(Environment.NewLine);
        s.Append("## Longueur des vers");
        s.Append(Environment.NewLine);
        s.Append(
            $"{{{{< chartjs id=\"season{Id}VerseLengthBar\" width=\"75%\" jsFile=\"../../charts/season-{Id}/poems-verse-length-bar.js\" />}}}}");
        s.Append(Environment.NewLine);
        s.Append("## Longueur des poèmes");
        s.Append(Environment.NewLine);
        s.Append(
            $"{{{{< chartjs id=\"season{Id}PoemLengthBar\" width=\"75%\" jsFile=\"../../charts/season-{Id}/poems-length-bar.js\" />}}}}");
        s.Append(Environment.NewLine);
        s.Append("## Intervalle");
        s.Append(Environment.NewLine);
        s.Append($"{{{{< chartjs id=\"season{Id}PoemIntervalBar\" width=\"75%\" jsFile=\"../../charts/season-{Id}/poem-interval-bar.js\" />}}}}");
        s.Append(Environment.NewLine);
        return s.ToString();
    }
}