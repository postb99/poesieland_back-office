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
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartDataFileHelper.ChartType.Bar, 3);
        chartDataFileHelper.WriteBeforeData();
        var nbVersesData = new Dictionary<int, int>();
        var hasQuatrainsData = new Dictionary<int, int>();
        var nbSonnets = 0;
        foreach (var poem in Data.Seasons.SelectMany(x => x.Poems))
        {
            var nbVerses = poem.VersesCount;
            var hasQuatrains = poem.HasQuatrains;
            var isSonnet = poem.PoemType?.ToLowerInvariant() == PoemType.Sonnet.ToString().ToLowerInvariant();
            if (nbVersesData.TryGetValue(nbVerses, out var _))
            {
                nbVersesData[nbVerses]++;
            }
            else
            {
                nbVersesData[nbVerses] = 1;
            }

            if (hasQuatrains)
            {
                if (hasQuatrainsData.TryGetValue(nbVerses, out var _))
                {
                    hasQuatrainsData[nbVerses]++;
                }
                else
                {
                    hasQuatrainsData[nbVerses] = 1;
                }
            }

            if (isSonnet)
            {
                nbSonnets++;
            }
        }

        var nbVersesRange = nbVersesData.Keys.Order().ToList();

        var nbVersesChartData = new List<ChartDataFileHelper.DataLine>();
        var hasQuatrainsChartData = new List<ChartDataFileHelper.DataLine>();
        var isSonnetChartData = new List<ChartDataFileHelper.DataLine>();

        foreach (var nbVerses in nbVersesRange)
        {
            var hasQuatrainValue = hasQuatrainsData.ContainsKey(nbVerses) ? hasQuatrainsData[nbVerses] : 0;
            hasQuatrainsChartData.Add(
                new ChartDataFileHelper.DataLine { Label = "Quatrains", Value = hasQuatrainValue });

            isSonnetChartData.Add(new ChartDataFileHelper.DataLine { Label = string.Empty, Value = 0 });

            nbVersesChartData.Add(new ChartDataFileHelper.DataLine
                { Label = nbVerses.ToString(), Value = nbVersesData[nbVerses] - hasQuatrainValue });
        }

        var index = nbVersesRange.FindIndex(x => x == 14);
        isSonnetChartData[index] = new ChartDataFileHelper.DataLine { Label = "Sonnets", Value = nbSonnets };
        nbVersesChartData[index] = new ChartDataFileHelper.DataLine
            { Label = nbVersesChartData[index].Label, Value = nbVersesChartData[index].Value - nbSonnets };

        chartDataFileHelper.WriteData(nbVersesChartData, false);
        chartDataFileHelper.WriteData(hasQuatrainsChartData, false);
        chartDataFileHelper.WriteData(isSonnetChartData, true);

        chartDataFileHelper.WriteAfterData("poemLengthBar", new[] { "Poèmes", "Avec quatrains", "Sonnets" });
        streamWriter.Close();
    }

    public void GenerateSeasonCategoriesPieChartDataFile(int seasonId)
    {
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CHART_DATA_FILES_ROOT_DIR]);
        var storageSettings = _configuration.GetSection(Constants.STORAGE_SETTINGS).Get<StorageSettings>();
        using var streamWriter = new StreamWriter(Path.Combine(rootDir, $"season-{seasonId}-pie.js"));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartDataFileHelper.ChartType.Pie, 1);
        chartDataFileHelper.WriteBeforeData();
        var byStorageSubcategoryCount = new Dictionary<string, int>();

        var season = Data.Seasons.First(x => x.Id == seasonId);
        foreach (var poem in season.Poems)
        {
            foreach (var subCategory in poem.Categories.SelectMany(x => x.SubCategories))
            {
                if (byStorageSubcategoryCount.TryGetValue(subCategory, out var _))
                {
                    byStorageSubcategoryCount[subCategory]++;
                }
                else
                {
                    byStorageSubcategoryCount[subCategory] = 1;
                }
            }
        }

        var orderedSubcategories =
            storageSettings.Categories.SelectMany(x => x.Subcategories).Select(x => x.Name).ToList();
        var pieChartData = new List<ChartDataFileHelper.ColoredDataLine>();

        foreach (var subcategory in orderedSubcategories)
        {
            if (byStorageSubcategoryCount.ContainsKey(subcategory))
                pieChartData.Add(new ChartDataFileHelper.ColoredDataLine
                {
                    Label = subcategory, Value = byStorageSubcategoryCount[subcategory],
                    RgbColor = storageSettings.Categories.SelectMany(x => x.Subcategories)
                        .First(x => x.Name == subcategory).Color
                });
        }

        chartDataFileHelper.WriteData(pieChartData);

        chartDataFileHelper.WriteAfterData($"season{seasonId}Pie",
            new[] { $"{season.NumberedName} Saison : {season.Name} - {season.Summary.Substring(season.Summary.LastIndexOf('.') + 2)}" });
        streamWriter.Close();
    }
}