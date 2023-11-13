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
        for (var i = 17; i < seasonMaxId + 1; i++)
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
                new ChartDataFileHelper.DataLine("Quatrains", hasQuatrainValue));

            isSonnetChartData.Add(new ChartDataFileHelper.DataLine(string.Empty, 0));

            nbVersesChartData.Add(new ChartDataFileHelper.DataLine(nbVerses.ToString(),
                nbVersesData[nbVerses] - hasQuatrainValue));
        }

        var index = nbVersesRange.FindIndex(x => x == 14);
        isSonnetChartData[index] = new ChartDataFileHelper.DataLine("Sonnets", nbSonnets);
        nbVersesChartData[index] = new ChartDataFileHelper.DataLine
            (nbVersesChartData[index].Label, nbVersesChartData[index].Value - nbSonnets);

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

    public void GeneratePoemsByDayRadarChartDataFile(string? storageSubCategory)
    {
        var poemStringDates = new List<string>();
        poemStringDates = storageSubCategory == null
            ? Data.Seasons.SelectMany(x => x.Poems).Select(x => x.TextDate).ToList()
            : Data.Seasons.SelectMany(x => x.Poems)
                .Where(x => x.Categories.Any(x => x.SubCategories.Contains(storageSubCategory))).Select(x => x.TextDate)
                .ToList();

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
        var fileName = storageSubCategory != null
            ? $"poems-day-{storageSubCategory.UnaccentedCleaned()}-radar.js"
            : "poems-day-radar.js";
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

        var chartId = storageSubCategory != null
            ? $"poemDay-{storageSubCategory.UnaccentedCleaned()}Radar"
            : "poemDayRadar";
        var borderColor = storageSubCategory != null
            ? _configuration.GetSection(Constants.STORAGE_SETTINGS).Get<StorageSettings>().Categories
                .SelectMany(x => x.Subcategories).FirstOrDefault(x => x.Name == storageSubCategory).Color
            : null;
        var backgroundColor = borderColor?.Replace("1)", "0.5)");

        chartDataFileHelper.WriteAfterData(chartId, new[] { "Poèmes selon le jour de l\\\'année" }, borderColor,
            backgroundColor);
        streamWriter.Close();

        // Second chart
        if (storageSubCategory != null)
            return;

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

    public void GeneratePoemVersesLengthBarChartDataFile()
    {
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CHART_DATA_FILES_ROOT_DIR]);
        using var streamWriter = new StreamWriter(Path.Combine(rootDir, "poems-verse-length-bar.js"));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartDataFileHelper.ChartType.Bar, 1);
        chartDataFileHelper.WriteBeforeData();
        var regularVerseLengthData = new Dictionary<int, int>();
        var variableVerseLengthData = new Dictionary<string, int>();
        var nbUndefinedVerseLength = 0;
        foreach (var poem in Data.Seasons.SelectMany(x => x.Poems))
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
        dataLines.Add(undefinedVerseLengthChartData);

        chartDataFileHelper.WriteData(dataLines, true);

        chartDataFileHelper.WriteAfterData("poemVerseLengthBar", new[] { "Poèmes" });
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

            nonAcrosticheDataLines.Add(new ChartDataFileHelper.DataLine(season.LongTitle, nonAcrosticheCount));
            acrosticheDataLines.Add(new ChartDataFileHelper.DataLine(season.LongTitle, acrosticheCount));
        }

        chartDataFileHelper.WriteData(nonAcrosticheDataLines, false);
        chartDataFileHelper.WriteData(acrosticheDataLines, true);

        chartDataFileHelper.WriteAfterData("acrosticheBar", new[] { "Ordinaire", "Acrostiche" });
        streamWriter.Close();
    }

    public void GeneratePoemIntervalBarChartDataFile()
    {
        var datesList = Data.Seasons.SelectMany(x => x.Poems).Where(x => x.TextDate != "01.01.1994")
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
        
        dataLines.Add(new ChartDataFileHelper.ColoredDataLine("Entre un et trois mois", moreThanOneMonthCount,
            upToThreeMonthsColor));
        
        dataLines.Add(new ChartDataFileHelper.ColoredDataLine("Entre trois mois et un an", moreThanThreeMonthsCount,
            upToOneYearColor));

        dataLines.Add(new ChartDataFileHelper.ColoredDataLine("Plus d\\'un an", moreThanOneYearCount,
            moreThanOneYearColor));

        var fileName = "poem-interval-bar.js";
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CHART_DATA_FILES_ROOT_DIR]);
        using var streamWriter = new StreamWriter(Path.Combine(rootDir, fileName));
        var chartDataFileHelper = new ChartDataFileHelper(streamWriter, ChartDataFileHelper.ChartType.Bar);
        chartDataFileHelper.WriteBeforeData();
        chartDataFileHelper.WriteData(dataLines, true);
        chartDataFileHelper.WriteAfterData("poemIntervalBar",
            new[] { "Fréquence" });
        streamWriter.Close();
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