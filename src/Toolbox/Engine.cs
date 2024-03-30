using System.Globalization;
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
        using var streamReader = new StreamReader(xmlDocPath, Encoding.GetEncoding(_configuration[Constants.XML_STORAGE_FILE_ENCODING]));

        Data = XmlSerializer.Deserialize(streamReader) as Root;
        
        xmlDocPath = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.XML_STORAGE_FILE_EN]);
        using var streamReaderEn = new StreamReader(xmlDocPath, Encoding.GetEncoding(_configuration[Constants.XML_STORAGE_FILE_ENCODING]));

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

        // once
        //GeneratePoemsLengthBarChartDataFile(seasonId);
        //GeneratePoemIntervalBarChartDataFile(seasonId);
    }

    public void GenerateAllSeasonsIndexFile()
    {
        for (var i = 1; i < Data.Seasons.Count + 1; i++)
        {
            GenerateSeasonIndexFile(i);
            // useful once
            //GeneratePoemVersesLengthBarChartDataFile(i);
            //GeneratePoemsLengthBarChartDataFile(i);
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
            return null;
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
    
    public void ImportPoemsEn(string year)
    {
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(), _configuration[Constants.CONTENT_ROOT_DIR_EN]);
        var yearDirName = Directory.EnumerateDirectories(rootDir).FirstOrDefault(x => Path.GetFileName(x) == year);
        var poemFilePaths = Directory.EnumerateFiles(yearDirName).Where(x => !x.EndsWith("_index.md"));
        var poemsByPosition = new Dictionary<int, Poem>(50);
        foreach (var poemContentPath in poemFilePaths)
        {
            var (poem, position) =
                (_poemContentImporter ??= new PoemContentImporter()).Import(poemContentPath, _configuration);

            poemsByPosition.Add(position, poem);
        }

        var seasonId = poemsByPosition.First().Value.SeasonId;
        var targetSeason = DataEn.Seasons.FirstOrDefault(x => x.Id == seasonId);

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
        for (var i = 17; i < seasonMaxId + 1; i++)
        {
            var season = Data.Seasons.First(x => x.Id == i);
            var contentDir = Path.Combine(rootDir, season.ContentDirectoryName);
            var poemContentPaths = Directory.EnumerateFiles(contentDir).Where(x => !x.EndsWith("_index.md"));
            foreach (var poemContentPath in poemContentPaths)
            {
                var (tags, year, poemId) = poemContentImporter.GetTagsAndYear(poemContentPath, _configuration);
                if (poemContentImporter.HasYamlMetadata && !tags.Contains(year.ToString()))
                {
                    yield return poemId;
                }
            }
        }
    }

    public void CheckPoemsWithoutVerseLength()
    {
        var poems = Data.Seasons.SelectMany(x => x.Poems);
        var poemsWithVerseLength = poems.Count(x => x.VerseLength != null && x.VerseLength != "0");
        int percentage = poemsWithVerseLength * 100 / poems.Count();
        Console.WriteLine("{0}/{1} poems ({2} %) with verse length specified",
            poemsWithVerseLength, poems.Count(), percentage);
        Console.WriteLine("[INFO] First poem without verse length specified: {0}",
            poems.FirstOrDefault(x => x.VerseLength == null)?.Id);
        Console.WriteLine("[ERROR] First poem with verse length equal to '0': {0}",
            poems.FirstOrDefault(x => x.VerseLength == "0")?.Id);

        var seasonWithoutAllVerseLength =
            Data.Seasons.Where(x => x.Poems.Any(x => x.VerseLength == null || x.VerseLength == "0")).ToList();
        Console.WriteLine("[INFO] IDs of seasons without all verse length specified: {0}",
            string.Join(',', seasonWithoutAllVerseLength.Select(x => x.Id)));
    }

    public void GeneratePoemsLengthBarChartDataFile(int? seasonId)
    {
        var barChartFileName = seasonId != null
            ? $"season-{seasonId}-poems-length-bar.js"
            : "poems-length-bar.js";
        var barChartId = seasonId != null ? $"season{seasonId}PoemLengthBar" : "poemLengthBar";
        var pieChartFileName = barChartFileName.Replace("bar", "pie");
        var pieChartId = barChartId.Replace("Bar", "Pie");

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

            if (isSonnet)
            {
                nbSonnets++;
            }
        }

        var nbVersesRange = nbVersesData.Keys.Order().ToList();

        // Bar chart
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CHART_DATA_FILES_ROOT_DIR]);
        using var streamWriter = new StreamWriter(Path.Combine(rootDir, barChartFileName));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartDataFileHelper.ChartType.Bar, 2);
        chartDataFileHelper.WriteBeforeData();

        var nbVersesChartData = new List<ChartDataFileHelper.DataLine>();
        var isSonnetChartData = new List<ChartDataFileHelper.DataLine>();

        // Pie chart
        using var streamWriter2 = new StreamWriter(Path.Combine(rootDir, pieChartFileName));
        var chartDataFileHelper2 = new ChartDataFileHelper(streamWriter2, ChartDataFileHelper.ChartType.Pie);
        chartDataFileHelper2.WriteBeforeData();

        var baseColor = "rgba(72, 149, 239, {0})";
        var baseAlpha = 0.4;
        var pieChartDataLines = new List<ChartDataFileHelper.DataLine>();

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
            chartTitles = new[] { "Poèmes", "Sonnets" };
        }
        else
        {
            chartDataFileHelper.WriteData(nbVersesChartData, true);
            chartTitles = new[] { "Poèmes" };
        }

        chartDataFileHelper.WriteAfterData(barChartId, chartTitles,
            barChartOptions: seasonId == null
                ? "{ scales: { y: { max: " + ChartDataFileHelper.NBVERSES_MAX_Y + " } } }"
                : "{ scales: { y: { ticks: { stepSize: 1 } } } }");
        streamWriter.Close();

        foreach (var key in nbVersesRange)
        {
            if (!quatrainsData.ContainsKey(key)) continue;
            pieChartDataLines.Add(new ChartDataFileHelper.ColoredDataLine(
                $"{key / 4} {(key == 4 ? "quatrain" : "quatrains")}",
                quatrainsData[key],
                string.Format(baseColor,
                    (baseAlpha + 0.1 * (key / 4 - 1)).ToString(new NumberFormatInfo
                        { NumberDecimalSeparator = ".", NumberDecimalDigits = 1 }))));
        }

        if (nbNotQuatrainImpossible > 0)
            pieChartDataLines.Add(new ChartDataFileHelper.ColoredDataLine("Nombre de vers non multiple de quatre",
                nbNotQuatrainImpossible, "rgba(67, 97, 238, 0.9)"));

        if (nbNotQuatrainVoluntarily > 0)
        {
            var title = poems.Any(x => x.Acrostiche != null)
                ? "Rimes suivies ou acrostiche découpé différemment"
                : "Rimes suivies";
            pieChartDataLines.Add(new ChartDataFileHelper.ColoredDataLine(
                title, nbNotQuatrainVoluntarily,
                "rgba(67, 97, 238, 0.7)"));
        }

        chartDataFileHelper2.WriteData(pieChartDataLines, true);
        chartDataFileHelper2.WriteAfterData(pieChartId, new[] { "En quatrains ?" });
        streamWriter2.Close();
    }

    public void GenerateSeasonCategoriesPieChartDataFile(int seasonId)
    {
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CHART_DATA_FILES_ROOT_DIR]);
        var storageSettings = _configuration.GetSection(Constants.STORAGE_SETTINGS).Get<StorageSettings>();
        using var streamWriter = new StreamWriter(Path.Combine(rootDir, $"season-{seasonId}-pie.js"));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartDataFileHelper.ChartType.Pie);
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
                (subcategory, byStorageSubcategoryCount[subcategory],
                    storageSettings.Categories.SelectMany(x => x.Subcategories)
                        .First(x => x.Name == subcategory).Color
                ));
        }

        chartDataFileHelper.WriteData(pieChartData);

        var seasonSummaryLastDot = season.Summary.LastIndexOf('.');
        chartDataFileHelper.WriteAfterData($"season{seasonId}Pie",
            new[]
            {
                $"{season.LongTitle} - {season.Summary.Substring(seasonSummaryLastDot == -1 ? 0 : seasonSummaryLastDot + 2).Replace("'", "\\'")}"
            });
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
            fileName = $"poems-day-{storageSubCategory.UnaccentedCleaned()}-radar.js";
            chartId = $"poemDay-{storageSubCategory.UnaccentedCleaned()}Radar";
            borderColor = _configuration.GetSection(Constants.STORAGE_SETTINGS).Get<StorageSettings>().Categories
                .SelectMany(x => x.Subcategories).FirstOrDefault(x => x.Name == storageSubCategory).Color;

            switch (borderColor)
            {
                // Use some not too light colors
                case "rgba(255, 229, 236, 1)":
                    borderColor = "rgba(255, 194, 209, 1)";
                    break;
                case "rgba(247, 235, 253, 1)":
                    borderColor = "rgba(234, 191, 250, 1)";
                    break;
            }
        }
        else if (storageCategory != null)
        {
            fileName = $"poems-day-{storageCategory.UnaccentedCleaned()}-radar.js";
            chartId = $"poemDay-{storageCategory.UnaccentedCleaned()}Radar";
            borderColor = _configuration.GetSection(Constants.STORAGE_SETTINGS).Get<StorageSettings>().Categories
                .FirstOrDefault(x => x.Name == storageCategory).Color;
        }
        else
        {
            fileName = "poems-day-radar.js";
            chartId = "poemDayRadar";
        }

        using var streamWriter = new StreamWriter(Path.Combine(rootDir, fileName));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartDataFileHelper.ChartType.Radar);
        chartDataFileHelper.WriteBeforeData();

        var dataLines = new List<ChartDataFileHelper.DataLine>();

        var dayWithPoems = 0;
        var dayWithoutPoems = 0;

        foreach (var monthDay in dataDict.Keys)
        {
            var value = dataDict[monthDay];
            dataLines.Add(new ChartDataFileHelper.DataLine(GetRadarChartLabel(monthDay), value
            ));
            if (value == 0)
            {
                dayWithoutPoems++;
            }
            else
            {
                dayWithPoems++;
            }
        }

        var specialValue = dataDict["02-29"];
        if (specialValue == 0)
        {
            dayWithoutPoems--;
        }
        else
        {
            dayWithPoems--;
        }

        chartDataFileHelper.WriteData(dataLines, true);

        var backgroundColor = borderColor?.Replace("1)", "0.5)");

        chartDataFileHelper.WriteAfterData(chartId, new[] { "Poèmes selon le jour de l\\\'année" }, borderColor,
            backgroundColor);
        streamWriter.Close();

        // Second chart
        if (storageSubCategory != null || storageCategory != null) return;

        fileName = "poem-day-pie.js";
        var streamWriter2 = new StreamWriter(Path.Combine(rootDir, fileName));
        chartDataFileHelper = new ChartDataFileHelper(streamWriter2, ChartDataFileHelper.ChartType.Pie);
        chartDataFileHelper.WriteBeforeData();
        dataLines = new List<ChartDataFileHelper.DataLine>();
        dataLines.Add(new ChartDataFileHelper.ColoredDataLine("Jours sans écrire", dayWithoutPoems,
            "rgba(72, 149, 239, 1)"));
        dataLines.Add(
            new ChartDataFileHelper.ColoredDataLine("Jours de création", dayWithPoems, "rgba(76, 201, 240, 1)"));
        chartDataFileHelper.WriteData(dataLines, true);
        chartDataFileHelper.WriteAfterData("poemDayPie", new[] { "Avec ou sans création ?" });
        streamWriter2.Close();
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

        using var streamWriter = new StreamWriter(Path.Combine(rootDir, fileName));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartDataFileHelper.ChartType.Radar);
        chartDataFileHelper.WriteBeforeData();

        var dataLines = new List<ChartDataFileHelper.DataLine>();

        foreach (var monthDay in dataDict.Keys)
        {
            var value = dataDict[monthDay];
            dataLines.Add(new ChartDataFileHelper.DataLine(GetRadarChartLabel(monthDay), value));
        }

        chartDataFileHelper.WriteData(dataLines, true);

        chartDataFileHelper.WriteAfterData(chartId, new[] { "Poèmes selon le jour de l\\\'année" }, string.Empty,
            string.Empty);
        streamWriter.Close();
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
        var fileName = seasonId != null
            ? $"season-{seasonId}-verse-length-bar.js"
            : "poems-verse-length-bar.js";
        var chartId = seasonId != null ? $"season{seasonId}VerseLengthBar" : "poemVerseLengthBar";
        using var streamWriter = new StreamWriter(Path.Combine(rootDir, fileName));
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
            else if (poem.VerseLength.Contains(',') || poem.VerseLength.Contains(' '))
            {
                if (variableVerseLengthData.TryGetValue(poem.VerseLength, out var _))
                {
                    variableVerseLengthData[poem.VerseLength]++;
                }
                else
                {
                    variableVerseLengthData[poem.VerseLength] = 1;
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

        chartDataFileHelper.WriteAfterData(chartId, new[] { "Poèmes" },
            barChartOptions: seasonId == null
                ? "{ scales: { y: { max: " + ChartDataFileHelper.VERSE_LENGTH_MAX_Y + " } } }"
                : "{ scales: { y: { ticks: { stepSize: 1 } } } }");
        streamWriter.Close();
    }

    public void GeneratePoemIntensityPieChartDataFile()
    {
        var dataDict = new Dictionary<string, int>();

        foreach (var fullDate in Data.Seasons.SelectMany(x => x.Poems).Select(x => x.TextDate)
                     .Where(x => x != "01.01.1994").ToList())
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
        using var streamWriter = new StreamWriter(Path.Combine(rootDir, fileName));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartDataFileHelper.ChartType.Pie);
        chartDataFileHelper.WriteBeforeData();
        chartDataFileHelper.WriteData(dataLines, true);
        chartDataFileHelper.WriteAfterData("poemIntensityPie", new[] { "Les jours de création sont-ils intenses ?" });
        streamWriter.Close();
    }

    public void GeneratePoemByDayOfWeekPieChartDataFile()
    {
        var dataDict = new Dictionary<int, int>();

        foreach (var dayOfWeek in Data.Seasons.SelectMany(x => x.Poems).Where(x => x.TextDate != "01.01.1994")
                     .Select(x => x.Date.DayOfWeek)
                     .ToList())
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
        int[] daysOfWeek = { 1, 2, 3, 4, 5, 6, 0 };
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
        using var streamWriter = new StreamWriter(Path.Combine(rootDir, fileName));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartDataFileHelper.ChartType.Pie);
        chartDataFileHelper.WriteBeforeData();
        chartDataFileHelper.WriteData(dataLines, true);
        chartDataFileHelper.WriteAfterData("poemDayOfWeekPie", new[] { "Par jour de la semaine" });
        streamWriter.Close();
    }

    public void GenerateAcrosticheBarChartDataFile()
    {
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CHART_DATA_FILES_ROOT_DIR]);
        using var streamWriter = new StreamWriter(Path.Combine(rootDir, "acrostiche-bar.js"));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartDataFileHelper.ChartType.Bar, 2);
        chartDataFileHelper.WriteBeforeData();
        var nonAcrosticheDataLines = new List<ChartDataFileHelper.DataLine>();
        var acrosticheDataLines = new List<ChartDataFileHelper.DataLine>();

        foreach (var season in Data.Seasons)
        {
            var acrosticheCount =
                season.Poems.Count(x => !string.IsNullOrEmpty(x.Acrostiche) || x.DoubleAcrostiche != null);
            var nonAcrosticheCount = season.Poems.Count() - acrosticheCount;

            nonAcrosticheDataLines.Add(new ChartDataFileHelper.DataLine($"{season.LongTitle} ({season.Years})",
                nonAcrosticheCount));
            acrosticheDataLines.Add(new ChartDataFileHelper.DataLine($"{season.LongTitle} ({season.Years})",
                acrosticheCount));
        }

        chartDataFileHelper.WriteData(nonAcrosticheDataLines, false);
        chartDataFileHelper.WriteData(acrosticheDataLines, true);

        chartDataFileHelper.WriteAfterData("acrosticheBar", new[] { "Non acrostiche", "Acrostiche" });
        streamWriter.Close();
    }

    public void GeneratePoemIntervalBarChartDataFile(int? seasonId)
    {
        var datesList =
            (seasonId == null ? Data.Seasons.SelectMany(x => x.Poems) : Data.Seasons.First(x => x.Id == seasonId).Poems)
            .Where(x => x.TextDate != "01.01.1994")
            .Select(x => x.Date).Order().ToList();
        var intervalDict = new Dictionary<int, int>();

        int dateCount = datesList.Count();
        for (var i = 1; i < dateCount; i++)
        {
            var current = datesList[i];
            var previous = datesList[i - 1];
            var dayDiff = (int)(current - previous).TotalDays;
            if (intervalDict.ContainsKey(dayDiff))
            {
                intervalDict[dayDiff]++;
            }
            else
            {
                intervalDict.Add(dayDiff, 1);
            }
        }

        var dataLines = new List<ChartDataFileHelper.ColoredDataLine>();
        var orderedIntervalKeys = intervalDict.Keys.Order();
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
                    new ChartDataFileHelper.ColoredDataLine("Moins d\\'un jour", intervalDict[key], zeroDayColor));
            }
            else if (key == 1)
            {
                dataLines.Add(new ChartDataFileHelper.ColoredDataLine("Un jour", intervalDict[key], oneDayColor));
            }
            else if (key < 8)
            {
                dataLines.Add(
                    new ChartDataFileHelper.ColoredDataLine($"{key}j", intervalDict[key], upToSevenDayColor));
            }
            else if (key < 31)
            {
                dataLines.Add(new ChartDataFileHelper.ColoredDataLine($"{key}j", intervalDict[key],
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

        var fileName = seasonId == null ? "poem-interval-bar.js" : $"season-{seasonId}-poem-interval-bar.js";
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CHART_DATA_FILES_ROOT_DIR]);
        using var streamWriter = new StreamWriter(Path.Combine(rootDir, fileName));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartDataFileHelper.ChartType.Bar);
        chartDataFileHelper.WriteBeforeData();
        chartDataFileHelper.WriteData(dataLines, true);
        chartDataFileHelper.WriteAfterData(seasonId == null ? "poemIntervalBar" : $"season{seasonId}PoemIntervalBar",
            new[] { "Fréquence" },
            barChartOptions: seasonId == null ? "{}" : "{ scales: { y: { ticks: { stepSize: 1 } } } }");
        streamWriter.Close();
    }

    public void GeneratePoemCountFile()
    {
        var poemCount = Data.Seasons.Select(x => x.Poems.Count).Sum();
        var poemCountFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CONTENT_ROOT_DIR], "../../common", "poem_count.md");
        File.WriteAllText(poemCountFilePath, poemCount.ToString());
        // EN
        poemCountFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CONTENT_ROOT_DIR], "../../common", "poem_count.md");
        File.WriteAllText(poemCountFilePath, poemCount.ToString());
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
}