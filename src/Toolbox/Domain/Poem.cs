using System.Text;
using System.Xml.Serialization;
using Toolbox.Settings;

namespace Toolbox.Domain;

public class Poem
{
    [XmlAttribute("id")] public string Id { get; set; } = string.Empty;

    [XmlAttribute("type")] public string? PoemType { get; set; }

    [XmlElement("titre")] public string Title { get; set; } = string.Empty;

    [XmlElement("date")] public string TextDate { get; set; } = string.Empty;

    [XmlElement("categorie")] public List<Category> Categories { get; set; } = [];

    /// <summary>
    /// When coming from storage, equal to DetailedVerseLength.
    /// When importing poem, integer value or -1 when variable.
    /// </summary>
    [XmlAttribute("longueurVers")]
    public string? VerseLength { get; set; }

    [XmlIgnore]
    public bool HasVariableMetric => VerseLength is not null &&
                                     (VerseLength == "-1" || VerseLength.Contains(",") || VerseLength.Contains(" "));

    /// <summary>
    /// Real metric, either an integer or integers separated by comma + space.
    /// </summary>
    [XmlIgnore]
    public string DetailedMetric
    {
        get
        {
            if (!HasVariableMetric)
                return VerseLength!;

            if (Info == null || !Info.StartsWith("Métrique variable : "))
            {
                throw new InvalidOperationException(
                    $"When metric is -1, info should begin with variable length indication: 'Métrique variable : ...'. Poem id: {Id}");
            }

            return Info.IndexOf(".") > -1 ? Info.Substring(20, Info.IndexOf(".") - 20) : Info.Substring(20);
        }
    }

    [XmlElement("info")] public string? Info { get; set; }

    [XmlElement("picture")] public List<string>? Pictures { get; set; }

    [XmlElement("extraTag")] public List<string>? ExtraTags { get; set; }

    [XmlElement("location")] public List<string>? Locations { get; set; }

    [XmlElement("acrostiche")] public string? Acrostiche { get; set; }

    [XmlElement("acrosticheDouble")] public DoubleAcrostiche? DoubleAcrostiche { get; set; }

    [XmlElement("para")] public List<Paragraph> Paragraphs { get; set; } = [];

    [XmlIgnore] public DateTime Date => TextDate.ToDateTime();

    [XmlIgnore]
    public bool IsSonnet => PoemType?.ToLowerInvariant() == Domain.PoemType.Sonnet.ToString().ToLowerInvariant();

    [XmlIgnore]
    public bool IsPantoun => PoemType?.ToLowerInvariant() == Domain.PoemType.Pantoun.ToString().ToLowerInvariant();

    [XmlIgnore] public string ContentFileName => $"{Title.UnaccentedCleaned()}.md";

    [XmlIgnore]
    public int SeasonId
    {
        get
        {
            if (int.TryParse(Id.Substring(Id.LastIndexOf('_') + 1), out var id))
            {
                return id;
            }

            throw new InvalidOperationException($"No season ID can be guessed from poem ID {Id}");
        }
    }

    [XmlIgnore] public int VersesCount => Paragraphs.SelectMany(x => x.Verses).Count();

    [XmlIgnore] public bool HasQuatrains => VersesCount == Paragraphs.Count * 4 && VersesCount % 4 == 0;

