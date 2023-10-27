using System.Globalization;
using System.Text;
using System.Xml.Serialization;

namespace Toolbox.Domain;

public class Poem
{
    [XmlAttribute("id")] public string Id { get; set; }

    [XmlAttribute("type")] public string? PoemType { get; set; }

    [XmlElement("titre")] public string Title { get; set; }

    [XmlElement("date")] public string TextDate { get; set; }

    [XmlElement("categorie")] public List<Category> Categories { get; set; }

    [XmlAttribute("longueurVers")] public string? VerseLength { get; set; }

    [XmlElement("info")] public string? Info { get; set; }

    [XmlElement("acrostiche")] public string? Acrostiche { get; set; }

    [XmlElement("acrosticheDouble")] public DoubleAcrostiche? DoubleAcrostiche { get; set; }

    [XmlElement("para")] public List<Paragraph> Paragraphs { get; set; }

    [XmlIgnore]
    public DateTime Date =>
        DateTime.ParseExact(TextDate, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None);

    [XmlIgnore]
    public string ContentFileName => $"{Title.UnaccentedCleaned()}.md";

    [XmlIgnore]
    public int SeasonId => int.Parse(Id.Substring(Id.LastIndexOf('_') + 1));

    [XmlIgnore]
    public int VersesCount => Paragraphs.SelectMany(x => x.Verses).Count();

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

        // Tags taxonomy is fed by: categories, (double) acrostiche, poem type, date year
        s.Append("tags = [");
        foreach (var categoryName in Categories.Select(x => x.Name).Distinct())
        {
            s.Append($"\"{categoryName.ToLowerInvariant()}\", ");
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

        if (PoemType != null)
        {
            s.Append($"\"{PoemType.ToLowerInvariant()}\", ");
        }

        s.Remove(s.Length - 2, 2);
        s.Append("]");
        s.Append(Environment.NewLine);

        if (Info != null)
        {
            s.Append($"info = \"{Info.Escaped()}\"");
            s.Append(Environment.NewLine);
        }

        if (PoemType != null)
        {
            s.Append($"type = \"{PoemType.ToLowerInvariant()}\"");
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
            var verseLength = VerseLength.Contains(',') ? "-1" : VerseLength;
            s.Append($"verseLength = {verseLength}");
            s.Append(Environment.NewLine);
        }

        s.Append("LastModifierDisplayName = \"Barbara Post\"");
        s.Append(Environment.NewLine);
        s.Append("+++");
        s.Append(Environment.NewLine);
        s.Append(Environment.NewLine);

        foreach (var paragraph in Paragraphs)
        {
            s.Append(paragraph.FileContent());
        }

        s.Remove(s.Length - 6, 6);

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