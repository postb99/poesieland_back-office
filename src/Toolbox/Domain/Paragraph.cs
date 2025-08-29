using System.Text;
using System.Xml.Serialization;

namespace Toolbox.Domain;

public class Paragraph
{
    [XmlElement("vers")] public List<string> Verses { get; set; } = [];

    public string FileContent()
    {
        StringBuilder s = new();
        foreach (var verse in Verses)
        {
            s.Append(verse);
            s.Append(Environment.NewLine);
            s.Append(Environment.NewLine);
        }
        s.Append((" \\"));
        s.Append(Environment.NewLine);
        return s.ToString();
    }
}