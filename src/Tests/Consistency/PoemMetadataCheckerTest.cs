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
        PoemMetadataChecker.CheckPoemsWithoutMetricValueSpecified(data);
    }

    [Theory]
    [Trait("UnitTest", "ConsistencyCheck")]
    [AutoDomainData]
    public void ShouldThrowWhenPoemHasEmptyVerseLength(Root data)
    {
        var poem = data.Seasons.First().Poems.First();
        poem.VerseLength = "";
        var act = () => PoemMetadataChecker.CheckPoemsWithoutMetricValueSpecified(data);
        act.ShouldThrow<MetadataConsistencyException>().Message
            .ShouldBe($"First poem with unspecified metric or equal to '0': {poem.Id}");
    }

    [Theory]
    [Trait("UnitTest", "ConsistencyCheck")]
    [AutoDomainData]
    public void ShouldThrowWhenPoemHasZeroVerseLength(Root data)
    {
        var poem = data.Seasons.First().Poems.First();
        poem.VerseLength = "0";
        var act = () => PoemMetadataChecker.CheckPoemsWithoutMetricValueSpecified(data);
        act.ShouldThrow<MetadataConsistencyException>().Message
            .ShouldBe($"First poem with unspecified metric or equal to '0': {poem.Id}");
    }

    [Theory]
    [Trait("UnitTest", "ConsistencyCheck")]
    [AutoDomainData]
    public void ShouldNotFailWhenPoemWithVariableVerseLengthHasExpectedInfo(Root data)
    {
        var poem = data.Seasons.First().Poems.First();
        poem.VerseLength = "4, 2";
        poem.Info = "Métrique variable : 4, 2";
        PoemMetadataChecker.CheckPoemsWithoutMetricValueSpecified(data);
    }

    [Theory]
    [Trait("UnitTest", "ConsistencyCheck")]
    [InlineAutoDomainData(null)]
    [InlineAutoDomainData("")]
    [InlineAutoDomainData("Info")]
    public void ShouldThrowWhenPoemWithVariableVerseLengthHasNotExpectedInfo(string? info, Root? data)
    {
        var poem = data.Seasons.First().Poems.First();
        poem.VerseLength = "4, 2";
        poem.Info = info;
        var act = () => PoemMetadataChecker.CheckPoemsWithVariableMetricNotPresentInInfo(data);
        act.ShouldThrow<MetadataConsistencyException>().Message
            .ShouldBe($"First poem with variable metric unspecified in Info: {poem.Id}");
    }

    [Theory]
    [Trait("UnitTest", "ConsistencyCheck")]
    [InlineAutoDomainData("")]
    [InlineAutoDomainData("0")]
    [InlineAutoDomainData(null)]
    public void ShouldThrowWhenMetricIsUnspecified(string? metric, PoemImporter.PartialImport? partialImport)
    {
        ArrangePartialImport(partialImport, 2000, metric, "Info");
        var act = () => PoemMetadataChecker.VerifyMetricValueIsSpecified(partialImport);
        act.ShouldThrow<MetadataConsistencyException>().Message.ShouldBe("Poem metric is unspecified");
    }

    [Theory]
    [Trait("UnitTest", "ConsistencyCheck")]
    [AutoDomainData]
    public void ShouldThrowWhenYearTagIsMissing(PoemImporter.PartialImport partialImport)
    {
        ArrangePartialImport(partialImport, 2000, "4", "Info");
        partialImport.Tags.Remove("2000");
        var act = () => PoemMetadataChecker.VerifyYearTagIsPresent(partialImport);
        act.ShouldThrow<MetadataConsistencyException>().Message.ShouldBe($"Missing year tag: {partialImport.Year}");
    }

    [Theory]
    [Trait("UnitTest", "ConsistencyCheck")]
    [AutoDomainData]
    public void ShouldThrowWhenVariableMetricTagIsMissing(PoemImporter.PartialImport partialImport)
    {
        ArrangePartialImport(partialImport, 2000, "4, 2", "Métrique variable : 4, 2");
        partialImport.HasVariableMetric = true;
        partialImport.Tags.Remove("métrique variable");
        var act = () => PoemMetadataChecker.VerifyVariableMetricTagIsPresent(partialImport);
        act.ShouldThrow<MetadataConsistencyException>().Message.ShouldBe("Missing 'métrique variable' tag");
    }

    [Theory]
    [Trait("UnitTest", "ConsistencyCheck")]
    [AutoDomainData]
    public void ShouldThrowWhenVariableMetricInfoIsMissing(PoemImporter.PartialImport partialImport)
    {
        ArrangePartialImport(partialImport, 2000, "4, 2", "Info");
        partialImport.HasVariableMetric = true;
        var act = () => PoemMetadataChecker.VerifyVariableMetricInfoIsPresent(partialImport);
        act.ShouldThrow<MetadataConsistencyException>().Message.ShouldBe("Missing 'Métrique variable : ' in Info");
    }

    [Theory]
    [Trait("UnitTest", "ConsistencyCheck")]
    [AutoDomainData]
    public void ShouldThrowWhenMetricTagIsMissing(PoemImporter.PartialImport partialImport)
    {
        ArrangePartialImport(partialImport, 2000, "4", "Info");
        partialImport.Tags.Remove("testmetric4");
        var act = () => PoemMetadataChecker.VerifyMetricTagsArePresent(partialImport, CreateMetrics());
        act.ShouldThrow<MetadataConsistencyException>().Message.ShouldBe("Missing 'testmetric4' tag");
    }

    [Theory]
    [Trait("UnitTest", "ConsistencyCheck")]
    [AutoDomainData]
    public void ShouldThrowWhenRequiredDescriptionIsMissing(PoemImporter.PartialImport partialImport, string extraTag)
    {
        partialImport.Tags = [extraTag];
        partialImport.Description = "";
        var act = () => PoemMetadataChecker.VerifyRequiredDescription(partialImport,
            [new RequiredDescription { ExtraTag = extraTag, Bold = false }]);
        act.ShouldThrow<MetadataConsistencyException>().Message
            .ShouldBe($"Poem {partialImport.PoemId} is missing description because of extra tag '{extraTag}'");
    }

    [Theory]
    [Trait("UnitTest", "ConsistencyCheck")]
    [AutoDomainData]
    public void ShouldThrowWhenRequiredDescriptionDoesNotContainBoldAsRequired(PoemImporter.PartialImport partialImport,
        string extraTag)
    {
        partialImport.Tags = [extraTag];
        partialImport.Description = "Not bold text";
        var act = () => PoemMetadataChecker.VerifyRequiredDescription(partialImport,
            [new RequiredDescription { ExtraTag = extraTag, Bold = true }]);
        act.ShouldThrow<MetadataConsistencyException>().Message
            .ShouldBe(
                $"Poem {partialImport.PoemId} description is missing bold formatting because of extra tag '{extraTag}'");
    }

    [Theory]
    [Trait("UnitTest", "ConsistencyCheck")]
    [AutoDomainData]
    public void ShouldNotThrowWhenRequiredDescriptionIsPresent(PoemImporter.PartialImport partialImport,
        string extraTag)
    {
        partialImport.Tags = [extraTag];
        partialImport.Description = "Some ordinary text";
        PoemMetadataChecker.VerifyRequiredDescription(partialImport, [new RequiredDescription { ExtraTag = extraTag, Bold = false }]);
    }

    [Theory]
    [Trait("UnitTest", "ConsistencyCheck")]
    [AutoDomainData]
    public void ShouldNotThrowWhenRequiredDescriptionContainsBoldAsRequired(PoemImporter.PartialImport partialImport,
        string extraTag)
    {
        partialImport.Tags = [extraTag];
        partialImport.Description = "Some **Bold text**";
        PoemMetadataChecker.VerifyRequiredDescription(partialImport, [new RequiredDescription { ExtraTag = extraTag, Bold = true }]);
    }
}