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
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public void Load()
    {
        var xmlDocPath = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Settings.XML_STORAGE_FILE]);
        using var streamReader = new StreamReader(xmlDocPath,
            Encoding.GetEncoding(_configuration[Settings.XML_STORAGE_FILE_ENCODING]));

        Data = XmlSerializer.Deserialize(streamReader) as Root;
    }

    public void Save()
    {
        var xmlDocPath = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Settings.XML_STORAGE_FILE]);
        using var streamWriter = new StreamWriter(xmlDocPath);

        XmlSerializer.Serialize(streamWriter, Data);
    }

    public void GenerateSeasonIndexFile(int seasonId)
    {
        var season = Data.Seasons.First(x => x.Id == seasonId);
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Settings.CONTENT_ROOT_DIR]);
        var contentDir = Path.Combine(rootDir, season.ContentDirectoryName);
        var indexFile = Path.Combine(contentDir, "_index.md");
        Directory.CreateDirectory(contentDir);
        File.WriteAllText(indexFile, season.IndexFileContent());
    }

    public void GenerateAllSeasonsIndexFile()
    {
        for (var i = 1; i < Data.Seasons.Count + 1; i++)
            GenerateSeasonIndexFile(i);
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