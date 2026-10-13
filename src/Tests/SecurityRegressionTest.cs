using System.Text;
using Microsoft.Extensions.Configuration;
using Tests.Customizations;
using Toolbox;
using Toolbox.Charts;
using Toolbox.Consistency;
using Toolbox.Domain;
using Toolbox.Generators;
using Toolbox.Importers;
using Toolbox.Persistence;
using Toolbox.Settings;
using Xunit;

namespace Tests;

// Chaque test possède son stockage jetable : les cas d'échec ne touchent ni le site
// ni les XML réels, et peuvent s'exécuter en parallèle sans modifier le répertoire courant.
public sealed class SecurityRegressionTest : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "toolbox-audit-" + Guid.NewGuid().ToString("N"));
    private readonly IConfiguration _configuration;

    public SecurityRegressionTest()
    {
        Directory.CreateDirectory(_directory);
        _configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [Constants.XML_STORAGE_FILE] = Path.Combine(_directory, "fr.xml"),
            [Constants.XML_STORAGE_FILE_EN] = Path.Combine(_directory, "en.xml"),
            [Constants.CONTENT_ROOT_DIR] = Path.Combine(_directory, "seasons"),
            [Constants.CONTENT_ROOT_DIR_EN] = Path.Combine(_directory, "english")
        }).Build();
    }

    [Theory]
    [Trait("UnitTest", "Security")]
    [InlineData("../outside_1")]
    [InlineData("..\\outside_1")]
    [InlineData("C:outside_1")]
    [InlineData("/outside_1")]
    [InlineData("1")]
    public void ShouldRejectPathIdentifiersUsedAsIdOfPoemToImport(string id)
    {
        Assert.Throws<MetadataConsistencyException>(() => new PoemImporter(_configuration).ImportPoem(id, new Root()));
    }

    [Fact]
    [Trait("UnitTest", "Security")]
    public void ShouldWorkForBothLanguagesWhenDoingSaveAndLoadXmlRoundTrip()
    {
        var manager = new DataManager(_configuration);
        manager.Save(new Root());
        manager.Save(new Root { Seasons = [new Season { Id = 1, Name = "Été" }] });
        manager.SaveEn(new Root());
        manager.SaveEn(new Root { Seasons = [new Season { Id = 2 }] });
        manager.Load(out var french, out var english);
        Assert.Equal("Été", Assert.Single(french.Seasons).Name);
        Assert.Equal(2, Assert.Single(english.Seasons).Id);
    }

    [Fact]
    [Trait("UnitTest", "Security")]
    public void ShouldRejectsDtdInXmlFileAndNotPublishPartialReload()
    {
        var manager = new DataManager(_configuration);
        manager.Save(new Root());
        File.WriteAllText(_configuration[Constants.XML_STORAGE_FILE_EN]!,
            $"<!DOCTYPE Saisons [<!ENTITY text 'expanded'>]><Saisons xmlns=\"{Root.XML_NAMESPACE}\" />");
        var french = new Root();
        var english = new Root();
        var originalFrench = french;
        var originalEnglish = english;
        Assert.Throws<InvalidOperationException>(() => manager.Load(out french, out english));
        Assert.Same(originalFrench, french);
        Assert.Same(originalEnglish, english);
    }

    [Theory]
    [Trait("UnitTest", "Security")]
    [InlineAutoDomainData(-2)]
    [InlineAutoDomainData(3)]
    public void ShouldNotReplaceOrRemoveExistingPoemWhenInvalidWeight(int index, Poem poem)
    {
        poem.Id = "test_1";
        var data = new Root { Seasons = [new Season { Id = 1, Poems = [poem] }] };
        Assert.Throws<MetadataConsistencyException>(() => new PoemImporter(_configuration)
            .ImportPoemToSeason(data, new Poem { Id = poem.Id, ContentFileIndex = index }));
        Assert.Same(poem, Assert.Single(data.Seasons[0].Poems));
    }

    [Theory]
    [Trait("UnitTest", "Security")]
    [AutoDomainData]
    public void ShouldPreserveExistingPoemsWhenEnglishImportFails(Poem poem)
    {
        var directory = Path.Combine(_configuration[Constants.CONTENT_ROOT_DIR_EN]!, "2026");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "broken.md"), "---\ndate: not-a-date\n---");
        poem.Id = "old_1";
        var data = new Root { Seasons = [new Season { Id = 1, Poems = [poem] }] };
        Assert.Throws<FormatException>(() => new PoemImporter(_configuration).ImportPoemsEn(data));
        Assert.Same(poem, Assert.Single(data.Seasons[0].Poems));
    }

    [Fact]
    [Trait("UnitTest", "Security")]
    public void ShouldNotAllocateUnboundedStackWhenLongIntegerInput()
    {
        var input = string.Join(',', Enumerable.Repeat(" 12 ", 100_000));
        var result = input.ToIntArray();
        Assert.Equal(100_000, result.Length);
        Assert.All(result, value => Assert.True(value == 12));
    }

    [Fact]
    [Trait("UnitTest", "Security")]
    public void ShouldPreserveDataAndBlocksCodeBoundariesWhenEscapingStringForCharts()
    {
        Assert.Equal("\\\\\\';alert(1);//\\u000a\\u003c/script\\u003e\\u2028","\\';alert(1);//\n</script>\u2028".JavaScriptString());
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true);
        var helper = new ChartDataFileHelper(writer, ChartType.Bar);
        helper.WriteData(new List<ColoredDataLine> { new("l'été\n", 1, "red") });
        helper.WriteAfterData("id'", ["l'été"]);
        var output = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Contains("label: 'l\\'été\\u000a'", output);
        Assert.Contains("color: 'red'", output);
        Assert.Contains("addBarChart('id\\'', ['l\\'été']", output);
    }

    [Fact]
    [Trait("UnitTest", "Security")]
    public void ShouldRejectBubbleRadiusWithJavaScriptContent()
    {
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream);
        var helper = new ChartDataFileHelper(writer, ChartType.Bubble);
        Assert.Throws<FormatException>(() => helper.WriteData(
            new List<BubbleChartDataLine> { new(1, 2, "1};alert(1)//", "red") }, true));
    }

    [Theory]
    [Trait("UnitTest", "Security")]
    [AutoDomainData]
    public void ShouldNotThrowOnDuplicateNormalizedTitles(Poem firstPoem, Poem secondPoem)
    {
        firstPoem.Id = "a_1";
        firstPoem.Title = "Été";
        secondPoem.Id = "b_1";
        secondPoem.Title = "Ete";
        var data = new Root { Seasons = [new Season { Id = 1, Poems = [firstPoem, secondPoem] }] };
        var generator = new ContentFileGenerator(_configuration);
        generator.GenerateSeasonAllPoemFiles(data, 1);
        generator.GeneratePoemFile(data, firstPoem);
        Assert.True(Directory.Exists(_configuration[Constants.CONTENT_ROOT_DIR]));
    }

    [Theory]
    [Trait("UnitTest", "Security")]
    [AutoDomainData]
    public void ShouldPreserveOrderAndIgnoreDuplicateMonthTagsWhenGeneratingWordCloud(List<Poem> poems)
    {
        string[] months = ["janvier", "février", "mars", "avril", "mai", "juin", "juillet", "août", "septembre", "octobre", "novembre", "décembre"];
        foreach (var month in months)
            Directory.CreateDirectory(Path.Combine(_directory, "other-perspectives", "les-mois", month));
        
        poems[0].ExtraTags = ["janvier", "janvier"];
        poems[0].WordCloud = "ÉTÉ";
        poems[1].ExtraTags = null;
        poems[2].ExtraTags = ["janvier"];
        poems[2].WordCloud = null;
        var data = new Root { Seasons = [new Season { Poems = poems }] };
        var generator = new WordCloudTextGenerator(_configuration);
        generator.GenerateWordCloudFiles(data);
        generator.GenerateWordCloudFiles(data);
        Assert.Equal("été" + Environment.NewLine + Environment.NewLine, File.ReadAllText(Path.Combine(_directory, "other-perspectives", "les-mois", "janvier", "wordcloud.txt")));
        Assert.Empty(File.ReadAllText(Path.Combine(_directory, "other-perspectives", "les-mois", "février", "wordcloud.txt")));
    }

    [Fact]
    [Trait("UnitTest", "Security")]
    public void ShouldHandleWhenBodyCannotReopenMetadataAndChangeIdentity()
    {
        var path = Path.Combine(_directory, "poem.md");
        File.WriteAllText(path, "+++\nid = \"original_1\"\ndate = 2026-09-08\nverseLength = 8\n+++\n---\nid = \"forged_2\"\n---");
        var importer = new PoemImporter(_configuration);
        Assert.Equal("original_1", importer.Import(path).Id);
        Assert.Equal("original_1", importer.GetPartialImport(path).PoemId);
    }

    [Theory]
    [Trait("UnitTest", "Security")]
    [InlineData("plain text")]
    [InlineData("+++\nid = \"test_1\"")]
    [InlineData("+++\n---")]
    public void ShouldFailWhenMalformedMetadata(string content)
    {
        var path = Path.Combine(_directory, "poem.md");
        File.WriteAllText(path, content);
        Assert.Throws<InvalidDataException>(() => new PoemImporter(_configuration).Import(path));
    }

    [Fact]
    [Trait("UnitTest", "Security")]
    public void ShouldNotReuseDescriptionFromPreviousFileWhenImportingAnotherSeason()
    {
        var path = Path.Combine(_directory, "season.md");
        var importer = new SeasonIndexImporter();
        File.WriteAllText(path, "+++\ndescription = \"first\"\n+++");
        Assert.Equal("first", importer.Import(path).Description);
        File.WriteAllText(path, "+++\ndescription = \"second\"\n+++");
        Assert.Equal("second", importer.Import(path).Description);
    }

    [Fact]
    [Trait("UnitTest", "Security")]
    public void ShouldKeepPoemsBeyondPositionFiftyAndIgnoreOtherFilesWhenImportingSeason()
    {
        var directory = Path.Combine(_configuration[Constants.CONTENT_ROOT_DIR]!, "1_first");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "poem.md"),
            "+++\nid = \"poem_1\"\ndate = 2026-09-08\nverseLength = 8\nweight = 51\ntags = [\"2026\", \"octosyllabe\"]\n+++");
        File.WriteAllText(Path.Combine(directory, "image.png"), "not a poem");
        var configuration = new ConfigurationBuilder().AddConfiguration(_configuration)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MetricSettings:Metrics:0:Name"] = "octosyllabe",
                ["MetricSettings:Metrics:0:Length"] = "8"
            }).Build();
        var data = new Root();
        new PoemImporter(configuration).ImportPoemsOfSeason(1, data);
        Assert.Equal(50, Assert.Single(Assert.Single(data.Seasons).Poems).ContentFileIndex);
    }

    [Fact]
    [Trait("UnitTest", "Security")]
    public void ShouldTreatRegexCharactersLiterallyWhenDoingCustomPageSearch()
    {
        var directory = Path.Combine(_directory, "tags", "saisons");
        Directory.CreateDirectory(directory);
        var page = Path.Combine(directory, "_index.md");
        var poem = new Poem { Id = "a.b_1", Categories = [new() { Name = "Saisons", SubCategories = ["été", "hiver"] }] };
        var checker = new CustomPageChecker(_configuration);
        File.WriteAllText(page, "../../seasons/1_first/axb");
        Assert.Throws<CustomPageConsistencyException>(() => checker.VerifyPoemOfMoreThanOneSeasonIsListedOnCustomPage(poem, new Root()));
        File.WriteAllText(page, "../../seasons/1_first/a.b");
        checker.VerifyPoemOfMoreThanOneSeasonIsListedOnCustomPage(poem, new Root());
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
