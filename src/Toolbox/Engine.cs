using System.Text;
using System.Xml.Serialization;
using Microsoft.Extensions.Configuration;
using Toolbox.Xml;

namespace Toolbox;

public class Engine
{
    private IConfiguration _configuration;
    public Root Data { get; private set; }
    public XmlSerializer XmlSerializer { get; private set; }
    
    public Engine(IConfiguration configuration)
    {
        _configuration = configuration;
        XmlSerializer = new XmlSerializer(typeof(Root));
    }

    public void Load()
    {
        var xmlDocPath = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Settings.XML_STORAGE_FILE]);
        using var streamReader = new StreamReader(xmlDocPath, Encoding.GetEncoding(_configuration[Settings.XML_STORAGE_FILE_ENCODING]));

        Data = XmlSerializer.Deserialize(streamReader) as Root;
    }
    
    public void Save()
    {
        var xmlDocPath = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Settings.XML_STORAGE_FILE]);
        using var streamWriter = new StreamWriter(xmlDocPath);
    
        XmlSerializer.Serialize(streamWriter, Data);
    }
    
    // public Root LoadCleaned()
    // {
    //     var xmlDocPath = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Settings.XML_STORAGE_CLEANED_FILE]);
    //     using var streamReader = new StreamReader(xmlDocPath);
    //
    //    return XmlSerializer.Deserialize(streamReader) as Root;
    // }
    //
    // public void SaveCleaned()
    // {
    //     var xmlDocPath = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Settings.XML_STORAGE_CLEANED_FILE]);
    //     using var streamWriter = new StreamWriter(xmlDocPath);
    //
    //     XmlSerializer.Serialize(streamWriter, Data);
    // }
}