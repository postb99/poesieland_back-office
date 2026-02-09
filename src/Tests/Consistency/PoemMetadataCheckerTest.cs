using Shouldly;
using Tests.Customizations;
using Toolbox.Consistency;
using Toolbox.Domain;
using Toolbox.Importers;
using Toolbox.Settings;
using Xunit;

namespace Tests.Consistency;

public class PoemMetadataCheckerTest : IClassFixture<BasicFixture>
{
    private static void ArrangePartialImport(PoemImporter.PartialImport partialImport, int year, string detailedMetric,
        string info)
    {
        partialImport.Year = year;
        partialImport.DetailedMetric = detailedMetric;
        partialImport.Info = info;
        partialImport.Tags = [$"{year}", "testmetric4", "testmetric2", "métrique variable"];
        partialImport.HasVariableMetric = false;
    }

    private static List<Metric> CreateMetrics()
    {
        return
        [
            new() { Length = 0, Name = "zero", Color = "blue" },
            new() { Length = 2, Name = "TestMetric2", Color = "blue" },
            new() { Length = 4, Name = "TestMetric4", Color = "blue" }
        ];
    }

    [Theory]
    [Trait("UnitTest", "ConsistencyCheck")]
    [AutoDomainData]
    public void ShouldNotFailWhenPoemHasVerseLength(Root data)
    {
        PoemMetadataChecker.CheckPoemsWithoutMetricSpecified(data);
    }

    [Theory]
    [Trait("UnitTest", "ConsistencyCheck")]
    [AutoDomainData]
    public void ShouldFailWhenPoemHasEmptyVerseLength(Root data)
    {
        var poem = data.Seasons.First().Poems.First();
        poem.VerseLength = "";
        var act = () => PoemMetadataChecker.CheckPoemsWithoutMetricSpecified(data);
        act.ShouldThrow<Exception>().Message
            .ShouldBe($"[ERROR] First poem with unspecified metric or equal to '0': {poem.Id}");
    }

    [Theory]
    [Trait("UnitTest", "ConsistencyCheck")]
    [AutoDomainData]
    public void ShouldFailWhenPoemHasZeroVerseLength(Root data)
    {
        var poem = data.Seasons.First().Poems.First();
        poem.VerseLength = "0";
        var act = () => PoemMetadataChecker.CheckPoemsWithoutMetricSpecified(data);
        act.ShouldThrow<Exception>().Message
            .ShouldBe($"[ERROR] First poem with unspecified metric or equal to '0': {poem.Id}");
    }

    [Theory]
    [Trait("UnitTest", "ConsistencyCheck")]
    [AutoDomainData]
    public void ShouldNotFailWhenPoemWithVariableVerseLengthHasExpectedInfo(Root data)
    {
        var poem = data.Seasons.First().Poems.First();
        poem.VerseLength = "4, 2";
        poem.Info = "Métrique variable : 4, 2";
        PoemMetadataChecker.CheckPoemsWithoutMetricSpecified(data);
    }

    [Theory]
    [Trait("UnitTest", "ConsistencyCheck")]
    [AutoDomainData]
    public void ShouldFailWhenPoemWithVariableVerseLengthHasNotExpectedInfo(Root data)
    {
        var poem = data.Seasons.First().Poems.First();
        poem.VerseLength = "4, 2";
        var act = () => PoemMetadataChecker.CheckPoemsWithVariableMetricNotPresentInInfo(data);
        act.ShouldThrow<Exception>().Message
            .ShouldBe($"[ERROR] First poem with variable metric unspecified in Info: {poem.Id}");
    }

    [Theory]
    [Trait("UnitTest", "ConsistencyCheck")]
    [AutoDomainData]
    public void ShouldReturnUnspecifiedMetricAnomaly(PoemImporter.PartialImport partialImport)
    {
        ArrangePartialImport(partialImport, 2000, "0", "Info");
        partialImport.Tags.Add("zero");
        var anomalies = PoemMetadataChecker.CheckAnomalies(partialImport, CreateMetrics());
        anomalies.ShouldContain("Poem metric is unspecified");
    }

    [Theory]
    [Trait("UnitTest", "ConsistencyCheck")]
    [AutoDomainData]
    public void ShouldReturnMissingYearTagAnomaly(PoemImporter.PartialImport partialImport)
    {
        ArrangePartialImport(partialImport, 2000, "4", "Info");
        partialImport.Tags.Remove("2000");
        var anomalies = PoemMetadataChecker.CheckAnomalies(partialImport, CreateMetrics());
        anomalies.ShouldContain("Missing year tag");
    }

