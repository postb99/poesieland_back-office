using System.Text;
using System.Xml.Serialization;
using Microsoft.Extensions.Configuration;
using Toolbox.Domain;
using Toolbox.Settings;

namespace Toolbox.Modules.Persistence;

public class DataManager : IDataManager
{
    private readonly IConfiguration _configuration;
    public XmlSerializer XmlSerializer { get; }
    
    public DataManager(IConfiguration configuration)
    {
        _configuration = configuration;
        XmlSerializer = new(typeof(Root));
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }
    
    public void Load(out Root data, out Root dataEn)
    {
        var xmlDocPath = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.XML_STORAGE_FILE]!);
        using var streamReader = new StreamReader(xmlDocPath,
            Encoding.GetEncoding(_configuration[Constants.XML_STORAGE_FILE_ENCODING]!));

        data = XmlSerializer.Deserialize(streamReader) as Root;

        xmlDocPath = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.XML_STORAGE_FILE_EN]!);
        using var streamReaderEn = new StreamReader(xmlDocPath,
            Encoding.GetEncoding(_configuration[Constants.XML_STORAGE_FILE_ENCODING]!));

        dataEn = XmlSerializer.Deserialize(streamReaderEn) as Root;
    }

    public void Save(Root data)
    {
        var xmlDocPath = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.XML_STORAGE_FILE]!);
                using var streamWriter = new StreamWriter(xmlDocPath);
                XmlSerializer.Serialize(streamWriter, data);
                streamWriter.Close();
    }
    
    public void SaveEn(Root dataEn)
    {
        var xmlDocPath = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.XML_STORAGE_FILE_EN]!);
        using var streamWriterEn = new StreamWriter(xmlDocPath);
        XmlSerializer.Serialize(streamWriterEn, dataEn);
        streamWriterEn.Close();
    }
}