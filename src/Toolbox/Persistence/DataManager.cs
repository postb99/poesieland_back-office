using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Extensions.Configuration;
using Toolbox.Domain;
using Toolbox.Settings;

namespace Toolbox.Persistence;

public class DataManager : IDataManager
{
    private readonly IConfiguration _configuration;
    public XmlSerializer XmlSerializer { get; }
    
    public DataManager(IConfiguration configuration)
    {
        _configuration = configuration;
        XmlSerializer = new(typeof(Root));
    }
    
    public void Load(out Root data, out Root dataEn)
    {
        var xmlDocPath = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.XML_STORAGE_FILE]!);
        var loadedData = Read(xmlDocPath);

        xmlDocPath = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.XML_STORAGE_FILE_EN]!);
        var loadedDataEn = Read(xmlDocPath);
        // Ne publier les deux modèles qu'après lecture complète : un fichier anglais invalide
        // ne doit pas laisser l'appelant avec un mélange de données anciennes et nouvelles.
        data = loadedData;
        dataEn = loadedDataEn;
    }

    public void Save(Root data)
    {
        var xmlDocPath = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.XML_STORAGE_FILE]!);
        Write(xmlDocPath, data);
    }
    
    public void SaveEn(Root dataEn)
    {
        var xmlDocPath = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.XML_STORAGE_FILE_EN]!);
        Write(xmlDocPath, dataEn);
    }

    private Root Read(string path)
    {
        // Le stockage n'utilise aucune DTD. Les interdire explicitement empêche l'expansion
        // d'entités et tout accès à une ressource externe, indépendamment des valeurs par défaut.
        using var reader = XmlReader.Create(path, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        });
        return XmlSerializer.Deserialize(reader) as Root
               ?? throw new InvalidDataException($"XML storage contains no root: {path}");
    }

    private void Write(string path, Root data)
    {
        ArgumentNullException.ThrowIfNull(data);
        using var streamWriter = new StreamWriter(path);
        XmlSerializer.Serialize(streamWriter, data);
        streamWriter.Close();
    }
}
