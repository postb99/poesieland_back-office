using System.Globalization;
using System.Text;
using System.Xml.Serialization;

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
    public bool HasVariableMetric => VerseLength != null &&
                                     (VerseLength == "-1" || VerseLength.Contains(",") || VerseLength.Contains(" "));

    /// <summary>
    /// Real verse length, either an integer or integers separated by comma + space.
    /// </summary>
    [XmlIgnore]
    public string DetailedVerseLength
    {
        get
        {
            if (!HasVariableMetric)
                return VerseLength!;

            if (Info == null || !Info.StartsWith("Métrique variable : "))
            {
                throw new InvalidOperationException(
                    $"When verse length is -1, info should begin with variable length indication: 'Métrique variable : ...'. Poem id: {Id}");
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

    public string FileContent(int poemIndex)
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

        // Tags taxonomy is fed by: categories, (double) acrostiche, poem type, date year, variable metric
        s.Append("tags = [");
        foreach (var categoryName in Categories.Select(x => x.Name).Distinct())
        {
            s.Append($"\"{categoryName.ToLowerInvariant()}\", ");
        }

        if (ExtraTags != null)
        {
            foreach (var extraTag in ExtraTags)
            {
                s.Append($"\"{extraTag.ToLowerInvariant()}\", ");
            }
        }

        s.Append($"\"{Date.ToString("yyyy")}\", ");

        if (Acrostiche != null)
        {
            s.Append($"\"acrostiche\", ");
        }

        if (DoubleAcrostiche != null)
        {
            s.Append($"\"doubleAcrostiche\", ");
        }

        if (PoemType != null && PoemType.ToLowerInvariant() != "default")
        {
            s.Append($"\"{PoemType.ToLowerInvariant()}\", ");
        }

        if (HasVariableMetric)
        {
            s.Append($"\"métrique variable\", ");
        }

        s.Remove(s.Length - 2, 2);
        s.Append("]");
        s.Append(Environment.NewLine);

        if (Info != null)
        {
            s.Append($"info = \"{Info.Escaped()}\"");
            s.Append(Environment.NewLine);
        }

        if (Pictures != null && Pictures.Count > 0)
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

        if (PoemType != null && PoemType.ToLowerInvariant() != "default")
        {
            s.Append($"poemType = \"{PoemType.ToLowerInvariant()}\"");
            s.Append(Environment.NewLine);
        }

        if (Acrostiche != null)
        {
            s.Append($"acrostiche = \"{Acrostiche}\"");
            s.Append(Environment.NewLine);
        }

        if (DoubleAcrostiche != null)
        {
            s.Append($"doubleAcrostiche = \"{DoubleAcrostiche.First} | {DoubleAcrostiche.Second}\"");
            s.Append(Environment.NewLine);
        }

        if (VerseLength != null)
        {
            var verseLength = HasVariableMetric ? "-1" : VerseLength;
            s.Append($"verseLength = {verseLength}");
            s.Append(Environment.NewLine);
        }

        if (Locations != null)
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

        if (Pictures != null)
        {
            for (var i = 0; i < Pictures.Count; i++)
            {
                s.Append(Environment.NewLine);
                s.Append($"{{{{< figure src=\"/images/{Id}_{i}.jpg\" title=\"{Pictures[i].Escaped()}\" >}}}}");
                s.Append(Environment.NewLine);
            }
        }

        if (Info != null || Acrostiche != null || DoubleAcrostiche != null)
        {
            s.Append(Environment.NewLine);
            s.Append("{{% notice style=\"primary\" %}}");
            s.Append(Environment.NewLine);

            if (Info != null)
            {
                s.Append(Info.Escaped());
            }

            if (Acrostiche != null || DoubleAcrostiche != null)
            {
                if (Info != null)
                {
                    s.Append(Environment.NewLine);
                    s.Append(Environment.NewLine);
                }

                if (Acrostiche != null)
                {
                    s.Append($"Acrostiche : {Acrostiche}");
                }
                else if (DoubleAcrostiche != null)
                {
                    s.Append(
                        $"Acrostiche double (lignes paires et impaires) : {DoubleAcrostiche.First} / {DoubleAcrostiche.Second}");
                }
            }

            s.Append(Environment.NewLine);
            s.Append("{{% /notice %}}");
            s.Append(Environment.NewLine);
        }

        return s.ToString();
    }
}