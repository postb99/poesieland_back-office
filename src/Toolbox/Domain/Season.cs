using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace Toolbox.Domain;

public class Season
{
    [XmlAttribute("id")] public int Id { get; set; }

    [XmlAttribute("name")] public string Name { get; set; } = string.Empty;

    [XmlAttribute("nombre")] public string NumberedName { get; set; } = string.Empty;

    [XmlElement("summary")] public string Summary { get; set; } = string.Empty;

    [XmlElement("info")] public string Introduction { get; set; } = string.Empty;

    [XmlElement("poeme")] public List<Poem> Poems { get; set; } = [];

    [XmlIgnore] public string ContentDirectoryName => $"{Id}_{NumberedName.UnaccentedCleaned()}_saison";

    [XmlIgnore] public string LongTitle => $"{NumberedName} Saison : {Name}";

    [XmlIgnore] public string EscapedTitleForChartsWithYears => $"{Name.Replace("'", "\\'")} ({Id}) {Years}";
    [XmlIgnore] public string EscapedTitleForChartsWithPeriod => $"{Name.Replace("'", "\\'")} ({Id}) {Period}";

    [XmlIgnore]
    public string Years
    {
        get
        {
            var years = Poems.Select(x => x.Date.Year.ToString()).Distinct().ToList();
            years.Sort();

            switch (years.Count)
            {
                case 0:
                    return string.Empty;
                case 1:
                    return years[0];
                case 2 when years[0] == years[1]:
                    return years[0];
                default:
                    // Same century or not
                    return years[0][0] == years[^1][0] ? $"{years[0]}-{years[^1][2..4]}" : $"{years[0]}-{years[^1]}";
            }
        }
    }

    [XmlIgnore]
    public string Period
    {
        get
        {
            var years = Poems.Select(x => x.Date).Distinct().ToList();
            years.Sort();
            if (years.Count == 0)
                return string.Empty;

            return years[0].Year == years[^1].Year
                ? $"{years[0]:MMMM} à {years[^1]:MMMM yyyy}"
                : $"{years[0]:MMMM yyyy} à {years[^1]:MMMM yyyy}";
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
        s.Append(
            $"{{{{< chartjs id=\"season{Id}Pie\" width=\"75%\" jsFile=\"../../charts/season-{Id}/categories-pie.js\" />}}}}");
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
        s.Append(
            $"{{{{< chartjs id=\"season{Id}PoemIntervalBar\" width=\"75%\" jsFile=\"../../charts/season-{Id}/poem-interval-bar.js\" />}}}}");
        s.Append(Environment.NewLine);
        return s.ToString();
    }
}