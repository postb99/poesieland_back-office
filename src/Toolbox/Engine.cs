using System.Text;
using System.Xml.Serialization;
using Microsoft.Extensions.Configuration;
using Toolbox.Domain;
using Toolbox.Settings;

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
        var xmlDocPath = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.XML_STORAGE_FILE]);
        using var streamReader = new StreamReader(xmlDocPath,
            Encoding.GetEncoding(_configuration[Constants.XML_STORAGE_FILE_ENCODING]));

        Data = XmlSerializer.Deserialize(streamReader) as Root;
    }

    public void Save()
    {
        var xmlDocPath = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.XML_STORAGE_FILE]);
        using var streamWriter = new StreamWriter(xmlDocPath);

        XmlSerializer.Serialize(streamWriter, Data);
    }

    public void GenerateSeasonIndexFile(int seasonId)
    {
        var season = Data.Seasons.First(x => x.Id == seasonId);
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.CONTENT_ROOT_DIR]);
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

    public void GeneratePoemFile(Poem poem)
    {
        var season = Data.Seasons.First(x => x.Id == poem.SeasonId);
        var poemIndex = season.Poems.IndexOf(poem);
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.CONTENT_ROOT_DIR]);
        var contentDir = Path.Combine(rootDir, season.ContentDirectoryName);
        var indexFile = Path.Combine(contentDir, poem.ContentFileName);
        File.WriteAllText(indexFile, poem.FileContent(poemIndex));
    }

    public void GenerateSeasonAllPoemFiles(int seasonId)
    {
        var season = Data.Seasons.First(x => x.Id == seasonId);
        season.Poems.ForEach(GeneratePoemFile);
    }

    public void GenerateAllPoemFiles()
    {
        var poems = Data.Seasons.SelectMany(x => x.Poems).ToList();
        poems.ForEach(GeneratePoemFile);
    }

    public IEnumerable<string> CheckMissingYearTagInYamlMetadata()
    {
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.CONTENT_ROOT_DIR]);
        var seasonMaxId = Data.Seasons.Count;
        var poemContentImporter = new PoemContentImporter();
        for (var i = 16; i < seasonMaxId + 1; i++)
        {
            var season = Data.Seasons.First(x => x.Id == i);
            var contentDir = Path.Combine(rootDir, season.ContentDirectoryName);
            var poemContentPaths = Directory.EnumerateFiles(contentDir).Where(x => !x.EndsWith("_index.md"));
            foreach (var poemContentPath in poemContentPaths)
            {
                var yearAndTags = poemContentImporter.Extract(poemContentPath);
                if (poemContentImporter.HasYamlMetadata && !yearAndTags.tags.Contains(yearAndTags.year.ToString()))
                {
                    yield return poemContentPath;
                }
            }
        }

    }
}