    [Theory]
    [Trait("UnitTest", "ConsistencyCheck")]
    [AutoDomainData]
    public void ShouldReturnMissingVariableMetricTagAnomaly(PoemImporter.PartialImport partialImport)
    {
        ArrangePartialImport(partialImport, 2000, "4, 2", "Métrique variable : 4, 2");
        partialImport.HasVariableMetric = true;
        partialImport.Tags.Remove("métrique variable");
        var anomalies = PoemMetadataChecker.CheckAnomalies(partialImport, CreateMetrics());
        anomalies.ShouldContain("Missing 'métrique variable' tag");
    }

    [Theory]
    [Trait("UnitTest", "ConsistencyCheck")]
    [AutoDomainData]
    public void ShouldReturnMissingVariableMetricInfoAnomaly(PoemImporter.PartialImport partialImport)
    {
        ArrangePartialImport(partialImport, 2000, "4, 2", "Info");
        partialImport.HasVariableMetric = true;
        var anomalies = PoemMetadataChecker.CheckAnomalies(partialImport, CreateMetrics());
        anomalies.ShouldContain("Missing 'Métrique variable : ' in Info");
    }

    [Theory]
    [Trait("UnitTest", "ConsistencyCheck")]
    [AutoDomainData]
    public void ShouldReturnMissingMetricTagAnomaly(PoemImporter.PartialImport partialImport)
    {
        ArrangePartialImport(partialImport, 2000, "4", "Info");
        partialImport.Tags.Remove("testmetric4");
        var anomalies = PoemMetadataChecker.CheckAnomalies(partialImport, CreateMetrics());
        anomalies.ShouldContain("Missing 'testmetric4' tag");
    }

    [Theory]
    [Trait("UnitTest", "ConsistencyCheck")]
    [AutoDomainData]
    public void ShouldThrowWhenRequiredDescriptionIsMissing(Poem poem, string extraTag)
    {
        poem.ExtraTags = [extraTag];
        poem.Description = "";
        var act = () => PoemMetadataChecker.CheckRequiredDescription(poem, new RequiredDescriptionSettings
        {
            RequiredDescriptions = [new RequiredDescription { ExtraTag = extraTag, Bold = false }]
        });
        act.ShouldThrow<Exception>().Message
            .ShouldBe($"Poem {poem.Id} is missing description because of extra tag '{extraTag}'");
    }

    [Theory]
    [Trait("UnitTest", "ConsistencyCheck")]
    [AutoDomainData]
    public void ShouldThrowWhenRequiredDescriptionDoesNotContainBoldAsRequired(Poem poem, string extraTag)
    {
        poem.ExtraTags = [extraTag];
        poem.Description = "Not bold text";
        var act = () => PoemMetadataChecker.CheckRequiredDescription(poem, new RequiredDescriptionSettings
        {
            RequiredDescriptions = [new RequiredDescription { ExtraTag = extraTag, Bold = true }]
        });
        act.ShouldThrow<Exception>().Message
            .ShouldBe($"Poem {poem.Id} description is missing bold formatting because of extra tag '{extraTag}'");
    }

    [Theory]
    [Trait("UnitTest", "ConsistencyCheck")]
    [AutoDomainData]
    public void ShouldNotThrowWhenRequiredDescriptionIsPresent(Poem poem, string extraTag)
    {
        poem.ExtraTags = [extraTag];
        poem.Description = "Some ordinary text";
        PoemMetadataChecker.CheckRequiredDescription(poem, new RequiredDescriptionSettings
            {
                RequiredDescriptions = [new RequiredDescription { ExtraTag = extraTag, Bold = false }]
            }
        );
    }

    [Theory]
    [Trait("UnitTest", "ConsistencyCheck")]
    [AutoDomainData]
    public void ShouldNotThrowWhenRequiredDescriptionContainsBoldAsRequired(Poem poem, string extraTag)
    {
        poem.ExtraTags = [extraTag];
        poem.Description = "Some **Bold text**";
        PoemMetadataChecker.CheckRequiredDescription(poem, new RequiredDescriptionSettings
            {
                RequiredDescriptions = [new RequiredDescription { ExtraTag = extraTag, Bold = true }]
            }
        );
    }
}