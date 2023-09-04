using System.Globalization;
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

    [XmlAttribute("longueurVers")] public string VerseLength { get; set; }

    [XmlElement("info")] public string? Info { get; set; }
    
    [XmlElement("acrostiche")] public string? Acrostiche { get; set; }
    
    [XmlElement("crossingAcrostiche")] public CrossingAcrostiche? CrossingAcrostiche { get; set; }
    
    [XmlElement("para")] public List<Paragraph> Paragraphs { get; set; }

    public DateTime Date =>
        DateTime.ParseExact(TextDate, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None);
}