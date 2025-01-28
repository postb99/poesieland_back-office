﻿using System.Globalization;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Extensions.Configuration;
using Toolbox.Domain;
using Toolbox.Settings;

namespace Toolbox;

public class Engine
{
    const decimal bubbleMaxRadiusPixels = 30;

    private IConfiguration _configuration;
    public Root Data { get; private set; }
    public Root DataEn { get; private set; }
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

        xmlDocPath = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.XML_STORAGE_FILE_EN]);
        using var streamReaderEn = new StreamReader(xmlDocPath,
            Encoding.GetEncoding(_configuration[Constants.XML_STORAGE_FILE_ENCODING]));

        DataEn = XmlSerializer.Deserialize(streamReaderEn) as Root;
    }

    public void Save()
    {
        var xmlDocPath = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.XML_STORAGE_FILE]);
        using var streamWriter = new StreamWriter(xmlDocPath);
        XmlSerializer.Serialize(streamWriter, Data);
        streamWriter.Close();

        xmlDocPath = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.XML_STORAGE_FILE_EN]);
        using var streamWriterEn = new StreamWriter(xmlDocPath);
        XmlSerializer.Serialize(streamWriterEn, DataEn);
        streamWriterEn.Close();
    }

    public void GenerateSeasonIndexFile(int seasonId)
    {
        var season = Data.Seasons.First(x => x.Id == seasonId);
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.CONTENT_ROOT_DIR]);
        var contentDir = Path.Combine(rootDir, season.ContentDirectoryName);
        var indexFile = Path.Combine(contentDir, "_index.md");
        Directory.CreateDirectory(contentDir);
        File.WriteAllText(indexFile, season.IndexFileContent());

        rootDir = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.CHART_DATA_FILES_ROOT_DIR]);
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
        var season = Data.Seasons.First(x => x.Id == poem.SeasonId);
        var poemIndex = season.Poems.IndexOf(poem);
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.CONTENT_ROOT_DIR]);
        var contentDir = Path.Combine(rootDir, season.ContentDirectoryName);
        var indexFile = Path.Combine(contentDir, poem.ContentFileName);
        File.WriteAllText(indexFile, poem.FileContent(poemIndex));
    }

    public void GenerateSeasonAllPoemFiles(int seasonId)
    {
        // if (seasonId == -1)
        // {
        //     var poems = Data.Seasons.SelectMany(x => x.Poems).Where(x => x.Categories.SelectMany(x => x.SubCategories).Any(x => x == "Faune")).ToList();
        //     poems.ForEach(GeneratePoemFile);
        //     return;
        // }

        var season = Data.Seasons.First(x => x.Id == seasonId);
        season.Poems.ForEach(GeneratePoemFile);
    }

    public Poem? ImportPoem(string poemId)
    {
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.CONTENT_ROOT_DIR]);
        var seasonId = poemId.Substring(poemId.LastIndexOf('_') + 1);
        var seasonDirName = Directory.EnumerateDirectories(rootDir)
            .FirstOrDefault(x => Path.GetFileName(x).StartsWith($"{seasonId}_"));
        if (seasonDirName == null)
        {
            throw new Exception("Create season directory before importing poem");
        }

        var poemFileName = $"{poemId.Substring(0, poemId.LastIndexOf('_'))}.md";
        var poemContentPath = Path.Combine(rootDir, seasonDirName, poemFileName);
        if (!File.Exists(poemContentPath))
        {
            return null;
        }

        var (poem, position) =
            (_poemContentImporter ??= new PoemContentImporter()).Import(poemContentPath, _configuration);
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

        Save();
        return poem;
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
                    (_poemContentImporter ??= new PoemContentImporter()).Import(poemContentPath, _configuration);

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
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.CONTENT_ROOT_DIR]);
        var seasonMaxId = Data.Seasons.Count;
        var poemContentImporter = new PoemContentImporter();
        for (var i = 17; i < seasonMaxId + 1; i++)
        {
            var season = Data.Seasons.First(x => x.Id == i);
            var contentDir = Path.Combine(rootDir, season.ContentDirectoryName);
            var poemContentPaths = Directory.EnumerateFiles(contentDir).Where(x => !x.EndsWith("_index.md"));
            foreach (var poemContentPath in poemContentPaths)
            {
                var (tags, year, poemId, variableVerseLength) =
                    poemContentImporter.GetTagsYearVariableVerseLength(poemContentPath, _configuration);
                if (poemContentImporter.HasYamlMetadata)
                {
                    if (!tags.Contains(year.ToString()))
                    {
                        yield return poemId;
                    }

                    if (variableVerseLength && !tags.Contains("versVariable"))
                    {
                        yield return poemId;
                    }
                }
            }
        }
    }

    public void CheckPoemsWithoutVerseLength()
    {
        var poems = Data.Seasons.SelectMany(x => x.Poems);
        var poemsWithVerseLength = poems.Count(x => x.VerseLength != null && x.VerseLength != "0");
        if (poemsWithVerseLength == poems.Count())
            return;

        var incorrectPoem = poems.FirstOrDefault(x => x.VerseLength == null || x.VerseLength == "0");
        if (incorrectPoem != null)
            throw new Exception(
                $"[ERROR] First poem with verse length unspecified or equal to '0': {incorrectPoem.Id}");
    }

    public void CheckPoemsWithVariableVerseLength()
    {
        var poems = Data.Seasons.SelectMany(x => x.Poems.Where(x => x.HasVariableVerseLength));

        var incorrectPoem = poems.FirstOrDefault(x => !x.Info.StartsWith("Vers variable : "));
        if (incorrectPoem != null)
            throw new Exception(
                $"[ERROR] First poem with variable verse length unspecified in Info: {incorrectPoem.Id}");
    }

    public void GeneratePoemsLengthBarChartDataFile(int? seasonId)
    {
        var barChartFileName = "poems-length-bar.js";
        var barChartId = seasonId != null ? $"season{seasonId}PoemLengthBar" : "poemLengthBar";

        var poems = seasonId != null
            ? Data.Seasons.First(x => x.Id == seasonId).Poems
            : Data.Seasons.SelectMany(x => x.Poems);

        var nbVersesData = new Dictionary<int, int>();
        var quatrainsData = new Dictionary<int, int>();
        var nbSonnets = 0;
        var nbNotQuatrainImpossible = 0;
        var nbNotQuatrainVoluntarily = 0;
        foreach (var poem in poems)
        {
            var nbVerses = poem.VersesCount;
            var hasQuatrains = poem.HasQuatrains;
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
                if (quatrainsData.TryGetValue(nbVerses, out var _))
                {
                    quatrainsData[nbVerses]++;
                }
                else
                {
                    quatrainsData[nbVerses] = 1;
                }
            }
            else
            {
                if (nbVerses % 4 != 0)
                {
                    nbNotQuatrainImpossible++;
                }
                else
                {
                    nbNotQuatrainVoluntarily++;
                }
            }

            if (poem.IsSonnet)
            {
                nbSonnets++;
            }
        }

        var nbVersesRange = nbVersesData.Keys.Order().ToList();


        // Bar chart
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CHART_DATA_FILES_ROOT_DIR]);
        var subDir = seasonId != null ? $"season-{seasonId}" : "general";
        var subDirPath = Path.Combine(rootDir, subDir);
        Directory.CreateDirectory(subDirPath);
        using var streamWriter = new StreamWriter(Path.Combine(subDirPath, barChartFileName));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartDataFileHelper.ChartType.Bar, 2);
        chartDataFileHelper.WriteBeforeData();

        var nbVersesChartData = new List<ChartDataFileHelper.DataLine>();
        var isSonnetChartData = new List<ChartDataFileHelper.DataLine>();

        var baseColor = "rgba(72, 149, 239, {0})";
        var baseAlpha = 0.4;

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

        chartDataFileHelper.WriteAfterData(barChartId, chartTitles,
            barChartOptions: seasonId == null
                ? "{ scales: { y: { max: " + ChartDataFileHelper.NBVERSES_MAX_Y + " } } }"
                : "{ scales: { y: { ticks: { stepSize: 1 } } } }");
        streamWriter.Close();
    }

    public void GenerateSeasonCategoriesPieChartDataFile(int? seasonId)
    {
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CHART_DATA_FILES_ROOT_DIR]);
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
                (subcategory, byStorageSubcategoryCount[subcategory],
                    storageSettings.Categories.SelectMany(x => x.Subcategories)
                        .First(x => x.Name == subcategory).Color
                ));
        }

        chartDataFileHelper.WriteData(pieChartData);

        chartDataFileHelper.WriteAfterData(seasonId.HasValue ? $"season{seasonId}Pie" : "categoriesPie",
        [
            seasonId.HasValue ? $"{season.EscapedLongTitle} - {season.Period.Replace("'", "\\'")}" : string.Empty
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
            var day = $"{poemStringDate.Substring(3, 2)}-{poemStringDate.Substring(0, 2)}";
            dataDict[day]++;
        }

        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CHART_DATA_FILES_ROOT_DIR]);
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

        chartDataFileHelper.WriteAfterData(chartId, ["Poèmes selon le jour de l\\\'année"], borderColor,
            backgroundColor);
        streamWriter.Close();

        if (storageSubCategory != null || storageCategory != null) return;

        // Days without poems listing

        var filePath = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.CONTENT_ROOT_DIR],
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
            _configuration[Constants.CHART_DATA_FILES_ROOT_DIR]);
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
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.CONTENT_ROOT_DIR]);
        var seasonDirName = Directory.EnumerateDirectories(rootDir)
            .FirstOrDefault(x => Path.GetFileName(x).StartsWith($"{seasonId}_"));
        var poemFiles = Directory.EnumerateFiles(seasonDirName).Where(x => !x.EndsWith("index.md"));

        foreach (var poemFile in poemFiles)
        {
            var (poem, position) =
                (_poemContentImporter ??= new PoemContentImporter()).Import(poemFile, _configuration);
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

    public void GeneratePoemVersesLengthBarChartDataFile(int? seasonId)
    {
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CHART_DATA_FILES_ROOT_DIR]);
        var fileName = "poems-verse-length-bar.js";
        var subDir = seasonId != null ? $"season-{seasonId}" : "general";
        var chartId = seasonId != null ? $"season{seasonId}VerseLengthBar" : "poemVerseLengthBar";
        using var streamWriter = new StreamWriter(Path.Combine(rootDir, subDir, fileName));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartDataFileHelper.ChartType.Bar, 1);
        chartDataFileHelper.WriteBeforeData();
        var regularVerseLengthData = new Dictionary<int, int>();
        var variableVerseLengthData = new Dictionary<string, int>();
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
            else if (poem.HasVariableVerseLength)
            {
                if (variableVerseLengthData.TryGetValue(poem.DetailedVerseLength, out var _))
                {
                    variableVerseLengthData[poem.DetailedVerseLength]++;
                }
                else
                {
                    variableVerseLengthData[poem.DetailedVerseLength] = 1;
                }
            }
            else
            {
                var verseLength = int.Parse(poem.VerseLength);
                if (regularVerseLengthData.TryGetValue(verseLength, out var _))
                {
                    regularVerseLengthData[verseLength]++;
                }
                else
                {
                    regularVerseLengthData[verseLength] = 1;
                }
            }
        }

        var regularVerseLengthRange = regularVerseLengthData.Keys.Order().ToList();
        var variableVerseLengthRange = variableVerseLengthData.Keys.Order().ToList();

        var regularVerseLengthChartData = new List<ChartDataFileHelper.DataLine>();
        var variableVerseLengthChartData = new List<ChartDataFileHelper.ColoredDataLine>();

        foreach (var verseLength in regularVerseLengthRange)
        {
            regularVerseLengthChartData.Add(new ChartDataFileHelper.DataLine(
                verseLength.ToString(), regularVerseLengthData[verseLength]));
        }

        foreach (var verseLength in variableVerseLengthRange)
        {
            variableVerseLengthChartData.Add(new ChartDataFileHelper.ColoredDataLine
                (verseLength, variableVerseLengthData[verseLength], "rgba(72, 149, 239, 1)"));
        }

        var undefinedVerseLengthChartData = new ChartDataFileHelper.ColoredDataLine
        ("Pas de données pour l\\'instant", nbUndefinedVerseLength, "rgb(211, 211, 211)"
        );

        var dataLines = new List<ChartDataFileHelper.DataLine>();
        dataLines.AddRange(regularVerseLengthChartData);
        dataLines.AddRange(variableVerseLengthChartData);
        if (nbUndefinedVerseLength > 0)
            dataLines.Add(undefinedVerseLengthChartData);

        chartDataFileHelper.WriteData(dataLines, true);

        chartDataFileHelper.WriteAfterData(chartId, ["Poèmes"],
            barChartOptions: seasonId == null
                ? "{ scales: { y: { max: " + ChartDataFileHelper.VERSE_LENGTH_MAX_Y + " } } }"
                : "{ scales: { y: { ticks: { stepSize: 1 } } } }");
        streamWriter.Close();

        // Second chart for variable length, when no season ID is given
        if (seasonId == null)
        {
            fileName = "variable-verse-length-bar.js";
            chartId = "variableVerseLengthBar";
            using var streamWriter2 = new StreamWriter(Path.Combine(rootDir, "general", fileName));
            var chartDataFileHelper2 = new ChartDataFileHelper(streamWriter2, ChartDataFileHelper.ChartType.Bar, 1);
            chartDataFileHelper2.WriteBeforeData();

            dataLines = [];
            dataLines.AddRange(variableVerseLengthChartData);

            chartDataFileHelper2.WriteData(dataLines, true);

            chartDataFileHelper2.WriteAfterData(chartId, ["Poèmes"],
                barChartOptions: "{ scales: { y: { ticks: { stepSize: 1 } } } }");
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
            if (dataDict.ContainsKey(fullDate))
            {
                dataDict[fullDate]++;
            }
            else
            {
                dataDict.Add(fullDate, 1);
            }
        }

        var intensityDict = new Dictionary<int, int>();

        foreach (var data in dataDict)
        {
            var value = data.Value;
            if (intensityDict.ContainsKey(value))
            {
                intensityDict[value]++;
            }
            else
            {
                intensityDict.Add(value, 1);
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
            _configuration[Constants.CHART_DATA_FILES_ROOT_DIR]);
        using var streamWriter = new StreamWriter(Path.Combine(rootDir, "general", fileName));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartDataFileHelper.ChartType.Pie);
        chartDataFileHelper.WriteBeforeData();
        chartDataFileHelper.WriteData(dataLines, true);
        chartDataFileHelper.WriteAfterData("poemIntensityPie", ["Les jours de création sont-ils intenses ?"]);
        streamWriter.Close();
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
            if (dataDict.ContainsKey((int)dayOfWeek))
            {
                dataDict[(int)dayOfWeek]++;
            }
            else
            {
                dataDict.Add((int)dayOfWeek, 1);
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
            _configuration[Constants.CHART_DATA_FILES_ROOT_DIR]);
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
            if (dataDict.ContainsKey((int)dayOfWeek))
            {
                dataDict[(int)dayOfWeek]++;
            }
            else
            {
                dataDict.Add((int)dayOfWeek, 1);
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
        bool forAcrostiche = false, bool forSonnet = false, bool forPantoun = false, bool forVariableVerse = false)
    {
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CHART_DATA_FILES_ROOT_DIR]);
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
        else if (forVariableVerse)
        {
            fileName = $"poems-versVariable-bar.js";
            chartId = $"poems-versVariableBar";
        }

        var backgroundColor = borderColor.Replace("1)", "0.5)");
        if (forVariableVerse)
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
            else if (forVariableVerse)
            {
                poemCount = season.Poems.Count(x => x.HasVariableVerseLength);
            }

            dataLines.Add(new ChartDataFileHelper.ColoredDataLine($"{season.EscapedLongTitle} ({season.Years})",
                poemCount,
                backgroundColor));
        }

        chartDataFileHelper.WriteData(dataLines, true);

        chartDataFileHelper.WriteAfterData(chartId, ["Poèmes au fil des saisons"],
            barChartOptions: "{ scales: { y: { ticks: { stepSize: 1 } } } }");
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

            if (intervalLengthDict.ContainsKey(dayDiff))
            {
                intervalLengthDict[dayDiff]++;
            }
            else
            {
                intervalLengthDict.Add(dayDiff, 1);
            }

            if (!intervalDict.ContainsKey(current))
                intervalDict.Add(current, dayDiff);

            if (!seriesDict.ContainsKey(previous))
                seriesDict.Add(previous, 1);

            if (dayDiff == 1)
            {
                var duration = seriesDict[previous];
                seriesDict.Remove(previous);
                seriesDict[current] = ++duration;
            }
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
            _configuration[Constants.CHART_DATA_FILES_ROOT_DIR]);
        using var streamWriter = new StreamWriter(Path.Combine(rootDir, subDir, fileName));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartDataFileHelper.ChartType.Bar);
        chartDataFileHelper.WriteBeforeData();
        chartDataFileHelper.WriteData(dataLines, true);
        chartDataFileHelper.WriteAfterData(seasonId == null ? "poemIntervalBar" : $"season{seasonId}PoemIntervalBar",
            ["Fréquence"],
            barChartOptions: seasonId == null ? "{}" : "{ scales: { y: { ticks: { stepSize: 1 } } } }");
        streamWriter.Close();

        if (seasonId.HasValue) return;

        // Longest intervals content file

        var longestIntervalKeys = orderedIntervalKeys.OrderDescending().ToList();
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.CONTENT_ROOT_DIR],
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
            if (seriesLengthDict.ContainsKey(seriesLength))
            {
                seriesLengthDict[seriesLength]++;
            }
            else
            {
                seriesLengthDict[seriesLength] = 1;
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
            barChartOptions: seasonId == null ? "{}" : "{ scales: { y: { ticks: { stepSize: 1 } } } }");
        streamWriter2.Close();

        // longest series content file

        var longestSeriesKeys = sortedKeys.OrderDescending().Where(x => x > 6);
        filePath = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.CONTENT_ROOT_DIR],
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
            _configuration[Constants.CONTENT_ROOT_DIR], "../../common", "poem_count.md");
        File.WriteAllText(poemCountFilePath, poemCount.ToString());

        // And for variable verse
        var variableVersePoemCount = Data.Seasons.SelectMany(x => x.Poems.Where(x => x.HasVariableVerseLength)).Count();
        var variableVersePoemCountFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CONTENT_ROOT_DIR], "../../common", "variableVerse_poem_count.md");
        File.WriteAllText(variableVersePoemCountFilePath, variableVersePoemCount.ToString());
    }

    public void GeneratePoemEnCountFile()
    {
        var poemCount = DataEn.Seasons.Select(x => x.Poems.Count).Sum();
        var poemCountFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CONTENT_ROOT_DIR], "../../common", "poem_count_en.md");
        File.WriteAllText(poemCountFilePath, poemCount.ToString());
    }

    public void GeneratePoemLengthByVerseLengthBubbleChartDataFile()
    {
        var poems = Data.Seasons.SelectMany(x => x.Poems);
        var poemLengthByVerseLength = new Dictionary<KeyValuePair<int, int>, int>();
        var variableVerseLength = new Dictionary<int, int>();
        foreach (var poem in poems)
        {
            var poemLength = poem.VersesCount;
            if (poem.HasVariableVerseLength)
            {
                if (variableVerseLength.ContainsKey(poemLength))
                {
                    variableVerseLength[poemLength]++;
                }
                else
                {
                    variableVerseLength[poemLength] = 1;
                }

                continue;
            }

            var key = new KeyValuePair<int, int>(int.Parse(poem.VerseLength), poemLength);
            if (poemLengthByVerseLength.ContainsKey(key))
            {
                poemLengthByVerseLength[key]++;
            }
            else
            {
                poemLengthByVerseLength[key] = 1;
            }
        }

        // Find max value
        var maxValue = poemLengthByVerseLength.Values.Max();

        var fileName = "poem-length-by-verse-length.js";
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CHART_DATA_FILES_ROOT_DIR]);
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
                maxValue);
        }

        foreach (var dataKey in variableVerseLength.Keys)
        {
            AddDataLine(0, dataKey, variableVerseLength[dataKey],
                [firstQuartileDataLines, secondQuartileDataLines, thirdQuartileDataLines, fourthQuartileDataLines],
                maxValue);
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
        ], chartXAxisTitle: "Longueur du vers (0 = variable)", chartYAxisTitle: "Nombre de vers", yAxisStep: 2);
        streamWriter.Close();
    }

    public void GenerateOverSeasonsVerseLengthLineChartDataFile()
    {
        var verseLengthRange = Enumerable.Range(2, 13);
        var dataDict = new Dictionary<int, List<int>> { { 0, [] } }; // For variable verse length

        var xLabels = new List<string>();
        foreach (var verseLength in verseLengthRange)
        {
            dataDict.Add(verseLength, new List<int>());
        }

        foreach (var season in Data.Seasons.Where(x => x.Poems.Count > 0))
        {
            // Multiplicator to get 100%
            var multiple = 100 / season.Poems.Count;
            xLabels.Add($"{season.EscapedLongTitle} ({season.Years})");
            dataDict[0].Add(season.Poems.Count(x => x.HasVariableVerseLength) * multiple);

            foreach (var verseLength in verseLengthRange)
            {
                dataDict[verseLength].Add(season.Poems.Count(x => x.VerseLength == verseLength.ToString()) * multiple);
            }
        }

        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CHART_DATA_FILES_ROOT_DIR]);

        var fileName = "poems-verseLength-line.js";

        using var streamWriter = new StreamWriter(Path.Combine(rootDir, "general", fileName));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartDataFileHelper.ChartType.Line, 13);
        chartDataFileHelper.WriteBeforeData();

        var variableVerseDataLines = new ChartDataFileHelper.LineChartDataLine("Vers variable", dataDict[0], "rgb(247, 249, 249)");
        var twoFeetDataLines = new ChartDataFileHelper.LineChartDataLine("2 pieds", dataDict[2], "rgb(230, 176, 170)");
        var threeFeetDataLines = new ChartDataFileHelper.LineChartDataLine("3 pieds", dataDict[3], "rgb(245, 183, 177)");
        var fourFeetDataLines = new ChartDataFileHelper.LineChartDataLine("4 pieds", dataDict[4], "rgb(215, 189, 226)");
        var fiveFeetDataLines = new ChartDataFileHelper.LineChartDataLine("5 pieds", dataDict[5], "rgb(169, 204, 227)");
        var sixFeetDataLines = new ChartDataFileHelper.LineChartDataLine("6 pieds", dataDict[6], "rgb(174, 214, 241)");
        var sevenFeetDataLines = new ChartDataFileHelper.LineChartDataLine("7 pieds", dataDict[7], "rgb(163, 228, 215)");
        var eightFeetDataLines = new ChartDataFileHelper.LineChartDataLine("8 pieds", dataDict[8], "rgb(162, 217, 206)");
        var nineFeetDataLines = new ChartDataFileHelper.LineChartDataLine("9 pieds", dataDict[9], "rgb(171, 235, 198)");
        var tenFeetDataLines = new ChartDataFileHelper.LineChartDataLine("10 pieds", dataDict[10], "rgb(249, 231, 159)");
        var elevenFeetDataLines = new ChartDataFileHelper.LineChartDataLine("11 pieds", dataDict[11], "rgb(250, 215, 160)");
        var twelveFeetDataLines = new ChartDataFileHelper.LineChartDataLine("12 pieds", dataDict[12], "rgb(237, 187, 153)");
        //var thirteenFeetDataLines = new ChartDataFileHelper.LineChartDataLine("13 pieds", dataDict[13], "rgb(229, 231, 233)");
        var fourteenFeetDataLines = new ChartDataFileHelper.LineChartDataLine("14 pieds", dataDict[14], "rgb(204, 209, 209)");
        
        chartDataFileHelper.WriteData(variableVerseDataLines);
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
        //chartDataFileHelper.WriteData(thirteenFeetDataLines);
        chartDataFileHelper.WriteData(fourteenFeetDataLines);
        
        chartDataFileHelper.WriteAfterData("poemsVerseLengthLine",
        [
            "Vers variable",
            "2 pieds",
            "3 pieds",
            "4 pieds",
            "5 pieds",
            "6 pieds",
            "7 pieds",
            "8 pieds",
            "9 pieds",
            "10 pieds",
            "11 pieds",
            "12 pieds",
            //"13 pieds",
            "14 pieds"
        ], chartYAxisTitle: "Longueur du vers (0 = variable)", chartXAxisTitle: "Au fil des Saisons", xLabels: xLabels.ToArray(), stack: "stack0");
        streamWriter.Close();
    }

    public void OutputSeasonsDuration()
    {
        foreach (var season in Data.Seasons)
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
        List<ChartDataFileHelper.BubbleChartDataLine>[] quartileBubbleChartDatalines, int maxValue)
    {
        // Bubble radius and color
        var bubbleSize = bubbleMaxRadiusPixels * value / maxValue;
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