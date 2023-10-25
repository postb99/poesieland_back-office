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

    private PoemContentImporter? _poemContentImporter;

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
        streamWriter.Close();
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

    public bool ImportPoem(string poemId)
    {
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.CONTENT_ROOT_DIR]);
        var seasonId = poemId.Substring(poemId.LastIndexOf('_') + 1);
        var seasonDirName = Directory.EnumerateDirectories(rootDir)
            .FirstOrDefault(x => Path.GetFileName(x).StartsWith($"{seasonId}_"));
        if (seasonDirName == null)
        {
            return false;
        }

        var poemFileName = $"{poemId.Substring(0, poemId.LastIndexOf('_'))}.md";
        var poemContentPath = Path.Combine(rootDir, seasonDirName, poemFileName);
        if (!File.Exists(poemContentPath))
        {
            return false;
        }

        var (poem, position) =
            (_poemContentImporter ??= new PoemContentImporter()).Import(poemContentPath, _configuration);
        var targetSeason = Data.Seasons.FirstOrDefault(x => x.Id == int.Parse(seasonId));

        var existingPosition = targetSeason.Poems.FindIndex(x => x.Id == poemId);

        if (existingPosition > -1)
            targetSeason.Poems[existingPosition] = poem;
        else
            targetSeason.Poems.Add(poem);

        Save();
        return true;
    }

    public void ImportSeason(int seasonId)
    {
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.CONTENT_ROOT_DIR]);
        var seasonDirName = Directory.EnumerateDirectories(rootDir)
            .FirstOrDefault(x => Path.GetFileName(x).StartsWith($"{seasonId}_"));
        var targetSeason = Data.Seasons.FirstOrDefault(x => x.Id == seasonId);
        var poemFilePaths = Directory.EnumerateFiles(seasonDirName).Where(x => !x.EndsWith("_index.md"));
        var poemsByPosition = new Dictionary<int, Poem>(50);
        foreach (var poemContentPath in poemFilePaths)
        {
            var (poem, position) =
                (_poemContentImporter ??= new PoemContentImporter()).Import(poemContentPath, _configuration);

            poemsByPosition.Add(position, poem);
        }

        targetSeason.Poems.Clear();

        for (var i = 0; i < 50; i++)
        {
            if (poemsByPosition.TryGetValue(i, out var poem))
                targetSeason.Poems.Add(poem);
        }

        Save();
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

    public void GeneratePoemsLengthBarChartDataFile()
    {
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CHART_DATA_FILES_ROOT_DIR]);
        using var streamWriter = new StreamWriter(Path.Combine(rootDir, "poems-length-bar.js"));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartDataFileHelper.ChartType.Bar);
        chartDataFileHelper.WriteBeforeData();
        var data = new Dictionary<int, int>();
        foreach (var poem in Data.Seasons.SelectMany(x => x.Poems))
        {
            var nbVerses = poem.VersesCount;
            if (data.TryGetValue(nbVerses, out var _))
            {
                data[nbVerses]++;
            }
            else
            {
                data[nbVerses] = 1;
            }
        }

        var chartData = new List<ChartDataFileHelper.DataLine>();
        foreach (var nbVerses in data.Keys.Order())
        {
            chartData.Add(new ChartDataFileHelper.DataLine { Label = nbVerses.ToString(), Value = data[nbVerses] });
        }

        chartDataFileHelper.WriteData(chartData);

        chartDataFileHelper.WriteAfterData("poemLengthBar", "Nombre de vers par poème");
        streamWriter.Close();
    }
}