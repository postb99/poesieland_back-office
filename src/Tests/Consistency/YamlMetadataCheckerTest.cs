using Shouldly;
using Toolbox.Consistency;
using Xunit;

namespace Tests.Consistency;

public class YamlMetadataCheckerTest(WithRealDataFixture fixture, ITestOutputHelper testOutputHelper)
    : IClassFixture<WithRealDataFixture>
{
    [Fact]
    [Trait("UnitTest", "MetadataCheck")]
    public async Task ShouldNotFindMissingYearTagInYamlMetadata()
    {
        var anomalies = await new YamlMetadataChecker(fixture.Configuration, fixture.Data).GetYamlMetadataAnomaliesAcrossSeasonsAsync().ToListAsync();
        testOutputHelper.WriteLine(string.Join(Environment.NewLine, anomalies));
        anomalies.ShouldBeEmpty();
    }
}
