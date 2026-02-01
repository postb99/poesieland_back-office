using Shouldly;
using Tests.Customizations;
using Toolbox.Consistency;
using Toolbox.Domain;
using Xunit;

namespace Tests.Consistency;

public class PoemMetadataCheckerTest(BasicFixture fixture, ITestOutputHelper testOutputHelper)
    : IClassFixture<BasicFixture>
{
    [Theory]
    [Trait("UnitTest", "ConsistencyCheck")]
    [AutoDomainData]
    public void ShouldNotFailWhenPoemHasVerseLength(Root data)
    {
        PoemMetadataChecker.CheckPoemsWithoutVerseLength(data);
    }

    [Theory]
    [Trait("UnitTest", "ConsistencyCheck")]
    [AutoDomainData]
    public void ShouldFailWhenPoemHasEmptyVerseLength(Root data)
    {
        var poem = data.Seasons.First().Poems.First();
        poem.VerseLength = "";
        var act = () => PoemMetadataChecker.CheckPoemsWithoutVerseLength(data);
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
        var act = () => PoemMetadataChecker.CheckPoemsWithoutVerseLength(data);
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
        PoemMetadataChecker.CheckPoemsWithoutVerseLength(data);
    }

    [Theory]
    [Trait("UnitTest", "ConsistencyCheck")]
    [AutoDomainData]
    public void ShouldFailWhenPoemWithVariableVerseLengthHasNotExpectedInfo(Root data)
    {
        var poem = data.Seasons.First().Poems.First();
        poem.VerseLength = "4, 2";
        var act = () => PoemMetadataChecker.CheckPoemsWithVariableMetric(data);
        act.ShouldThrow<Exception>().Message
            .ShouldBe($"[ERROR] First poem with variable metric unspecified in Info: {poem.Id}");
    }
}