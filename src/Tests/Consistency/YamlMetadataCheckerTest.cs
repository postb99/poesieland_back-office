using Shouldly;
using Toolbox.Consistency;
using Xunit;

namespace Tests.Consistency;

public class YamlMetadataCheckerTest(WithRealDataFixture fixture, ITestOutputHelper testOutputHelper)
    : IClassFixture<WithRealDataFixture>
{
    [Fact]
    [Trait("UnitTest", "MetadataCheck")]
    public void ShouldNotFindMissingYearTagInYamlMetadata()
    {
        var anomalies = new YamlMetadataChecker(fixture.Configuration, fixture.Data).GetMissingTagsInYamlMetadata();
        testOutputHelper.WriteLine(string.Join(Environment.NewLine, anomalies));
        anomalies.ShouldBeEmpty();
    }
}