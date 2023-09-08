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

    [XmlAttribute("longueurVers")] public string VerseLength { get; set; }

    [XmlElement("info")] public string? Info { get; set; }


    [XmlElement("acrostiche")] public string? Acrostiche { get; set; }

    [XmlElement("acrosticheDouble")] public DoubleAcrostiche? DoubleAcrostiche { get; set; }

    [XmlElement("para")] public List<Paragraph> Paragraphs { get; set; }

    public DateTime Date =>
        DateTime.ParseExact(TextDate, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None);

    public string ContentFileName => $"{Title.Unaccented()}.md";

    public int SeasonId => int.Parse(Id.Substring(Id.LastIndexOf('_') + 1));

    public string FileContent(int poemIndex)
    {
        var s = new StringBuilder("+++");
        s.Append(Environment.NewLine);
        s.Append($"title = \"{Title}\"");
        s.Append(Environment.NewLine);
        s.Append($"date = {Date.ToString("yyyy-MM-dd")}");
        s.Append(Environment.NewLine);
        s.Append($"weight = {poemIndex + 1}");
        s.Append(Environment.NewLine);
        s.Append("LastModifierDisplayName = \"Barbara Post\"");
        s.Append(Environment.NewLine);

        s.Append("tags = [");
        foreach (var category in Categories)
        {
            s.Append($"\"{category.Name.ToLowerInvariant()}\", ");
        }

        if (Acrostiche != null)
        {
            s.Append($"\"acrostiche\", ");
        }

        if (PoemType != null)
        {
            s.Append($"\"{PoemType.ToLowerInvariant()}\", ");
        }

        s.Remove(s.Length - 2, 2);
        s.Append("]");
        s.Append(Environment.NewLine);

        s.Append("categories = [");
        foreach (var subCategory in Categories.SelectMany(x => x.SubCategories))
        {
            s.Append($"\"{subCategory}\", ");
        }

        s.Remove(s.Length - 2, 2);
        s.Append("]");


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
                s.Append(Environment.NewLine);
            }

            if (Acrostiche != null)
            {
                s.Append(Environment.NewLine);
                s.Append($"Acrostiche : {Acrostiche}");
                s.Append(Environment.NewLine);
            }
            else if (DoubleAcrostiche != null)
            {
                s.Append(Environment.NewLine);
                s.Append(
                    $"Acrostiche double (lignes paires et impaires) : {DoubleAcrostiche.First} / {DoubleAcrostiche.Second}");
                s.Append(Environment.NewLine);
            }

            s.Append("{{% /notice %}}");
            s.Append(Environment.NewLine);
        }

        return s.ToString();
    }
}