    public string FileContent(int poemIndex, MetricSettings metricSettings)
    {
        var s = new StringBuilder("+++");
        s.Append(Environment.NewLine);
        s.Append($"title = \"{Title}\"");
        s.Append(Environment.NewLine);
        s.Append($"id = \"{Title.UnaccentedCleaned()}_{SeasonId}\"");
        s.Append(Environment.NewLine);
        s.Append($"date = {Date.ToString("yyyy-MM-dd")}");
        s.Append(Environment.NewLine);
        s.Append($"weight = {poemIndex + 1}");
        s.Append(Environment.NewLine);

        // Categories taxonomy is fed by subcategories
        s.Append("categories = [");
        foreach (var subCategory in Categories.SelectMany(x => x.SubCategories))
        {
            s.Append($"\"{subCategory}\", ");
        }

        s.Remove(s.Length - 2, 2);
        s.Append("]");
        s.Append(Environment.NewLine);

        // Tags taxonomy is fed by: categories, (double) acrostiche, poem type, date year, variable metric, metric valu(e).
        s.Append("tags = [");
        foreach (var categoryName in Categories.Select(x => x.Name).Distinct())
        {
            s.Append($"\"{categoryName.ToLowerInvariant()}\", ");
        }

        if (ExtraTags is not null)
        {
            foreach (var extraTag in ExtraTags)
            {
                s.Append($"\"{extraTag.ToLowerInvariant()}\", ");
            }
        }

        s.Append($"\"{Date.ToString("yyyy")}\", ");

        if (Acrostiche is not null)
        {
            s.Append($"\"acrostiche\", ");
        }

        if (DoubleAcrostiche is not null)
        {
            s.Append($"\"doubleAcrostiche\", ");
        }

        if (PoemType is not null && PoemType.ToLowerInvariant() is not "default")
        {
            s.Append($"\"{PoemType.ToLowerInvariant()}\", ");
        }

        if (HasVariableMetric)
        {
            s.Append($"\"métrique variable\", ");
        }

        foreach (var metric in VerseLength.Split(','))
        {
            var metricName = metricSettings.Metrics.FirstOrDefault(x => x.Length.ToString() == metric.Trim())?.Name;
            if (metricName is not null)
                s.Append($"\"{metricName.ToLowerInvariant()}\", ");
        }

        s.Remove(s.Length - 2, 2);
        s.Append("]");
        s.Append(Environment.NewLine);

        if (Info is not null)
        {
            // When info is multiline, should be surrounded by """ followed by a line break
            // When it contains 'include "', it should be surrounded by '
            // Else it is surrounded by " but its " should be escaped
            var sep = Info.Contains("\n") ? "\"\"\""+Environment.NewLine : Info.Contains("include \"") ? "'" : "\"";
            var info = sep == "\"" ? Info.Escaped() : Info;
            s.Append($"info = {sep}{info}{sep}");
            s.Append(Environment.NewLine);
        }

        if (Pictures is not null && Pictures.Count > 0)
        {
            s.Append("pictures = [");
            foreach (var picture in Pictures)
            {
                s.Append($"\"{picture}\", ");
            }

            s.Remove(s.Length - 2, 2);
            s.Append("]");
            s.Append(Environment.NewLine);
        }

        if (PoemType is not null && PoemType.ToLowerInvariant() is not "default")
        {
            s.Append($"poemType = \"{PoemType.ToLowerInvariant()}\"");
            s.Append(Environment.NewLine);
        }

        if (Acrostiche is not null)
        {
            s.Append($"acrostiche = \"{Acrostiche.Escaped()}\"");
            s.Append(Environment.NewLine);
        }

        if (DoubleAcrostiche is not null)
        {
            s.Append($"doubleAcrostiche = \"{DoubleAcrostiche.First} | {DoubleAcrostiche.Second}\"");
            s.Append(Environment.NewLine);
        }

        if (VerseLength is not null)
        {
            var verseLength = HasVariableMetric ? "-1" : VerseLength;
            s.Append($"verseLength = {verseLength}");
            s.Append(Environment.NewLine);
        }

        if (Locations is not null && Locations.Count > 0)
        {
            s.Append("locations = [");
            foreach (var location in Locations)
            {
                s.Append($"\"{location}\", ");
            }

            s.Remove(s.Length - 2, 2);
            s.Append("]");
            s.Append(Environment.NewLine);
        }

        s.Append("LastModifierDisplayName = \"Barbara Post - Licence CC BY-NC-ND 4.0\"");
        s.Append(Environment.NewLine);

        s.Append("+++");
        s.Append(Environment.NewLine);
        s.Append(Environment.NewLine);

        foreach (var paragraph in Paragraphs)
        {
            s.Append(paragraph.FileContent());
        }

        s.Remove(s.Length - 6, 6);

        if (Pictures is not null)
        {
            for (var i = 0; i < Pictures.Count; i++)
            {
                s.Append(Environment.NewLine);
                s.Append($"{{{{< figure src=\"/images/{Id}_{i}.jpg\" title=\"{Pictures[i].Escaped()}\" >}}}}");
                s.Append(Environment.NewLine);
            }
        }

        if (Info is not null && Info.StartsWith("[^"))
        {
            s.Append(Info);
            if (!Info.EndsWith("."))
                s.Append(".");
        }

        if ((Info is not null && !Info.StartsWith("[^")) || Acrostiche is not null || DoubleAcrostiche is not null)
        {
            s.Append(Environment.NewLine);
            s.Append("{{% notice style=\"primary\" %}}");
            s.Append(Environment.NewLine);

            if (Info is not null)
            {
                s.Append(Info);
                if (!Info.EndsWith("."))
                    s.Append(".");
            }

            if (Acrostiche is not null || DoubleAcrostiche is not null)
            {
                if (Info is not null)
                {
                    s.Append(Environment.NewLine);
                    s.Append(Environment.NewLine);
                }

                if (Acrostiche is not null)
                {
                    s.Append($"Acrostiche : {Acrostiche}");
                    if (!new List<char>{'.', '?', '!'}.Contains(Acrostiche[Acrostiche.Length-1]))
                        s.Append(".");
                }
                else if (DoubleAcrostiche is not null)
                {
                    s.Append(
                        $"Acrostiche double (lignes paires et impaires) : {DoubleAcrostiche.First} / {DoubleAcrostiche.Second}.");
                }
            }

            s.Append(Environment.NewLine);
            s.Append("{{% /notice %}}");
            s.Append(Environment.NewLine);
        }

        return s.ToString();
    }
}