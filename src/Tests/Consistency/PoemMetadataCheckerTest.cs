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
    private static PoemImporter.PartialImport CreateValidPartialImport(int year, string detailedMetric, string info)
    {
        return new()
        {
            Year = year,
            DetailedMetric = detailedMetric,
            Info = info,
            Tags = [$"{year}", "testmetric4", "testmetric2", "métrique variable"],
            HasVariableMetric = false,
            PoemId = "poem-1"
        };
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

    [Fact]
    [Trait("UnitTest", "ConsistencyCheck")]
    public void ShouldReturnUnspecifiedMetricAnomaly()
    {
        var partialImport = CreateValidPartialImport(2000, "0", "Info");
        partialImport.Tags.Add("zero");
        var anomalies = PoemMetadataChecker.CheckAnomalies(partialImport, CreateMetrics());
        anomalies.ShouldContain("Poem metric is unspecified");
    }

    [Fact]
    [Trait("UnitTest", "ConsistencyCheck")]
    public void ShouldReturnMissingYearTagAnomaly()
    {
        var partialImport = CreateValidPartialImport(2000, "4", "Info");
        partialImport.Tags.Remove("2000");
        var anomalies = PoemMetadataChecker.CheckAnomalies(partialImport, CreateMetrics());
        anomalies.ShouldContain("Missing year tag");
    }

    [Fact]
    [Trait("UnitTest", "ConsistencyCheck")]
    public void ShouldReturnMissingVariableMetricTagAnomaly()
    {
        var partialImport = CreateValidPartialImport(2000, "4, 2", "Métrique variable : 4, 2");
        partialImport.HasVariableMetric = true;
        partialImport.Tags.Remove("métrique variable");
        var anomalies = PoemMetadataChecker.CheckAnomalies(partialImport, CreateMetrics());
        anomalies.ShouldContain("Missing 'métrique variable' tag");
    }

    [Fact]
    [Trait("UnitTest", "ConsistencyCheck")]
    public void ShouldReturnMissingVariableMetricInfoAnomaly()
    {
        var partialImport = CreateValidPartialImport(2000, "4, 2", "Info");
        partialImport.HasVariableMetric = true;
        var anomalies = PoemMetadataChecker.CheckAnomalies(partialImport, CreateMetrics());
        anomalies.ShouldContain("Missing 'Métrique variable : ' in Info");
    }

    [Fact]
    [Trait("UnitTest", "ConsistencyCheck")]
    public void ShouldReturnMissingMetricTagAnomaly()
    {
        var partialImport = CreateValidPartialImport(2000, "4", "Info");
        partialImport.Tags.Remove("testmetric4");
        var anomalies = PoemMetadataChecker.CheckAnomalies(partialImport, CreateMetrics());
        anomalies.ShouldContain("Missing 'testmetric4' tag");
    }
}
