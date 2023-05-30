using System.Xml;
using System.Xml.Serialization;

namespace Toolbox.Xml;

public class Poem
{
    [XmlAttribute("id")] public string Id { get; set; }

    [XmlAttribute("type")] public string? PoemType { get; set; }

    [XmlElement("titre")] public string Title { get; set; }

    [XmlElement("date")] public string TextDate { get; set; }

    [XmlElement("categorie")] public List<Category> Categories { get; set; }

    [XmlElement("longueur-vers")] public int? LineLength { get; set; }

    [XmlElement("info")] public string? Info { get; set; }

    [XmlElement("acrostiche")] public dynamic? AnyAcrostiche { get; set; }

    [XmlElement("para")] public List<Paragraph> Paragraphs { get; set; }

    public DateTime Date => TextDate.Length == 10
        ? new DateTime(int.Parse(TextDate.Substring(6)), int.Parse(TextDate.Substring(3, 2)),
            int.Parse(TextDate.Substring(0, 2)))
        : new DateTime(int.Parse(TextDate), 1, 1);

    public string? Acrostiche
    {
        get
        {
            var xmlNodes = AnyAcrostiche as XmlNode[];
            if (xmlNodes?.Length == 1)
            {
                return (xmlNodes[0] as XmlText)?.Data;
            }

            return null;
        }
    }

    public SpecialAcrostiche? SpecialAcrostiche
    {
        get
        {
            var xmlNodes = AnyAcrostiche as XmlNode[];
            if (xmlNodes?.Length == 2)
            {
                return new SpecialAcrostiche
                {
                    First = (xmlNodes[0] as XmlElement)?.InnerText,
                    Second = (xmlNodes[1] as XmlElement)?.InnerText
                };
            }

            return null;
        }
    }
}