using System.Text;
using Shouldly;
using Microsoft.Extensions.Configuration;
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
    public void RejectsPathIdentifiersBeforeDiskAccess(string id)
    {
        Should.Throw<MetadataConsistencyException>(() => new PoemImporter(_configuration).ImportPoem(id, new Root()));
    }

    [Fact]
    [Trait("UnitTest", "Security")]
    public void FailedXmlSerializationPreservesPreviousFileAndCleansTemporaryFile()
    {
        var manager = new DataManager(_configuration);
        manager.Save(new Root());
        var previous = File.ReadAllBytes(_configuration[Constants.XML_STORAGE_FILE]!);
        var invalid = new Root { Seasons = [new Season { Name = "invalid\u0001" }] };
        Should.Throw<InvalidOperationException>(() => manager.Save(invalid));
        (File.ReadAllBytes(_configuration[Constants.XML_STORAGE_FILE]!)).ShouldBe(previous);
        (Directory.GetFiles(_directory, "*.tmp")).ShouldBeEmpty();
    }

    [Fact]
    [Trait("UnitTest", "Security")]
    public void XmlRoundTripAndReplacementWorkForBothLanguages()
    {
        var manager = new DataManager(_configuration);
        manager.Save(new Root());
        manager.Save(new Root { Seasons = [new Season { Id = 1, Name = "Été" }] });
        manager.SaveEn(new Root());
        manager.SaveEn(new Root { Seasons = [new Season { Id = 2 }] });
        manager.Load(out var french, out var english);
        ((french.Seasons).ShouldHaveSingleItem().Name).ShouldBe("Été");
        ((english.Seasons).ShouldHaveSingleItem().Id).ShouldBe(2);
    }

    [Fact]
    [Trait("UnitTest", "Security")]
    public void XmlRejectsDtdAndDoesNotPublishPartialReload()
    {
        var manager = new DataManager(_configuration);
        manager.Save(new Root());
        File.WriteAllText(_configuration[Constants.XML_STORAGE_FILE_EN]!,
            $"<!DOCTYPE Saisons [<!ENTITY text 'expanded'>]><Saisons xmlns=\"{Root.XML_NAMESPACE}\" />");
        var french = new Root();
        var english = new Root();
        var originalFrench = french;
        var originalEnglish = english;
        Should.Throw<InvalidOperationException>(() => manager.Load(out french, out english));
        (french).ShouldBeSameAs(originalFrench);
        (english).ShouldBeSameAs(originalEnglish);
    }

    [Theory]
    [Trait("UnitTest", "Security")]
    [InlineData(-2)]
    [InlineData(3)]
    public void InvalidWeightDoesNotReplaceOrRemoveExistingPoem(int index)
    {
        var original = new Poem { Id = "test_1" };
        var data = new Root { Seasons = [new Season { Id = 1, Poems = [original] }] };
        Should.Throw<MetadataConsistencyException>(() => new PoemImporter(_configuration)
            .ImportPoemToSeason(data, new Poem { Id = original.Id, ContentFileIndex = index }));
        ((data.Seasons[0].Poems).ShouldHaveSingleItem()).ShouldBeSameAs(original);
    }

    [Fact]
    [Trait("UnitTest", "Security")]
    public void FailedEnglishImportPreservesExistingPoems()
    {
        var directory = Path.Combine(_configuration[Constants.CONTENT_ROOT_DIR_EN]!, "2026");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "broken.md"), "---\ndate: not-a-date\n---");
        var original = new Poem { Id = "old_1" };
        var data = new Root { Seasons = [new Season { Id = 1, Poems = [original] }] };
        Should.Throw<FormatException>(() => new PoemImporter(_configuration).ImportPoemsEn(data));
        ((data.Seasons[0].Poems).ShouldHaveSingleItem()).ShouldBeSameAs(original);
    }

    [Fact]
    [Trait("UnitTest", "Security")]
    public void LongIntegerInputDoesNotAllocateUnboundedStack()
    {
        var input = string.Join(',', Enumerable.Repeat(" 12 ", 100_000));
        var result = input.ToIntArray();
        (result.Length).ShouldBe(100_000);
        result.ShouldAllBe(value => value == 12);
    }

    [Fact]
    [Trait("UnitTest", "Security")]
    public void ChartEscapingShouldPreserveDataAndBlocksCodeBoundaries()
    {
        "\\';alert(1);//\n</script>\u2028".JavaScriptString().ShouldBe("\\\\\\';alert(1);//\\u000a\\u003c/script\\u003e\\u2028");
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true);
        var helper = new ChartDataFileHelper(writer, ChartType.Bar);
        helper.WriteData(new List<ColoredDataLine> { new("l'été\n", 1, "red'") });
        helper.WriteAfterData("id'", ["l'été"]);
        var output = Encoding.UTF8.GetString(stream.ToArray());
        (output).ShouldContain("label: 'l\\'été\\u000a'");
        (output).ShouldContain("color: 'red\\''");
        (output).ShouldContain("addBarChart('id\\'', ['l\\'été']");
    }

    [Fact]
    [Trait("UnitTest", "Security")]
    public void BubbleRadiusCannotContainJavaScript()
    {
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream);
        var helper = new ChartDataFileHelper(writer, ChartType.Bubble);
        Should.Throw<FormatException>(() => helper.WriteData(
            new List<BubbleChartDataLine> { new(1, 2, "1};alert(1)//", "red") }, true));
    }

    [Fact]
    [Trait("UnitTest", "Security")]
    public void DuplicateNormalizedFilenamesAreRejectedBeforeWriting()
    {
        var first = new Poem { Id = "a_1", Title = "Été" };
        var data = new Root { Seasons = [new Season { Id = 1, Poems = [first, new Poem { Id = "b_1", Title = "Ete" }] }] };
        var generator = new ContentFileGenerator(_configuration);
        Should.Throw<InvalidDataException>(() => generator.GenerateSeasonAllPoemFiles(data, 1).ToList());
        Should.Throw<InvalidDataException>(() => generator.GeneratePoemFile(data, first));
        (Directory.Exists(_configuration[Constants.CONTENT_ROOT_DIR])).ShouldBeFalse();
    }

    [Fact]
    [Trait("UnitTest", "Security")]
    public void WordCloudPreservesOrderAndIgnoresDuplicateMonthTags()
    {
        string[] months = ["janvier", "février", "mars", "avril", "mai", "juin", "juillet", "août", "septembre", "octobre", "novembre", "décembre"];
        foreach (var month in months)
            Directory.CreateDirectory(Path.Combine(_directory, "other-perspectives", "les-mois", month));
        var data = new Root { Seasons = [new Season { Poems = [
            new Poem { ExtraTags = ["janvier", "janvier"], WordCloud = "ÉTÉ" },
            new Poem { ExtraTags = null },
            new Poem { ExtraTags = ["janvier"], WordCloud = null }
        ] }] };
        var generator = new WordCloudTextGenerator(_configuration);
        generator.GenerateWordCloudFiles(data);
        generator.GenerateWordCloudFiles(data);
        (File.ReadAllText(Path.Combine(_directory, "other-perspectives", "les-mois", "janvier", "wordcloud.txt"))).ShouldBe("été" + Environment.NewLine + Environment.NewLine);
        (File.ReadAllText(Path.Combine(_directory, "other-perspectives", "les-mois", "février", "wordcloud.txt"))).ShouldBeEmpty();
    }

    [Fact]
    [Trait("UnitTest", "Security")]
    public void BodyCannotReopenMetadataAndChangeIdentity()
    {
        var path = Path.Combine(_directory, "poem.md");
        File.WriteAllText(path, "+++\nid = \"original_1\"\ndate = 2026-09-08\nverseLength = 8\n+++\n---\nid = \"forged_2\"\n---");
        var importer = new PoemImporter(_configuration);
        importer.Import(path).Id.ShouldBe("original_1");
        importer.GetPartialImport(path).PoemId.ShouldBe("original_1");
    }

    [Theory]
    [Trait("UnitTest", "Security")]
    [InlineData("plain text")]
    [InlineData("+++\nid = \"test_1\"")]
    [InlineData("+++\n---")]
    public void MalformedMetadataFailsExplicitly(string content)
    {
        var path = Path.Combine(_directory, "poem.md");
        File.WriteAllText(path, content);
        Should.Throw<InvalidDataException>(() => new PoemImporter(_configuration).Import(path));
    }

    [Fact]
    [Trait("UnitTest", "Security")]
    public void SeasonImporterDoesNotReuseDescriptionFromPreviousFile()
    {
        var path = Path.Combine(_directory, "season.md");
        var importer = new SeasonIndexImporter();
        File.WriteAllText(path, "+++\ndescription = \"first\"\n+++");
        importer.Import(path).Description.ShouldBe("first");
        File.WriteAllText(path, "+++\ndescription = \"second\"\n+++");
        importer.Import(path).Description.ShouldBe("second");
    }

    [Fact]
    [Trait("UnitTest", "Security")]
    public void SeasonImportKeepsPoemsBeyondPositionFiftyAndIgnoresOtherFiles()
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
        data.Seasons.ShouldHaveSingleItem().Poems.ShouldHaveSingleItem().ContentFileIndex.ShouldBe(50);
    }

    [Fact]
    [Trait("UnitTest", "Security")]
    public void CustomPageSearchTreatsRegexCharactersLiterally()
    {
        var directory = Path.Combine(_directory, "tags", "saisons");
        Directory.CreateDirectory(directory);
        var page = Path.Combine(directory, "_index.md");
        var poem = new Poem { Id = "a.b_1", Categories = [new() { Name = "Saisons", SubCategories = ["été", "hiver"] }] };
        var checker = new CustomPageChecker(_configuration);
        File.WriteAllText(page, "../../seasons/1_first/axb");
        Should.Throw<CustomPageConsistencyException>(() => checker.VerifyPoemOfMoreThanOneSeasonIsListedOnCustomPage(poem, new Root()));
        File.WriteAllText(page, "../../seasons/1_first/a.b");
        checker.VerifyPoemOfMoreThanOneSeasonIsListedOnCustomPage(poem, new Root());
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
