using System.Globalization;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Extensions.Primitives;

namespace Toolbox.Xml;

public class Poem
{
    [XmlAttribute("id")] public string Id { get; set; }

    [XmlAttribute("type")] public string? PoemType { get; set; }

    [XmlElement("titre")] public string Title { get; set; }

    [XmlElement("date")] public string TextDate { get; set; }

    [XmlElement("categorie")] public List<Category> Categories { get; set; }

    [XmlAttribute("longueurVers")] public string VerseLength { get; set; }

    [XmlElement("info")] public string? Info { get; set; }
    
    private string EscapedInfo => Info.Replace("\"", "\\\"");
    
    [XmlElement("acrostiche")] public string? Acrostiche { get; set; }
    
    [XmlElement("acrosticheDouble")] public DoubleAcrostiche? DoubleAcrostiche { get; set; }
    
    [XmlElement("para")] public List<Paragraph> Paragraphs { get; set; }

    public DateTime Date =>
        DateTime.ParseExact(TextDate, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None);
        
    public string ContentFileName =>
        $"{System.Text.Encoding.UTF8.GetString(System.Text.Encoding.GetEncoding("ISO-8859-8").GetBytes(Title.ToLowerInvariant())).Replace(' ', '_').Replace('\'', '_').Replace('.', '_')}.md";

    public string FileContent(int poemIndex)
    {
        var s = new StringBuilder("+++");
        s.Append(Environment.NewLine);
        s.Append($"title = \"{Title}\"");
        s.Append(Environment.NewLine);
        s.Append($"date = \"{Date.ToString("yyyy-MM-dd")}\"");
        s.Append(Environment.NewLine);
        s.Append($"weight = {poemIndex}");
        s.Append(Environment.NewLine);
        s.Append("LastModifierDisplayName = \"Barbara Post\"");
        s.Append(Environment.NewLine);
        s.Append("tags = [");
        foreach (var category in Categories)
        {
            s.Append($"\"{category}\", ");
        }
        if (Acrostiche != null)
        {
            s.Append($"\"Acrostiche\", ");
        }
        s.Remove(s.Length - 2, 2);
        s.Append("]");
        s.Append(Environment.NewLine);
        s.Append("categories = [");
        foreach (var subCategory in Categories.Select(x => x.SubCategories))
        {
            s.Append($"\"{subCategory}\", ");
        }
        s.Remove(s.Length - 2, 2);
        s.Append("]");
        if (Info != null)
        {
            s.Append("{{% notice style=\"primary\" %}}");
            s.Append(Environment.NewLine);
            s.Append(EscapedInfo);
            s.Append(Environment.NewLine);
            if (Acrostiche != null)
            {
                s.Append($"Acrostiche : {Acrostiche}");
            }
            if (DoubleAcrostiche != null)
            {
                s.Append($"Acrostiche double (lignes paires et impaires) : {DoubleAcrostiche.First} / {DoubleAcrostiche.Second}");
            }
            s.Append("{{% /notice %}}");
        }
        s.Append("+++");
        s.Append(Environment.NewLine);
        s.Append(Environment.NewLine);
        foreach (var paragraph in Paragraphs)
        {
            s.Append(paragraph.FileContent());
            s.Append((" \\"));
        }
        s.Remove(s.Length - 2, 2);
        s.Append(Environment.NewLine);
        return s.ToString();
    }
}