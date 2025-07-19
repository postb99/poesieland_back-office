using System.Globalization;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Extensions.Configuration;
using Toolbox.Domain;
using Toolbox.Settings;

namespace Toolbox;

public class Engine
{
    private readonly IConfiguration _configuration;
    public Root Data { get; private set; } = default!;
    public Root DataEn { get; private set; } = default!;
    public XmlSerializer XmlSerializer { get; }

    private PoemContentImporter? _poemContentImporter;

    public Engine(IConfiguration configuration)
    {
        _configuration = configuration;
        Data = new Root { Seasons = [] };
        XmlSerializer = new XmlSerializer(typeof(Root));
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public void Load()
    {
        var xmlDocPath = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.XML_STORAGE_FILE]!);
        using var streamReader = new StreamReader(xmlDocPath,
            Encoding.GetEncoding(_configuration[Constants.XML_STORAGE_FILE_ENCODING]!));

        Data = XmlSerializer.Deserialize(streamReader) as Root;

        xmlDocPath = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.XML_STORAGE_FILE_EN]!);
        using var streamReaderEn = new StreamReader(xmlDocPath,
            Encoding.GetEncoding(_configuration[Constants.XML_STORAGE_FILE_ENCODING]!));

        DataEn = XmlSerializer.Deserialize(streamReaderEn) as Root;
    }

    public void Save()
    {
        var xmlDocPath = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.XML_STORAGE_FILE]!);
        using var streamWriter = new StreamWriter(xmlDocPath);
        XmlSerializer.Serialize(streamWriter, Data);
        streamWriter.Close();

        xmlDocPath = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.XML_STORAGE_FILE_EN]!);
        using var streamWriterEn = new StreamWriter(xmlDocPath);
        XmlSerializer.Serialize(streamWriterEn, DataEn);
        streamWriterEn.Close();
    }

    public void GenerateSeasonIndexFile(int seasonId)
    {
        var season = Data.Seasons.First(x => x.Id == seasonId);
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.CONTENT_ROOT_DIR]!);
        var contentDir = Path.Combine(rootDir, season.ContentDirectoryName);
        var indexFile = Path.Combine(contentDir, "_index.md");
        Directory.CreateDirectory(contentDir);
        File.WriteAllText(indexFile, season.IndexFileContent());

        rootDir = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.CHART_DATA_FILES_ROOT_DIR]!);
        contentDir = Path.Combine(rootDir, $"season-{seasonId}");
        Directory.CreateDirectory(contentDir);
    }

    public void GenerateAllSeasonsIndexFile()
    {
        for (var i = 1; i < Data.Seasons.Count + 1; i++)
        {
            GenerateSeasonIndexFile(i);
        }
    }

    public void GeneratePoemFile(Poem poem)
    {
        var metricSettings = _configuration.GetSection(Constants.METRIC_SETTINGS).Get<MetricSettings>();
        var season = Data.Seasons.First(x => x.Id == poem.SeasonId);
        var poemIndex = season.Poems.IndexOf(poem);
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.CONTENT_ROOT_DIR]!);
        var contentDir = Path.Combine(rootDir, season.ContentDirectoryName);
        var indexFile = Path.Combine(contentDir, poem.ContentFileName);
        File.WriteAllText(indexFile, poem.FileContent(poemIndex, metricSettings!));
    }

    public void GenerateSeasonAllPoemFiles(int seasonId)
    {
        var season = Data.Seasons.First(x => x.Id == seasonId);
        season.Poems.ForEach(GeneratePoemFile);
    }

    public Poem ImportPoem(string poemId, bool save = true)
    {
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.CONTENT_ROOT_DIR]!);
        var seasonId = poemId.Substring(poemId.LastIndexOf('_') + 1);
        if (!int.TryParse(seasonId, out _))
        {
            throw new ArgumentException($"'{poemId}' does not end with season id");
        }

        var seasonDirName = Directory.EnumerateDirectories(rootDir)
            .FirstOrDefault(x => Path.GetFileName(x).StartsWith($"{seasonId}_"));
        if (seasonDirName == null)
        {
            throw new ArgumentException(
                $"No such season content directory for id '{seasonId}'. Create season directory before importing poem");
        }

        var poemFileName = $"{poemId.Substring(0, poemId.LastIndexOf('_'))}.md";
        var poemContentPath = Path.Combine(rootDir, seasonDirName, poemFileName);
        if (!File.Exists(poemContentPath))
        {
            throw new ArgumentException($"Poem content file not found: {poemContentPath}");
        }

        var (poem, _) = (_poemContentImporter ??= new PoemContentImporter(_configuration)).Import(poemContentPath);
        var anomalies = _poemContentImporter!.CheckAnomaliesAfterImport();
        foreach (var anomaly in anomalies)
            Console.WriteLine($"[ERROR]: {anomaly}");
        var targetSeason = Data.Seasons.FirstOrDefault(x => x.Id == int.Parse(seasonId));

        if (targetSeason == null)
        {
            targetSeason = new Season
            {
                Id = int.Parse(seasonId), Name = "TODO", NumberedName = "TODO", Introduction = "TODO", Summary = "TODO",
                Poems = []
            };
            Data.Seasons.Add(targetSeason);
        }

        var existingPosition = targetSeason.Poems.FindIndex(x => x.Id == poemId);

        if (existingPosition > -1)
            targetSeason.Poems[existingPosition] = poem;
        else
            targetSeason.Poems.Add(poem);

        if (save)
            Save();
        return poem;
    }

    public void ImportSeason(int seasonId, bool save = true)
    {
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.CONTENT_ROOT_DIR]!);
        var seasonDirName = Directory.EnumerateDirectories(rootDir)
            .FirstOrDefault(x => Path.GetFileName(x).StartsWith($"{seasonId}_"));
        var targetSeason = Data.Seasons.FirstOrDefault(x => x.Id == seasonId);
        var poemFilePaths = Directory.EnumerateFiles(seasonDirName!).Where(x => !x.EndsWith("_index.md"));
        var poemsByPosition = new Dictionary<int, Poem>(50);
        foreach (var poemContentPath in poemFilePaths)
        {
            var (poem, position) =
                (_poemContentImporter ??= new PoemContentImporter(_configuration)).Import(poemContentPath);
            var anomalies = _poemContentImporter!.CheckAnomaliesAfterImport();
            foreach (var anomaly in anomalies)
                Console.WriteLine($"[ERROR]: {anomaly}");

            poemsByPosition.Add(position, poem);
        }

        if (targetSeason != null)
        {
            targetSeason.Poems.Clear();
        }
        else
        {
            targetSeason = new Season { Id = seasonId, Poems = [] };
            Data.Seasons.Add(targetSeason);
        }

        for (var i = 0; i < 50; i++)
        {
            if (poemsByPosition.TryGetValue(i, out var poem))
                targetSeason.Poems.Add(poem);
        }

        if (save)
            Save();
    }

    public void ImportPoemsEn()
    {
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.CONTENT_ROOT_DIR_EN]);

        foreach (var season in DataEn.Seasons)
        {
            season.Poems.Clear();
        }

        var yearDirNames = Directory.EnumerateDirectories(rootDir);

        foreach (var yearDirName in yearDirNames)
        {
            var poemFilePaths = Directory.EnumerateFiles(yearDirName).Where(x => !x.EndsWith("_index.md"));
            var poemsByPosition = new Dictionary<int, Poem>(50);

            foreach (var poemContentPath in poemFilePaths)
            {
                var (poem, position) =
                    (_poemContentImporter ??= new PoemContentImporter(_configuration)).Import(poemContentPath);

                poemsByPosition.Add(position, poem);
            }

            for (var i = 0; i < 50; i++)
            {
                if (poemsByPosition.TryGetValue(i, out var poem))
                {
                    var seasonId = poem.SeasonId;
                    var targetSeason = DataEn.Seasons.FirstOrDefault(x => x.Id == seasonId);
                    if (targetSeason == null)
                    {
                        targetSeason = new Season { Id = seasonId, Poems = [] };
                        DataEn.Seasons.Add(targetSeason);
                    }

                    targetSeason.Poems.Add(poem);
                }
            }
        }

        Save();
    }

    public void GenerateAllPoemFiles()
    {
        var poems = Data.Seasons.SelectMany(x => x.Poems).ToList();
        poems.ForEach(GeneratePoemFile);
    }

    public IEnumerable<string> CheckMissingTagsInYamlMetadata()
    {
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.CONTENT_ROOT_DIR]!);
        var poemContentImporter = new PoemContentImporter(_configuration);

        var seasonMaxId = Data.Seasons.Count;
        for (var i = 17; i < seasonMaxId + 1; i++)
        {
            var season = Data.Seasons.First(x => x.Id == i);
            var contentDir = Path.Combine(rootDir, season.ContentDirectoryName);
            var poemContentPaths = Directory.EnumerateFiles(contentDir).Where(x => !x.EndsWith("_index.md"));
            foreach (var poemContentPath in poemContentPaths)
            {
                var partialImport = poemContentImporter.GetPartialImport(poemContentPath);
                if (!poemContentImporter.HasYamlMetadata) continue;

                foreach (var p in poemContentImporter.CheckAnomalies(partialImport))
                    yield return
                        $"{p}in {poemContentPath}";
            }
        }
    }

    public IEnumerable<string> GetReusedTitles()
    {
        var allowedReuses = File.ReadAllLines("./allowed_reuses.txt");
        foreach (var group in Data.Seasons.SelectMany(x => x.Poems).GroupBy(x => x.Title))
        {
            var count = group.Count();
            if (count > 1)
            {
                var outputLine =
                    $"Reused title {group.Key} {count} times ({string.Join(", ", group.Select(g => g.Id))})";
                if (!allowedReuses.Contains(outputLine))
                    yield return outputLine;
            }
        }
    }

    public void CheckPoemsWithoutVerseLength()
    {
        var poems = Data.Seasons.SelectMany(x => x.Poems);
        var poemsWithVerseLength = poems.Count(x => x.HasVerseLength);
        if (poemsWithVerseLength == poems.Count())
            return;

        var incorrectPoem = poems.FirstOrDefault(x => !x.HasVerseLength);
        if (incorrectPoem != null)
            throw new Exception(
                $"[ERROR] First poem with unspecified metric or equal to '0': {incorrectPoem.Id}");
    }

    public void CheckPoemsWithVariableMetric()
    {
        var poems = Data.Seasons.SelectMany(x => x.Poems.Where(x => x.HasVariableMetric));

        var incorrectPoem = poems.FirstOrDefault(x => !x.Info.StartsWith("Métrique variable : "));
        if (incorrectPoem != null)
            throw new Exception(
                $"[ERROR] First poem with variable metric unspecified in Info: {incorrectPoem.Id}");
    }

    public void GeneratePoemsLengthBarAndPieChartDataFile(int? seasonId)
    {
        var isGeneral = seasonId is null;
        var fileName = isGeneral ? "poems-length-pie.js" : "poems-length-bar.js";
        var subDir = isGeneral ? "general" : $"season-{seasonId}";
        var chartId = isGeneral ? "poemLengthPie" : $"season{seasonId}PoemLengthBar";

        var poems = seasonId != null
            ? Data.Seasons.First(x => x.Id == seasonId).Poems
            : Data.Seasons.SelectMany(x => x.Poems);

        var nbVersesData = new Dictionary<int, int>();
        var nbSonnets = 0;
        foreach (var poem in poems)
        {
            var nbVerses = poem.VersesCount;
            if (nbVersesData.TryGetValue(nbVerses, out _))
            {
                nbVersesData[nbVerses]++;
            }
            else
            {
                nbVersesData[nbVerses] = 1;
            }

            if (poem.IsSonnet)
            {
                nbSonnets++;
            }
        }

        var nbVersesRange = nbVersesData.Keys.Order().ToList();

        // General pie chart or Season's bar chart
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CHART_DATA_FILES_ROOT_DIR]!);
        var subDirPath = Path.Combine(rootDir, subDir);
        Directory.CreateDirectory(subDirPath);
        using var streamWriter = new StreamWriter(Path.Combine(subDirPath, fileName));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter,
            isGeneral ? ChartDataFileHelper.ChartType.Pie : ChartDataFileHelper.ChartType.Bar, isGeneral ? 1 : 2);
        chartDataFileHelper.WriteBeforeData();

        if (isGeneral)
        {
            var metrics = _configuration.GetSection(Constants.METRIC_SETTINGS).Get<MetricSettings>().Metrics;
            var coloredDataLines = new List<ChartDataFileHelper.ColoredDataLine>();

            foreach (var nbVerses in nbVersesRange)
            {
                var lookup = nbVerses switch
                {
                    3 => 0,
                    26 => 1,
                    _ => nbVerses / 2
                };

                var color = metrics.First(m => m.Length == lookup).Color;
                coloredDataLines.Add(new ChartDataFileHelper.ColoredDataLine(nbVerses.ToString(),
                    nbVersesData[nbVerses], color));
            }

            chartDataFileHelper.WriteData(coloredDataLines, true);

            chartDataFileHelper.WriteAfterData(chartId, ["Poèmes"]);
        }
        else
        {
            var nbVersesChartData = new List<ChartDataFileHelper.DataLine>();
            var isSonnetChartData = new List<ChartDataFileHelper.DataLine>();

            foreach (var nbVerses in nbVersesRange)
            {
                isSonnetChartData.Add(new ChartDataFileHelper.DataLine(string.Empty, 0));

                nbVersesChartData.Add(new ChartDataFileHelper.DataLine(nbVerses.ToString(),
                    nbVersesData[nbVerses]));
            }

            var index = nbVersesRange.FindIndex(x => x == 14);
            if (index != -1)
            {
                isSonnetChartData[index] = new ChartDataFileHelper.DataLine("Sonnets", nbSonnets);
                nbVersesChartData[index] = new ChartDataFileHelper.DataLine
                    (nbVersesChartData[index].Label, nbVersesChartData[index].Value - nbSonnets);
            }

            string[] chartTitles;
            if (nbSonnets > 0)
            {
                chartDataFileHelper.WriteData(nbVersesChartData, false);
                chartDataFileHelper.WriteData(isSonnetChartData, true);
                chartTitles = ["Poèmes", "Sonnets"];
            }
            else
            {
                chartDataFileHelper.WriteData(nbVersesChartData, true);
                chartTitles = ["Poèmes"];
            }

            chartDataFileHelper.WriteAfterData(chartId, chartTitles,
                customScalesOptions: "scales: { y: { ticks: { stepSize: 1 } } }");
        }

        streamWriter.Close();
    }

    public void GenerateSeasonCategoriesPieChartDataFile(int? seasonId)
    {
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CHART_DATA_FILES_ROOT_DIR]!);
        var subDir = seasonId.HasValue ? $"season-{seasonId}" : "general";
        var storageSettings = _configuration.GetSection(Constants.STORAGE_SETTINGS).Get<StorageSettings>();
        using var streamWriter = new StreamWriter(Path.Combine(rootDir, subDir, "categories-pie.js"));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartDataFileHelper.ChartType.Pie);
        chartDataFileHelper.WriteBeforeData();
        var byStorageSubcategoryCount = new Dictionary<string, int>();

        var season = seasonId.HasValue ? Data.Seasons.First(x => x.Id == seasonId) : null;
        foreach (var poem in season?.Poems ?? Data.Seasons.SelectMany(x => x.Poems))
        {
            foreach (var subCategory in poem.Categories.SelectMany(x => x.SubCategories))
            {
                if (byStorageSubcategoryCount.TryGetValue(subCategory, out _))
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
            if (byStorageSubcategoryCount.TryGetValue(subcategory, out var value))
                pieChartData.Add(new ChartDataFileHelper.ColoredDataLine
                (subcategory, value,
                    storageSettings.Categories.SelectMany(x => x.Subcategories)
                        .First(x => x.Name == subcategory).Color
                ));
        }

        chartDataFileHelper.WriteData(pieChartData);

        chartDataFileHelper.WriteAfterData(seasonId.HasValue ? $"season{seasonId}Pie" : "categoriesPie",
        [
            seasonId.HasValue ? $"{season.EscapedTitleForChartsWithPeriod}" : string.Empty
        ]);
        streamWriter.Close();
    }

    public void GeneratePoemsByDayRadarChartDataFile(string? storageSubCategory, string? storageCategory)
    {
        var poemStringDates = new List<string>();
        if (storageSubCategory != null)
        {
            poemStringDates = Data.Seasons.SelectMany(x => x.Poems)
                .Where(x => x.Categories.Any(x => x.SubCategories.Contains(storageSubCategory))).Select(x => x.TextDate)
                .ToList();
        }
        else if (storageCategory != null)
        {
            poemStringDates = Data.Seasons.SelectMany(x => x.Poems)
                .Where(x => x.Categories.Any(x => x.Name == storageCategory)).Select(x => x.TextDate)
                .ToList();
        }
        else
        {
            poemStringDates = Data.Seasons.SelectMany(x => x.Poems).Select(x => x.TextDate).ToList();

            // Add EN poems
            poemStringDates.AddRange(DataEn.Seasons.SelectMany(x => x.Poems).Select(x => x.TextDate));
        }

        var dataDict = InitMonthDayDictionary();

        foreach (var poemStringDate in poemStringDates)
        {
            var year = poemStringDate.Substring(6);
            if (year == "1994")
                continue;
            var monthDay = $"{poemStringDate.Substring(3, 2)}-{poemStringDate.Substring(0, 2)}";
            dataDict[monthDay]++;
        }

        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CHART_DATA_FILES_ROOT_DIR]!);
        var fileName = string.Empty;

        var chartId = string.Empty;
        var borderColor = string.Empty;

        if (storageSubCategory != null)
        {
            // categories
            fileName = $"poems-day-{storageSubCategory.UnaccentedCleaned()}-radar.js";
            chartId = $"poemDay-{storageSubCategory.UnaccentedCleaned()}Radar";
            borderColor = _configuration.GetSection(Constants.STORAGE_SETTINGS).Get<StorageSettings>().Categories
                .SelectMany(x => x.Subcategories).FirstOrDefault(x => x.Name == storageSubCategory).Color;

            switch (borderColor)
            {
                // Use some not too light colors
                case "rgba(254, 231, 240, 1)":
                    borderColor = "rgba(255, 194, 209, 1)";
                    break;
                case "rgba(247, 235, 253, 1)":
                    borderColor = "rgba(234, 191, 250, 1)";
                    break;
                case "rgba(244, 254, 254, 1)":
                    borderColor = "rgba(119, 181, 254, 1)";
                    break;
            }
        }
        else if (storageCategory != null)
        {
            // tags
            fileName = $"poems-day-{storageCategory.UnaccentedCleaned()}-radar.js";
            chartId = $"poemDay-{storageCategory.UnaccentedCleaned()}Radar";
            borderColor = _configuration.GetSection(Constants.STORAGE_SETTINGS).Get<StorageSettings>().Categories
                .FirstOrDefault(x => x.Name == storageCategory).Color;
        }
        else
        {
            // general
            fileName = "poems-day-radar.js";
            chartId = "poemDayRadar";
        }

        using var streamWriter = new StreamWriter(Path.Combine(rootDir,
            storageCategory != null || storageSubCategory != null ? "taxonomy" : "general", fileName));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartDataFileHelper.ChartType.Radar);
        chartDataFileHelper.WriteBeforeData();

        var dataLines = new List<ChartDataFileHelper.DataLine>();

        var dayWithoutPoems = new List<string>();

        foreach (var monthDay in dataDict.Keys)
        {
            var value = dataDict[monthDay];
            dataLines.Add(new ChartDataFileHelper.DataLine(GetRadarChartLabel(monthDay), value
            ));
            if (value == 0)
            {
                dayWithoutPoems.Add(monthDay);
            }
        }

        chartDataFileHelper.WriteData(dataLines, true);

        var backgroundColor = borderColor?.Replace("1)", "0.5)");

        var title = $"Mois les plus représentés : {string.Join(", ", GetTopMostMonths(dataDict))}";

        chartDataFileHelper.WriteAfterData(chartId, [title], borderColor, backgroundColor);
        streamWriter.Close();

        if (storageSubCategory != null || storageCategory != null) return;

        // Days without poems listing

        var filePath = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.CONTENT_ROOT_DIR]!,
            "../includes/days_without_creation.md");
        var streamWriter2 = new StreamWriter(filePath);

        streamWriter2.WriteLine("+++");
        streamWriter2.WriteLine("title = \"Les jours sans\"");
        streamWriter2.WriteLine("+++");

        foreach (var monthDay in dayWithoutPoems)
        {
            var splitted = monthDay.Split('-');
            streamWriter2.WriteLine(
                $"- {splitted[1].TrimStart('0')} {GetRadarChartLabel($"{splitted[0]}-01").ToLower()}");
        }

        streamWriter2.Close();
    }

    public void GeneratePoemsEnByDayRadarChartDataFile()
    {
        var poemStringDates = DataEn.Seasons.SelectMany(x => x.Poems).Select(x => x.TextDate).ToList();

        var dataDict = InitMonthDayDictionary();

        foreach (var poemStringDate in poemStringDates)
        {
            var year = poemStringDate.Substring(6);
            var day = $"{poemStringDate.Substring(3, 2)}-{poemStringDate.Substring(0, 2)}";
            dataDict[day]++;
        }

        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CONTENT_ROOT_DIR_EN], "../charts/general");

        var fileName = "poems-en-day-radar.js";
        var chartId = "poemEnDayRadar";

        using var streamWriter = new StreamWriter(Path.Combine(rootDir, fileName));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartDataFileHelper.ChartType.Radar);
        chartDataFileHelper.WriteBeforeData();

        var dataLines = new List<ChartDataFileHelper.DataLine>();

        foreach (var monthDay in dataDict.Keys)
        {
            var value = dataDict[monthDay];
            dataLines.Add(new ChartDataFileHelper.DataLine(GetRadarEnChartLabel(monthDay), value
            ));
        }

        chartDataFileHelper.WriteData(dataLines, true);

        chartDataFileHelper.WriteAfterData(chartId, ["Poems by day of year"], string.Empty, string.Empty);
        streamWriter.Close();
    }

    public void GeneratePoemsOfYearByDayRadarChartDataFile(int year)
    {
        var poemStringDates = Data.Seasons.SelectMany(x => x.Poems)
            .Where(x => x.Date.Year == year).Select(x => x.TextDate)
            .ToList();

        var dataDict = InitMonthDayDictionary();

        foreach (var poemStringDate in poemStringDates)
        {
            var day = $"{poemStringDate.Substring(3, 2)}-{poemStringDate.Substring(0, 2)}";
            dataDict[day]++;
        }

        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CHART_DATA_FILES_ROOT_DIR]!);
        var fileName = $"poems-day-{year}-radar.js";
        var chartId = $"poemDay-{year}Radar";

        using var streamWriter = new StreamWriter(Path.Combine(rootDir, "taxonomy", fileName));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartDataFileHelper.ChartType.Radar);
        chartDataFileHelper.WriteBeforeData();

        var dataLines = new List<ChartDataFileHelper.DataLine>();

        foreach (var monthDay in dataDict.Keys)
        {
            var value = dataDict[monthDay];
            dataLines.Add(new ChartDataFileHelper.DataLine(GetRadarChartLabel(monthDay), value));
        }

        chartDataFileHelper.WriteData(dataLines, true);

        chartDataFileHelper.WriteAfterData(chartId, ["Poèmes selon le jour de l\\\'année"], string.Empty,
            string.Empty);
        streamWriter.Close();
    }

    public void VerifySeasonHaveCorrectPoemCount()
    {
        var seasons = Data.Seasons.Where(x => x.Poems.Count > 0).ToList();
        var seasonCount = seasons.Count;
        for (int i = 0; i < seasonCount; i++)
        {
            var season = seasons[i];
            var desc = $"[{season.Id} - {season.Name}]: {season.Poems.Count}";
            if (i < seasonCount - 1 && season.Poems.Count != 50)
            {
                throw new Exception($"Not last season. Not 50 poems for {desc}!");
            }

            if (i == seasonCount - 1 && season.Poems.Count > 50)
            {
                throw new Exception($"Last season. More than 50 poems for {desc}!");
            }
        }
    }

    public void VerifySeasonHaveCorrectWeightInPoemFile(int? seasonId)
    {
        if (seasonId == null)
        {
            VerifySeasonHaveCorrectWeightInPoemFile(Data.Seasons.Last().Id);
            VerifySeasonHaveCorrectWeightInPoemFile(Data.Seasons.Last().Id - 1);
            return;
        }

        var season = Data.Seasons.First(s => s.Id == seasonId);
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.CONTENT_ROOT_DIR]!);
        var seasonDirName = Directory.EnumerateDirectories(rootDir)
            .FirstOrDefault(x => Path.GetFileName(x).StartsWith($"{seasonId}_"));
        var poemFiles = Directory.EnumerateFiles(seasonDirName!).Where(x => !x.EndsWith("index.md"));

        foreach (var poemFile in poemFiles)
        {
            var (poem, position) = (_poemContentImporter ??= new PoemContentImporter(_configuration)).Import(poemFile);
            var poemInSeason = season.Poems.FirstOrDefault(x => x.Id == poem.Id);
            var poemIndex = season.Poems.IndexOf(poemInSeason);
            if (poemIndex != -1 && poemIndex != position)
            {
                throw new Exception($"Poem {poem.Id} should have weight {poemIndex + 1}!");
            }
        }
    }

    private static Dictionary<string, int> InitMonthDayDictionary()
    {
        var dataDict = new Dictionary<string, int>();
        for (var i = 1; i < 32; i++)
            dataDict.Add(i < 10 ? $"01-0{i}" : $"01-{i}", 0);
        for (var i = 1; i < 30; i++)
            dataDict.Add(i < 10 ? $"02-0{i}" : $"02-{i}", 0);
        for (var i = 1; i < 32; i++)
            dataDict.Add(i < 10 ? $"03-0{i}" : $"03-{i}", 0);
        for (var i = 1; i < 31; i++)
            dataDict.Add(i < 10 ? $"04-0{i}" : $"04-{i}", 0);
        for (var i = 1; i < 32; i++)
            dataDict.Add(i < 10 ? $"05-0{i}" : $"05-{i}", 0);
        for (var i = 1; i < 31; i++)
            dataDict.Add(i < 10 ? $"06-0{i}" : $"06-{i}", 0);
        for (var i = 1; i < 32; i++)
            dataDict.Add(i < 10 ? $"07-0{i}" : $"07-{i}", 0);
        for (var i = 1; i < 32; i++)
            dataDict.Add(i < 10 ? $"08-0{i}" : $"08-{i}", 0);
        for (var i = 1; i < 31; i++)
            dataDict.Add(i < 10 ? $"09-0{i}" : $"09-{i}", 0);
        for (var i = 1; i < 32; i++)
            dataDict.Add(i < 10 ? $"10-0{i}" : $"10-{i}", 0);
        for (var i = 1; i < 31; i++)
            dataDict.Add(i < 10 ? $"11-0{i}" : $"11-{i}", 0);
        for (var i = 1; i < 32; i++)
            dataDict.Add(i < 10 ? $"12-0{i}" : $"12-{i}", 0);
        return dataDict;
    }

    public void GeneratePoemMetricBarAndPieChartDataFile(int? seasonId, bool useDetailedMetric)
    {
        var isGeneral = seasonId is null;
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CHART_DATA_FILES_ROOT_DIR]!);
        var fileName = isGeneral ? "poems-verse-length-pie.js" : "poems-verse-length-bar.js";
        var subDir = isGeneral ? "general" : $"season-{seasonId}";
        var chartId = isGeneral ? "poemVerseLengthPie" : $"season{seasonId}VerseLengthBar";
        using var streamWriter = new StreamWriter(Path.Combine(rootDir, subDir, fileName));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter,
            isGeneral ? ChartDataFileHelper.ChartType.Pie : ChartDataFileHelper.ChartType.Bar, 1);
        chartDataFileHelper.WriteBeforeData();
        var regularMetricData = new Dictionary<int, int>();
        var variableMetricData = new Dictionary<string, int>();
        var nbUndefinedVerseLength = 0;
        var poems = seasonId != null
            ? Data.Seasons.First(x => x.Id == seasonId).Poems
            : Data.Seasons.SelectMany(x => x.Poems);

        foreach (var poem in poems)
        {
            if (string.IsNullOrEmpty(poem.VerseLength))
            {
                nbUndefinedVerseLength++;
            }
            else if (poem.HasVariableMetric)
            {
                if (useDetailedMetric)
                {
                    foreach (var metric in poem.VerseLength.Split(','))
                    {
                        // Standard metrics defined in variable metrics go to general metric pie chart
                        if (!int.TryParse(metric.Trim(), out var verseLength)) continue;
                        if (regularMetricData.TryGetValue(verseLength, out _))
                        {
                            regularMetricData[verseLength]++;
                        }
                        else
                        {
                            regularMetricData[verseLength] = 1;
                        }
                    }
                }

                // Detailed variable metric go to general metric bar chart
                if (variableMetricData.TryGetValue(poem.DetailedMetric, out _))
                {
                    variableMetricData[poem.DetailedMetric]++;
                }
                else
                {
                    variableMetricData[poem.DetailedMetric] = 1;
                }
            }
            else
            {
                var verseLength = int.Parse(poem.VerseLength);
                if (regularMetricData.TryGetValue(verseLength, out _))
                {
                    regularMetricData[verseLength]++;
                }
                else
                {
                    regularMetricData[verseLength] = 1;
                }
            }
        }

        var regularMetricRange = regularMetricData.Keys.Order().ToList();
        var variableMetricRange = variableMetricData.Keys.Order().ToList();

        var regularMetricChartData = new List<ChartDataFileHelper.DataLine>();
        var variableMetricChartData = new List<ChartDataFileHelper.ColoredDataLine>();

        foreach (var metricValue in regularMetricRange)
        {
            regularMetricChartData.Add(new ChartDataFileHelper.DataLine(
                metricValue.ToString(), regularMetricData[metricValue]));
        }

        foreach (var verseLength in variableMetricRange)
        {
            variableMetricChartData.Add(new ChartDataFileHelper.ColoredDataLine
                (verseLength, variableMetricData[verseLength], "rgba(72, 149, 239, 1)"));
        }

        var undefinedVerseLengthChartData = new ChartDataFileHelper.ColoredDataLine
        ("Pas de données pour l\\'instant", nbUndefinedVerseLength, "rgb(211, 211, 211)"
        );

        // General pie chart or Season's bar chart
        var dataLines = new List<ChartDataFileHelper.DataLine>();

        if (isGeneral)
        {
            var metrics = _configuration.GetSection(Constants.METRIC_SETTINGS).Get<MetricSettings>().Metrics;
            var coloredDataLines = new List<ChartDataFileHelper.ColoredDataLine>();

            foreach (var metricValue in regularMetricRange)
            {
                var term = metricValue == 1 ? "syllabe" : "syllabes";
                coloredDataLines.Add(new ChartDataFileHelper.ColoredDataLine($"{metricValue} {term}",
                    regularMetricData[metricValue], metrics.First(m => m.Length == metricValue).Color));
            }

            chartDataFileHelper.WriteData(coloredDataLines, true);

            chartDataFileHelper.WriteAfterData(chartId, ["Poèmes"]);
            streamWriter.Close();
        }
        else
        {
            dataLines.AddRange(regularMetricChartData);
            dataLines.AddRange(variableMetricChartData);
            if (nbUndefinedVerseLength > 0)
                dataLines.Add(undefinedVerseLengthChartData);

            chartDataFileHelper.WriteData(dataLines, true);

            chartDataFileHelper.WriteAfterData(chartId, ["Poèmes"],
                customScalesOptions: "scales: { y: { ticks: { stepSize: 1 } } }");
            streamWriter.Close();
        }

        // Variable metric general bar chart
        if (isGeneral)
        {
            fileName = "metrique_variable-bar.js";
            chartId = "metrique_variableBar";
            using var streamWriter2 = new StreamWriter(Path.Combine(rootDir, "general", fileName));
            var chartDataFileHelper2 = new ChartDataFileHelper(streamWriter2, ChartDataFileHelper.ChartType.Bar, 1);
            chartDataFileHelper2.WriteBeforeData();

            dataLines = [];
            dataLines.AddRange(variableMetricChartData);

            chartDataFileHelper2.WriteData(dataLines, true);

            chartDataFileHelper2.WriteAfterData(chartId, ["Poèmes"],
                customScalesOptions: "scales: { y: { ticks: { stepSize: 1 } } }");
            streamWriter2.Close();
        }
    }

    public void GeneratePoemIntensityPieChartDataFile()
    {
        var dataDict = new Dictionary<string, int>();

        var fullDates = Data.Seasons.SelectMany(x => x.Poems).Select(x => x.TextDate)
            .Where(x => x != "01.01.1994").ToList();

        // Add EN poems
        fullDates.AddRange(DataEn.Seasons.SelectMany(x => x.Poems).Select(x => x.TextDate));

        foreach (var fullDate in fullDates)
        {
            if (!dataDict.TryAdd(fullDate, 1))
            {
                dataDict[fullDate]++;
            }
        }

        var intensityDict = new Dictionary<int, int>();

        foreach (var data in dataDict)
        {
            var value = data.Value;
            if (!intensityDict.TryAdd(value, 1))
            {
                intensityDict[value]++;
            }
        }

        var dataLines = new List<ChartDataFileHelper.DataLine>();
        var orderedIntensitiesKeys = intensityDict.Keys.Order();
        var baseColor = "rgba(72, 149, 239, {0})";
        var baseAlpha = 0.5;
        foreach (var key in orderedIntensitiesKeys)
        {
            if (key == 0) continue;
            dataLines.Add(new ChartDataFileHelper.ColoredDataLine($"{key} {(key == 1 ? "poème" : "poèmes")}",
                intensityDict[key],
                string.Format(baseColor,
                    (baseAlpha + 0.1 * (key - 1)).ToString(new NumberFormatInfo
                        { NumberDecimalSeparator = ".", NumberDecimalDigits = 1 }))));
        }

        var fileName = "poem-intensity-pie.js";
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CHART_DATA_FILES_ROOT_DIR]!);
        using var streamWriter = new StreamWriter(Path.Combine(rootDir, "general", fileName));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartDataFileHelper.ChartType.Pie);
        chartDataFileHelper.WriteBeforeData();
        chartDataFileHelper.WriteData(dataLines, true);
        chartDataFileHelper.WriteAfterData("poemIntensityPie", ["Les jours de création sont-ils intenses ?"]);
        streamWriter.Close();

        // Most intense days content file
        var intensityKeys = intensityDict.Keys.OrderDescending().Where(x => x > 2);
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.CONTENT_ROOT_DIR]!,
            "../includes/most_intense_days.md");
        var streamWriter2 = new StreamWriter(filePath);

        streamWriter2.WriteLine("+++");
        streamWriter2.WriteLine("title = \"Les jours les plus intenses\"");
        streamWriter2.WriteLine("+++");

        foreach (var key in intensityKeys)
        {
            streamWriter2.WriteLine($"- {key} poèmes en un jour :");
            var matchingIntensities = dataDict.Where(x => x.Value == key).Select(x => x.Key);
            var years = matchingIntensities.Select(x => x.Substring(6)).Distinct();

            foreach (var year in years)
            {
                var dates = matchingIntensities.Where(x => x.Substring(6) == year).Select(x => x.ToDateTime()).Order();
                streamWriter2.Write($"  - {year} : ");
                streamWriter2.WriteLine(string.Join(", ", dates.Select(x => x.ToString("D"))));
            }
        }

        streamWriter2.Close();
    }

    public void GeneratePoemByDayOfWeekPieChartDataFile()
    {
        var dataDict = new Dictionary<int, int>();

        var dayOfWeekData = Data.Seasons.SelectMany(x => x.Poems).Where(x => x.TextDate != "01.01.1994")
            .Select(x => x.Date.DayOfWeek).ToList();

        // Add EN poems
        dayOfWeekData.AddRange(DataEn.Seasons.SelectMany(x => x.Poems).Select(x => x.Date.DayOfWeek));

        foreach (var dayOfWeek in dayOfWeekData)
        {
            if (!dataDict.TryAdd((int)dayOfWeek, 1))
            {
                dataDict[(int)dayOfWeek]++;
            }
        }

        var dataLines = new List<ChartDataFileHelper.DataLine>();
        var baseColor = "rgba(72, 149, 239, {0})";
        var baseAlpha = 0.2;
        int[] daysOfWeek = [1, 2, 3, 4, 5, 6, 0];
        foreach (var key in daysOfWeek)
        {
            dataLines.Add(new ChartDataFileHelper.ColoredDataLine(
                key == 1 ? "Lundi" :
                key == 2 ? "Mardi" :
                key == 3 ? "Mercredi" :
                key == 4 ? "Jeudi" :
                key == 5 ? "Vendredi" :
                key == 6 ? "Samedi" : "Dimanche",
                dataDict[key],
                string.Format(baseColor,
                    (baseAlpha + 0.1 * (key == 0 ? 7 : key)).ToString(new NumberFormatInfo
                        { NumberDecimalSeparator = ".", NumberDecimalDigits = 1 }))));
        }

        var fileName = "poem-dayofweek-pie.js";
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CHART_DATA_FILES_ROOT_DIR]!);
        using var streamWriter = new StreamWriter(Path.Combine(rootDir, "general", fileName));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartDataFileHelper.ChartType.Pie);
        chartDataFileHelper.WriteBeforeData();
        chartDataFileHelper.WriteData(dataLines, true);
        chartDataFileHelper.WriteAfterData("poemDayOfWeekPie", ["Par jour de la semaine"]);
        streamWriter.Close();
    }

    public void GenerateEnPoemByDayOfWeekPieChartDataFile()
    {
        var dataDict = new Dictionary<int, int>();

        var dayOfWeekData = DataEn.Seasons.SelectMany(x => x.Poems).Select(x => x.Date.DayOfWeek);

        foreach (var dayOfWeek in dayOfWeekData)
        {
            if (!dataDict.TryAdd((int)dayOfWeek, 1))
            {
                dataDict[(int)dayOfWeek]++;
            }
        }

        var dataLines = new List<ChartDataFileHelper.DataLine>();
        var baseColor = "rgba(72, 149, 239, {0})";
        var baseAlpha = 0.2;
        int[] daysOfWeek = [1, 2, 3, 4, 5, 6, 0];
        foreach (var key in daysOfWeek)
        {
            dataLines.Add(new ChartDataFileHelper.ColoredDataLine(
                key == 1 ? "Monday" :
                key == 2 ? "Tuesday" :
                key == 3 ? "Wednesday" :
                key == 4 ? "Thursday" :
                key == 5 ? "Friday" :
                key == 6 ? "Saturday" : "Sunday",
                dataDict[key],
                string.Format(baseColor,
                    (baseAlpha + 0.1 * (key == 0 ? 7 : key)).ToString(new NumberFormatInfo
                        { NumberDecimalSeparator = ".", NumberDecimalDigits = 1 }))));
        }

        var fileName = "poem-en-dayofweek-pie.js";
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CONTENT_ROOT_DIR_EN]);
        using var streamWriter = new StreamWriter(Path.Combine(rootDir, "../charts/general", fileName));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartDataFileHelper.ChartType.Pie);
        chartDataFileHelper.WriteBeforeData();
        chartDataFileHelper.WriteData(dataLines, true);
        chartDataFileHelper.WriteAfterData("poemEnDayOfWeekPie", ["By day of week"]);
        streamWriter.Close();
    }

    public void GenerateOverSeasonsChartDataFile(string? storageSubCategory, string? storageCategory,
        bool forAcrostiche = false, bool forSonnet = false, bool forPantoun = false, bool forVariableMetric = false,
        bool forRefrain = false, int? forMetric = null)
    {
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CHART_DATA_FILES_ROOT_DIR]!);
        var fileName = string.Empty;

        var chartId = string.Empty;
        var borderColor = "rgba(72, 149, 239, 1)";

        if (storageSubCategory != null)
        {
            fileName = $"poems-{storageSubCategory.UnaccentedCleaned()}-bar.js";
            chartId = $"poems-{storageSubCategory.UnaccentedCleaned()}Bar";
            borderColor = _configuration.GetSection(Constants.STORAGE_SETTINGS).Get<StorageSettings>().Categories
                .SelectMany(x => x.Subcategories).FirstOrDefault(x => x.Name == storageSubCategory).Color;

            switch (borderColor)
            {
                // Use some not too light colors
                case "rgba(254, 231, 240, 1)":
                    borderColor = "rgba(255, 194, 209, 1)";
                    break;
                case "rgba(247, 235, 253, 1)":
                    borderColor = "rgba(234, 191, 250, 1)";
                    break;
                case "rgba(244, 254, 254, 1)":
                    borderColor = "rgba(119, 181, 254, 1)";
                    break;
            }
        }
        else if (storageCategory != null)
        {
            fileName = $"poems-{storageCategory.UnaccentedCleaned()}-bar.js";
            chartId = $"poems-{storageCategory.UnaccentedCleaned()}Bar";
            borderColor = _configuration.GetSection(Constants.STORAGE_SETTINGS).Get<StorageSettings>().Categories
                .FirstOrDefault(x => x.Name == storageCategory).Color;
        }
        else if (forAcrostiche)
        {
            fileName = $"poems-acrostiche-bar.js";
            chartId = $"poems-acrosticheBar";
        }
        else if (forSonnet)
        {
            fileName = $"poems-sonnet-bar.js";
            chartId = $"poems-sonnetBar";
        }
        else if (forPantoun)
        {
            fileName = $"poems-pantoun-bar.js";
            chartId = $"poems-pantounBar";
        }
        else if (forVariableMetric)
        {
            fileName = $"poems-metrique_variable-bar.js";
            chartId = $"poems-metrique_variableBar";
        }
        else if (forRefrain)
        {
            fileName = $"poems-refrain-bar.js";
            chartId = $"poems-refrainBar";
        }
        else if (forMetric is not null)
        {
            fileName = $"poems-metric-{forMetric}-bar.js";
            chartId = $"poems-metric{forMetric}Bar";
        }

        var backgroundColor = borderColor.Replace("1)", "0.5)");
        if (forVariableMetric)
        {
            // For this one, same color to look nice below other bar chart
            backgroundColor = borderColor;
        }

        using var streamWriter = new StreamWriter(Path.Combine(rootDir, "taxonomy", fileName));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartDataFileHelper.ChartType.Bar);
        chartDataFileHelper.WriteBeforeData();

        var dataLines = new List<ChartDataFileHelper.DataLine>();

        foreach (var season in Data.Seasons.Where(x => x.Poems.Count > 0))
        {
            var poemCount = 0;
            if (storageSubCategory != null)
            {
                poemCount = season.Poems.Count(x =>
                    x.Categories.Any(x => x.SubCategories.Contains(storageSubCategory)));
            }
            else if (storageCategory != null)
            {
                poemCount = season.Poems.Count(x => x.Categories.Any(x => x.Name == storageCategory));
            }
            else if (forAcrostiche)
            {
                poemCount = season.Poems.Count(x => x.Acrostiche != null || x.DoubleAcrostiche != null);
            }
            else if (forSonnet)
            {
                poemCount = season.Poems.Count(x => x.IsSonnet);
            }
            else if (forPantoun)
            {
                poemCount = season.Poems.Count(x => x.IsPantoun);
            }
            else if (forVariableMetric)
            {
                poemCount = season.Poems.Count(x => x.HasVariableMetric);
            }
            else if (forRefrain)
            {
                poemCount = season.Poems.Count(x => x.ExtraTags.Contains("refrain"));
            }
            else if (forMetric is not null)
            {
                poemCount = season.Poems.Count(x => x.HasMetric(forMetric.Value));
            }

            dataLines.Add(new ChartDataFileHelper.ColoredDataLine($"{season.EscapedTitleForChartsWithYears}",
                poemCount,
                backgroundColor));
        }

        chartDataFileHelper.WriteData(dataLines, true);

        chartDataFileHelper.WriteAfterData(chartId, ["Poèmes au fil des saisons"],
            customScalesOptions: "scales: { y: { ticks: { stepSize: 1 } } }");
        streamWriter.Close();
    }

    public void GeneratePoemIntervalBarChartDataFile(int? seasonId)
    {
        var frDatesList =
            (seasonId == null ? Data.Seasons.SelectMany(x => x.Poems) : Data.Seasons.First(x => x.Id == seasonId).Poems)
            .Where(x => x.TextDate != "01.01.1994")
            .Select(x => x.Date);

        // Add EN poems
        var enDatesList = (seasonId == null
                ? DataEn.Seasons.SelectMany(x => x.Poems)
                : DataEn.Seasons.FirstOrDefault(x => x.Id == seasonId)?.Poems)?
            .Select(x => x.Date);

        var datesList = new List<DateTime>();
        datesList.AddRange(frDatesList);
        if (enDatesList != null)
            datesList.AddRange(enDatesList);
        datesList.Sort();

        var intervalDict = new Dictionary<DateTime, int>(); // end date, duration
        var intervalLengthDict = new Dictionary<int, int>(); // duration, occurrence
        var seriesDict = new Dictionary<DateTime, int>(); // end date, duration

        int dateCount = datesList.Count();
        for (var i = 1; i < dateCount; i++)
        {
            var current = datesList[i];
            var previous = datesList[i - 1];
            var dayDiff = (int)(current - previous).TotalDays;

            if (!intervalLengthDict.TryAdd(dayDiff, 1))
            {
                intervalLengthDict[dayDiff]++;
            }

            intervalDict.TryAdd(current, dayDiff);

            seriesDict.TryAdd(previous, 1);

            if (dayDiff != 1) continue;

            var duration = seriesDict[previous];
            seriesDict.Remove(previous);
            seriesDict[current] = ++duration;
        }

        // Interval length charts

        var dataLines = new List<ChartDataFileHelper.ColoredDataLine>();
        var orderedIntervalKeys = intervalLengthDict.Keys.Order().ToList();
        var zeroDayColor = "rgba(72, 149, 239, 1)";
        var oneDayColor = "rgba(72, 149, 239, 0.9)";
        var upToSevenDayColor = "rgba(72, 149, 239, 0.7)";
        var upToOneMonthColor = "rgba(72, 149, 239, 0.5)";
        var upToThreeMonthsColor = "rgba(72, 149, 239, 0.3)";
        var upToOneYearColor = "rgba(72, 149, 239, 0.2)";
        var moreThanOneYearColor = "rgba(72, 149, 239, 0.1)";
        var moreThanOneMonthCount = 0;
        var moreThanThreeMonthsCount = 0;
        var moreThanOneYearCount = 0;
        foreach (var key in orderedIntervalKeys)
        {
            if (key == 0)
            {
                dataLines.Add(
                    new ChartDataFileHelper.ColoredDataLine("Moins d\\'un jour", intervalLengthDict[key],
                        zeroDayColor));
            }
            else if (key == 1)
            {
                dataLines.Add(new ChartDataFileHelper.ColoredDataLine("Un jour", intervalLengthDict[key], oneDayColor));
            }
            else if (key < 8)
            {
                dataLines.Add(
                    new ChartDataFileHelper.ColoredDataLine($"{key}j", intervalLengthDict[key], upToSevenDayColor));
            }
            else if (key < 31)
            {
                dataLines.Add(new ChartDataFileHelper.ColoredDataLine($"{key}j", intervalLengthDict[key],
                    upToOneMonthColor));
            }
            else if (key < 91)
            {
                moreThanOneMonthCount++;
            }
            else if (key < 366)
            {
                moreThanThreeMonthsCount++;
            }
            else
            {
                moreThanOneYearCount++;
            }
        }

        if (moreThanOneMonthCount > 0)
            dataLines.Add(new ChartDataFileHelper.ColoredDataLine("Entre un et trois mois", moreThanOneMonthCount,
                upToThreeMonthsColor));

        if (moreThanThreeMonthsCount > 0)
            dataLines.Add(new ChartDataFileHelper.ColoredDataLine("Entre trois mois et un an", moreThanThreeMonthsCount,
                upToOneYearColor));

        if (moreThanOneYearCount > 0)
            dataLines.Add(new ChartDataFileHelper.ColoredDataLine("Plus d\\'un an", moreThanOneYearCount,
                moreThanOneYearColor));

        var fileName = "poem-interval-bar.js";
        var subDir = seasonId != null ? $"season-{seasonId}" : "general";
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CHART_DATA_FILES_ROOT_DIR]!);
        using var streamWriter = new StreamWriter(Path.Combine(rootDir, subDir, fileName));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartDataFileHelper.ChartType.Bar);
        chartDataFileHelper.WriteBeforeData();
        chartDataFileHelper.WriteData(dataLines, true);
        chartDataFileHelper.WriteAfterData(seasonId == null ? "poemIntervalBar" : $"season{seasonId}PoemIntervalBar",
            ["Fréquence"],
            customScalesOptions: seasonId == null ? string.Empty : "scales: { y: { ticks: { stepSize: 1 } } }");
        streamWriter.Close();

        if (seasonId.HasValue) return;

        // Longest intervals content file

        var longestIntervalKeys = orderedIntervalKeys.OrderDescending().ToList();
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.CONTENT_ROOT_DIR!],
            "../includes/longest_intervals.md");
        var streamWriter3b = new StreamWriter(filePath);

        streamWriter3b.WriteLine("+++");
        streamWriter3b.WriteLine("title = \"Les plus longs intervalles\"");
        streamWriter3b.WriteLine("+++");

        var moreThanOneYearDates = new List<KeyValuePair<DateTime, DateTime>>();
        var moreThanThreeMonthsDates = new List<KeyValuePair<DateTime, DateTime>>();

        foreach (var key in longestIntervalKeys)
        {
            if (key < 91) break;
            var matchingSeries = intervalDict.Where(x => x.Value == key).ToList();
            foreach (var pair in matchingSeries)
            {
                if (key < 366)
                {
                    moreThanThreeMonthsDates.Add(
                        new KeyValuePair<DateTime, DateTime>(pair.Key.AddDays(-key), pair.Key));
                }
                else
                {
                    moreThanOneYearDates.Add(new KeyValuePair<DateTime, DateTime>(pair.Key.AddDays(-key), pair.Key));
                }
            }
        }

        streamWriter3b.WriteLine($"- Plus d'un an, du plus long au plus court :");

        foreach (var data in moreThanOneYearDates)
        {
            streamWriter3b.WriteLine(
                $"  - Du {data.Key.ToString("dd.MM.yyyy")} au {data.Value.ToString("dd.MM.yyyy")}");
        }

        streamWriter3b.WriteLine($"- Plus de trois mois, du plus long au plus court :");

        foreach (var data in moreThanThreeMonthsDates)
        {
            streamWriter3b.WriteLine(
                $"  - Du {data.Key.ToString("dd.MM.yyyy")} au {data.Value.ToString("dd.MM.yyyy")}");
        }

        streamWriter3b.Close();

        // Series length chart (general chart)

        var seriesLengthDict = new Dictionary<int, int>();
        foreach (var seriesLength in seriesDict.Values)
        {
            if (!seriesLengthDict.TryAdd(seriesLength, 1))
            {
                seriesLengthDict[seriesLength]++;
            }
        }

        var seriesDataLines = new List<ChartDataFileHelper.ColoredDataLine>();
        var sortedKeys = seriesLengthDict.Keys.Order().ToList();

        foreach (var key in sortedKeys.Skip(1))
        {
            seriesDataLines.Add(new ChartDataFileHelper.ColoredDataLine($"{key}j", seriesLengthDict[key],
                "rgba(72, 149, 239, 1)"));
        }

        fileName = "poem-series-bar.js";
        subDir = "general";
        using var streamWriter2 = new StreamWriter(Path.Combine(rootDir, subDir, fileName));
        var chartDataFileHelper2 = new ChartDataFileHelper(streamWriter2, ChartDataFileHelper.ChartType.Bar);
        chartDataFileHelper2.WriteBeforeData();
        chartDataFileHelper2.WriteData(seriesDataLines, true);
        chartDataFileHelper2.WriteAfterData("poemSeriesBar",
            ["Séries"],
            customScalesOptions: seasonId == null ? string.Empty : "scales: { y: { ticks: { stepSize: 1 } } }");
        streamWriter2.Close();

        // longest series content file

        var longestSeriesKeys = sortedKeys.OrderDescending().Where(x => x > 7);
        filePath = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.CONTENT_ROOT_DIR]!,
            "../includes/longest_series.md");
        var streamWriter3 = new StreamWriter(filePath);

        streamWriter3.WriteLine("+++");
        streamWriter3.WriteLine("title = \"Les plus longues séries\"");
        streamWriter3.WriteLine("+++");

        foreach (var key in longestSeriesKeys)
        {
            var matchingSeries = seriesDict.Where(x => x.Value == key);
            streamWriter3.WriteLine($"- {key} jours :");
            foreach (var pair in matchingSeries)
            {
                streamWriter3.WriteLine(
                    $"  - Du {pair.Key.AddDays(-key).ToString("dd.MM.yyyy")} au {pair.Key.ToString("dd.MM.yyyy")}");
            }
        }

        streamWriter3.Close();
    }

    public void GeneratePoemCountFile()
    {
        var poemCount = Data.Seasons.Select(x => x.Poems.Count).Sum();
        var poemCountFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CONTENT_ROOT_DIR]!, "../../common", "poem_count.md");
        File.WriteAllText(poemCountFilePath, poemCount.ToString());

        // And for variable verse
        var variableMetricPoemCount = Data.Seasons.SelectMany(x => x.Poems.Where(x => x.HasVariableMetric)).Count();
        var variableMetricPoemCountFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CONTENT_ROOT_DIR]!, "../../common", "variable_metric_poem_count.md");
        File.WriteAllText(variableMetricPoemCountFilePath, variableMetricPoemCount.ToString());
    }

    public void GeneratePoemEnCountFile()
    {
        var poemCount = DataEn.Seasons.Select(x => x.Poems.Count).Sum();
        var poemCountFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CONTENT_ROOT_DIR]!, "../../common", "poem_count_en.md");
        File.WriteAllText(poemCountFilePath, poemCount.ToString());
    }

    public void GeneratePoemLengthByVerseLengthBubbleChartDataFile()
    {
        var poems = Data.Seasons.SelectMany(x => x.Poems);
        var poemLengthByVerseLength = new Dictionary<KeyValuePair<int, int>, int>();
        var variableMetric = new Dictionary<int, int>();
        foreach (var poem in poems)
        {
            var poemLength = poem.VersesCount;
            if (poem.HasVariableMetric)
            {
                if (!variableMetric.TryAdd(poemLength, 1))
                {
                    variableMetric[poemLength]++;
                }

                continue;
            }

            var key = new KeyValuePair<int, int>(int.Parse(poem.VerseLength!), poemLength);
            if (!poemLengthByVerseLength.TryAdd(key, 1))
            {
                poemLengthByVerseLength[key]++;
            }
        }

        // Find max value
        var maxValue = poemLengthByVerseLength.Values.Max();

        var fileName = "poem-length-by-verse-length.js";
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CHART_DATA_FILES_ROOT_DIR]!);
        using var streamWriter = new StreamWriter(Path.Combine(rootDir, "general", fileName));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartDataFileHelper.ChartType.Bubble, 4);
        chartDataFileHelper.WriteBeforeData();

        var firstQuartileDataLines = new List<ChartDataFileHelper.BubbleChartDataLine>();
        var secondQuartileDataLines = new List<ChartDataFileHelper.BubbleChartDataLine>();
        var thirdQuartileDataLines = new List<ChartDataFileHelper.BubbleChartDataLine>();
        var fourthQuartileDataLines = new List<ChartDataFileHelper.BubbleChartDataLine>();

        foreach (var dataKey in poemLengthByVerseLength.Keys)
        {
            AddDataLine(dataKey.Key, dataKey.Value, poemLengthByVerseLength[dataKey],
                [firstQuartileDataLines, secondQuartileDataLines, thirdQuartileDataLines, fourthQuartileDataLines],
                maxValue, 30);
        }

        foreach (var dataKey in variableMetric.Keys)
        {
            AddDataLine(0, dataKey, variableMetric[dataKey],
                [firstQuartileDataLines, secondQuartileDataLines, thirdQuartileDataLines, fourthQuartileDataLines],
                maxValue, 30);
        }

        chartDataFileHelper.WriteData(firstQuartileDataLines, false);
        chartDataFileHelper.WriteData(secondQuartileDataLines, false);
        chartDataFileHelper.WriteData(thirdQuartileDataLines, false);
        chartDataFileHelper.WriteData(fourthQuartileDataLines, true);
        chartDataFileHelper.WriteAfterData("poemLengthByVerseLength",
        [
            "Premier quartile",
            "Deuxième quartile",
            "Troisième quartile",
            "Quatrième quartile"
        ], chartXAxisTitle: "Métrique (0 = variable)", chartYAxisTitle: "Nombre de vers", yAxisStep: 2);
        streamWriter.Close();
    }

    public void GenerateOverSeasonsMetricLineChartDataFile()
    {
        var dataDict = FillMetricDataDict(out var xLabels);

        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CHART_DATA_FILES_ROOT_DIR]!);

        var metrics = _configuration.GetSection(Constants.METRIC_SETTINGS).Get<MetricSettings>().Metrics;

        var fileName = "poems-verseLength-line.js";

        using var streamWriter = new StreamWriter(Path.Combine(rootDir, "general", fileName));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartDataFileHelper.ChartType.Line, 14);
        chartDataFileHelper.WriteBeforeData();

        var oneFootDataLines =
            new ChartDataFileHelper.LineChartDataLine("1 syllabe", dataDict[1],
                metrics.First(x => x.Length == 1).Color);
        var twoFeetDataLines =
            new ChartDataFileHelper.LineChartDataLine("2 syllabes", dataDict[2],
                metrics.First(x => x.Length == 2).Color);
        var threeFeetDataLines =
            new ChartDataFileHelper.LineChartDataLine("3 syllabes", dataDict[3],
                metrics.First(x => x.Length == 3).Color);
        var fourFeetDataLines =
            new ChartDataFileHelper.LineChartDataLine("4 syllabes", dataDict[4],
                metrics.First(x => x.Length == 4).Color);
        var fiveFeetDataLines =
            new ChartDataFileHelper.LineChartDataLine("5 syllabes", dataDict[5],
                metrics.First(x => x.Length == 5).Color);
        var sixFeetDataLines =
            new ChartDataFileHelper.LineChartDataLine("6 syllabes", dataDict[6],
                metrics.First(x => x.Length == 6).Color);
        var sevenFeetDataLines =
            new ChartDataFileHelper.LineChartDataLine("7 syllabes", dataDict[7],
                metrics.First(x => x.Length == 7).Color);
        var eightFeetDataLines =
            new ChartDataFileHelper.LineChartDataLine("8 syllabes", dataDict[8],
                metrics.First(x => x.Length == 8).Color);
        var nineFeetDataLines =
            new ChartDataFileHelper.LineChartDataLine("9 syllabes", dataDict[9],
                metrics.First(x => x.Length == 9).Color);
        var tenFeetDataLines =
            new ChartDataFileHelper.LineChartDataLine("10 syllabes", dataDict[10],
                metrics.First(x => x.Length == 10).Color);
        var elevenFeetDataLines =
            new ChartDataFileHelper.LineChartDataLine("11 syllabes", dataDict[11],
                metrics.First(x => x.Length == 11).Color);
        var twelveFeetDataLines =
            new ChartDataFileHelper.LineChartDataLine("12 syllabes", dataDict[12],
                metrics.First(x => x.Length == 12).Color);
        var fourteenFeetDataLines =
            new ChartDataFileHelper.LineChartDataLine("14 syllabes", dataDict[14],
                metrics.First(x => x.Length == 14).Color);

        chartDataFileHelper.WriteData(oneFootDataLines);
        chartDataFileHelper.WriteData(twoFeetDataLines);
        chartDataFileHelper.WriteData(threeFeetDataLines);
        chartDataFileHelper.WriteData(fourFeetDataLines);
        chartDataFileHelper.WriteData(fiveFeetDataLines);
        chartDataFileHelper.WriteData(sixFeetDataLines);
        chartDataFileHelper.WriteData(sevenFeetDataLines);
        chartDataFileHelper.WriteData(eightFeetDataLines);
        chartDataFileHelper.WriteData(nineFeetDataLines);
        chartDataFileHelper.WriteData(tenFeetDataLines);
        chartDataFileHelper.WriteData(elevenFeetDataLines);
        chartDataFileHelper.WriteData(twelveFeetDataLines);
        chartDataFileHelper.WriteData(fourteenFeetDataLines);

        chartDataFileHelper.WriteAfterData("poemsVerseLengthLine",
            [
                "1 syllabe",
                "2 syllabes",
                "3 syllabes",
                "4 syllabes",
                "5 syllabes",
                "6 syllabes",
                "7 syllabes",
                "8 syllabes",
                "9 syllabes",
                "10 syllabes",
                "11 syllabes",
                "12 syllabes",
                "14 syllabes"
            ], chartYAxisTitle: "Métrique", chartXAxisTitle: "Au fil des Saisons",
            xLabels: xLabels.ToArray(), stack: "stack0");
        streamWriter.Close();
    }

    public void GenerateCategoriesBubbleChartDataFile()
    {
        var poems = Data.Seasons.SelectMany(x => x.Poems);
        var categoriesDataDictionary = new Dictionary<KeyValuePair<string, string>, int>();
        var xAxisLabels = new SortedSet<string>();
        var yAxisLabels = new SortedSet<string>();

        foreach (var poem in poems)
        {
            FillCategoriesBubbleChartDataDict(categoriesDataDictionary, xAxisLabels, yAxisLabels, poem);
        }

        // Find max value
        var maxValue = categoriesDataDictionary.Values.Max();

        var fileName = "associated-categories.js";
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CHART_DATA_FILES_ROOT_DIR]!);
        using var streamWriter = new StreamWriter(Path.Combine(rootDir, "taxonomy", fileName));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartDataFileHelper.ChartType.Bubble, 4);
        chartDataFileHelper.WriteBeforeData();

        var firstQuartileDataLines = new List<ChartDataFileHelper.BubbleChartDataLine>();
        var secondQuartileDataLines = new List<ChartDataFileHelper.BubbleChartDataLine>();
        var thirdQuartileDataLines = new List<ChartDataFileHelper.BubbleChartDataLine>();
        var fourthQuartileDataLines = new List<ChartDataFileHelper.BubbleChartDataLine>();

        // Get the values for x-axis
        var xAxisKeys = categoriesDataDictionary.Keys.Select(x => x.Key).Distinct().ToList();
        xAxisKeys.Sort();

        var yAxisKeys = categoriesDataDictionary.Keys.Select(x => x.Value).Distinct().ToList();
        yAxisKeys.Sort();

        foreach (var dataKey in categoriesDataDictionary.Keys)
        {
            var xAxisValue = xAxisKeys.IndexOf(dataKey.Key);
            var yAxisValue = yAxisKeys.IndexOf(dataKey.Value);
            AddDataLine(xAxisValue, yAxisValue, categoriesDataDictionary[dataKey],
                [firstQuartileDataLines, secondQuartileDataLines, thirdQuartileDataLines, fourthQuartileDataLines],
                maxValue, 10);
        }

        chartDataFileHelper.WriteData(firstQuartileDataLines, false);
        chartDataFileHelper.WriteData(secondQuartileDataLines, false);
        chartDataFileHelper.WriteData(thirdQuartileDataLines, false);
        chartDataFileHelper.WriteData(fourthQuartileDataLines, true);
        chartDataFileHelper.WriteAfterData("associatedCategories",
            [
                "Premier quartile",
                "Deuxième quartile",
                "Troisième quartile",
                "Quatrième quartile"
            ],
            customScalesOptions: chartDataFileHelper.FormatCategoriesBubbleChartLabelOptions(xAxisLabels.ToList(),
                yAxisLabels.ToList()));
        streamWriter.Close();

        // Automatic listing of topmost associations
        GenerateTopMostAssociatedCategoriesListing(categoriesDataDictionary, maxValue);
    }

    private void GenerateTopMostAssociatedCategoriesListing(Dictionary<KeyValuePair<string, string>, int> dataDict,
        int maxValue)
    {
        var sortedDict = dataDict.OrderByDescending(x => x.Value).Take(10).ToList();

        var outFile = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CONTENT_ROOT_DIR]!, "../includes", "associated_categories.md");
        using var streamWriter = new StreamWriter(outFile);

        streamWriter.WriteLine("+++");
        streamWriter.WriteLine("title = \"Associations privilégiées\"");
        streamWriter.WriteLine("+++");
        foreach (var (key, _) in sortedDict)
        {
            streamWriter.WriteLine(
                $"- {key.Key.MarkdownLink("categories")} et {key.Value.MarkdownLink("categories")}");
        }

        streamWriter.Close();
    }

    public void GenerateCategoryMetricBubbleChartDataFile()
    {
        var poems = Data.Seasons.SelectMany(x => x.Poems);
        var categoryMetricDataDictionary = new Dictionary<KeyValuePair<string, int>, int>();
        var xAxisLabels = new SortedSet<string>();

        foreach (var poem in poems)
        {
            FillCategoryMetricBubbleChartDataDict(categoryMetricDataDictionary, xAxisLabels, poem);
        }

        // Find max value
        var maxValue = categoryMetricDataDictionary.Values.Max();

        var fileName = "category-metric.js";
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CHART_DATA_FILES_ROOT_DIR]!);
        using var streamWriter = new StreamWriter(Path.Combine(rootDir, "general", fileName));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartDataFileHelper.ChartType.Bubble, 4);
        chartDataFileHelper.WriteBeforeData();

        var firstQuartileDataLines = new List<ChartDataFileHelper.BubbleChartDataLine>();
        var secondQuartileDataLines = new List<ChartDataFileHelper.BubbleChartDataLine>();
        var thirdQuartileDataLines = new List<ChartDataFileHelper.BubbleChartDataLine>();
        var fourthQuartileDataLines = new List<ChartDataFileHelper.BubbleChartDataLine>();

        // Get the values for x-axis
        var xAxisKeys = categoryMetricDataDictionary.Keys.Select(x => x.Key).Distinct().ToList();
        xAxisKeys.Sort();

        foreach (var dataKey in categoryMetricDataDictionary.Keys)
        {
            var xAxisValue = xAxisKeys.IndexOf(dataKey.Key);
            var yAxisValue = dataKey.Value;
            AddDataLine(xAxisValue, yAxisValue, categoryMetricDataDictionary[dataKey],
                [firstQuartileDataLines, secondQuartileDataLines, thirdQuartileDataLines, fourthQuartileDataLines],
                maxValue, 10);
        }

        chartDataFileHelper.WriteData(firstQuartileDataLines, false);
        chartDataFileHelper.WriteData(secondQuartileDataLines, false);
        chartDataFileHelper.WriteData(thirdQuartileDataLines, false);
        chartDataFileHelper.WriteData(fourthQuartileDataLines, true);
        chartDataFileHelper.WriteAfterData("categoryMetric",
            [
                "Premier quartile",
                "Deuxième quartile",
                "Troisième quartile",
                "Quatrième quartile"
            ],
            customScalesOptions: chartDataFileHelper.FormatCategoriesBubbleChartLabelOptions(xAxisLabels.ToList(),
                xAxisTitle: "Catégorie", yAxisTitle: "Métrique (0 = variable)"));
        streamWriter.Close();
    }

    public void FillCategoriesBubbleChartDataDict(Dictionary<KeyValuePair<string, string>, int> dictionary,
        SortedSet<string> xLabels, SortedSet<string> yLabels, Poem poem)
    {
        var subCategories = poem.Categories.SelectMany(x => x.SubCategories).ToList();

        if (subCategories.Count == 1)
        {
            return;
        }

        for (var i = 0; i < subCategories.Count; i++)
        {
            for (var j = subCategories.Count - 1; j > -1; j--)
            {
                if (string.Compare(subCategories[i], subCategories[j]) >= 0) continue;
                var key = new KeyValuePair<string, string>(subCategories[i], subCategories[j]);
                if (dictionary.TryGetValue(key, out _))
                {
                    dictionary[key]++;
                }
                else
                {
                    dictionary.Add(key, 1);
                    xLabels.Add(key.Key);
                    yLabels.Add(key.Value);
                }
            }
        }
    }

    public void FillCategoryMetricBubbleChartDataDict(Dictionary<KeyValuePair<string, int>, int> dictionary,
        SortedSet<string> xLabels, Poem poem)
    {
        var subCategories = poem.Categories.SelectMany(x => x.SubCategories).ToList();
        var metric = poem.HasVariableMetric ? 0 : int.Parse(poem.VerseLength!);

        foreach (var key in subCategories.Select(subCategory => new KeyValuePair<string, int>(subCategory, metric)))
        {
            if (dictionary.TryGetValue(key, out _))
            {
                dictionary[key]++;
            }
            else
            {
                dictionary.Add(key, 1);
                xLabels.Add(key.Key);
            }
        }
    }

    public Dictionary<int, List<decimal>> FillMetricDataDict(out List<string> xLabels)
    {
        var metricRange = Enumerable.Range(1, 14);
        var dataDict = new Dictionary<int, List<decimal>> { };

        xLabels = new List<string>();
        foreach (var metric in metricRange)
        {
            dataDict.Add(metric, new List<decimal>());
        }

        foreach (var season in Data.Seasons.Where(x => x.Poems.Count > 0))
        {
            // Multiplication to get 50
            var multiple = 50m / season.Poems.Count;
            xLabels.Add($"{season.EscapedTitleForChartsWithYears}");

            foreach (var metric in metricRange)
            {
                dataDict[metric]
                    .Add(Decimal.Round(
                        season.Poems.Count(x =>
                            x.HasMetric(metric)) * multiple, 1));
            }
        }

        return dataDict;
    }

    public List<string> GetTopMostMonths(Dictionary<string, int> monthDayDict)
    {
        var monthDict = new Dictionary<string, int>();
        foreach (var monthDay in monthDayDict.Keys)
        {
            var month = monthDay.Substring(0, 2);
            if (monthDict.TryGetValue(month, out _))
            {
                monthDict[month] += monthDayDict[monthDay];
            }
            else
            {
                monthDict.Add(month, monthDayDict[monthDay]);
            }
        }

        return monthDict.OrderByDescending(x => x.Value).Take(4).Select(x => x.Key).Select(GetMonthLabel).ToList();
    }

    public void OutputSeasonsDuration()
    {
        foreach (var season in Data.Seasons.Where(x => x.Poems.Count > 0))
        {
            var dates = season.Poems.Select(x => x.Date).OrderBy(x => x.Date).ToList();
            var duration = dates[dates.Count() - 1] - dates[0];
            decimal nbDays = int.Parse(duration.ToString("%d"));
            var value = nbDays;
            var unit = "days";
            if (value > 30)
            {
                value = value / 30;
                unit = "months";

                if (value > 12)
                {
                    value = value / 12;
                    unit = "years";
                }
            }

            Console.WriteLine($"{season.NumberedName} ({season.Period}): {value} {unit}");
        }
    }

    private void AddDataLine(int x, int y, int value,
        List<ChartDataFileHelper.BubbleChartDataLine>[] quartileBubbleChartDatalines, int maxValue,
        int bubbleMaxRadiusPixels)
    {
        // Bubble radius and color
        decimal bubbleSize = (decimal)bubbleMaxRadiusPixels * value / maxValue;
        var bubbleColor = string.Empty;
        if (bubbleSize < (bubbleMaxRadiusPixels / 4))
        {
            // First quartile
            bubbleSize *= 4;
            bubbleColor = "rgba(121, 248, 248, 1)";
            quartileBubbleChartDatalines[0].Add(new ChartDataFileHelper.BubbleChartDataLine(x, y,
                bubbleSize.ToString(new NumberFormatInfo { NumberDecimalSeparator = "." }), bubbleColor));
        }
        else if (bubbleSize < (bubbleMaxRadiusPixels / 2))
        {
            // Second quartile
            bubbleSize *= 2;
            bubbleColor = "rgba(119, 181, 254, 1)";
            quartileBubbleChartDatalines[1].Add(new ChartDataFileHelper.BubbleChartDataLine(x, y,
                bubbleSize.ToString(new NumberFormatInfo { NumberDecimalSeparator = "." }), bubbleColor));
        }
        else if (bubbleSize < (bubbleMaxRadiusPixels * 3 / 4))
        {
            // Third quartile
            bubbleSize *= 1.5m;
            bubbleColor = "rgba(0, 127, 255, 1)";
            quartileBubbleChartDatalines[2].Add(new ChartDataFileHelper.BubbleChartDataLine(x, y,
                bubbleSize.ToString(new NumberFormatInfo { NumberDecimalSeparator = "." }), bubbleColor));
        }
        else
        {
            // Fourth quartile
            bubbleColor = "rgba(50, 122, 183, 1)";
            quartileBubbleChartDatalines[3].Add(new ChartDataFileHelper.BubbleChartDataLine(x, y,
                bubbleSize.ToString(new NumberFormatInfo { NumberDecimalSeparator = "." }), bubbleColor));
        }
    }

    private string GetRadarChartLabel(string monthDay)
    {
        var day = monthDay.Substring(3);
        var month = monthDay.Substring(0, 2);
        switch (month)
        {
            case "01":
                return day == "01" ? "Janvier" : string.Empty;
            case "02":
                return day == "01" ? "Février" : string.Empty;
            case "03":
                return day == "01" ? "Mars" : day == "20" ? "Printemps" : string.Empty;
            case "04":
                return day == "01" ? "Avril" : string.Empty;
            case "05":
                return day == "01" ? "Mai" : string.Empty;
            case "06":
                return day == "01" ? "Juin" : day == "21" ? "Eté" : string.Empty;
            case "07":
                return day == "01" ? "Juillet" : string.Empty;
            case "08":
                return day == "01" ? "Août" : string.Empty;
            case "09":
                return day == "01" ? "Septembre" : day == "23" ? "Automne" : string.Empty;
            case "10":
                return day == "01" ? "Octobre" : string.Empty;
            case "11":
                return day == "01" ? "Novembre" : string.Empty;
            case "12":
                return day == "01" ? "Décembre" : day == "21" ? "Hiver" : string.Empty;
            default:
                return string.Empty;
        }
    }

    private string GetMonthLabel(string month)
    {
        switch (month)
        {
            case "01":
                return "janvier";
            case "02":
                return "février";
            case "03":
                return "mars";
            case "04":
                return "avril";
            case "05":
                return "mai";
            case "06":
                return "juin";
            case "07":
                return "juillet";
            case "08":
                return "août";
            case "09":
                return "septembre";
            case "10":
                return "octobre";
            case "11":
                return "novembre";
            case "12":
                return "décembre";
            default:
                return "?";
        }
    }

    private string GetRadarEnChartLabel(string monthDay)
    {
        var day = monthDay.Substring(3);
        var month = monthDay.Substring(0, 2);
        switch (month)
        {
            case "01":
                return day == "01" ? "January" : string.Empty;
            case "02":
                return day == "01" ? "February" : string.Empty;
            case "03":
                return day == "01" ? "March" : day == "20" ? "Spring" : string.Empty;
            case "04":
                return day == "01" ? "April" : string.Empty;
            case "05":
                return day == "01" ? "May" : string.Empty;
            case "06":
                return day == "01" ? "June" : day == "21" ? "Summer" : string.Empty;
            case "07":
                return day == "01" ? "July" : string.Empty;
            case "08":
                return day == "01" ? "August" : string.Empty;
            case "09":
                return day == "01" ? "September" : day == "23" ? "Fall" : string.Empty;
            case "10":
                return day == "01" ? "October" : string.Empty;
            case "11":
                return day == "01" ? "November" : string.Empty;
            case "12":
                return day == "01" ? "December" : day == "21" ? "Winter" : string.Empty;
            default:
                return string.Empty;
        }
    }
}