using Shouldly;
using Xunit;

namespace Tests.Persistence;

public class DataManagerTest(WithRealDataFixture fixture, ITestOutputHelper testOutputHelper)
    : IClassFixture<WithRealDataFixture>
{
    [Fact]
    [Trait("UnitTest", "Persistence")]
    public void ShouldLoad()
    {
        fixture.Data.ShouldNotBeNull();
        fixture.DataEn.ShouldNotBeNull();
    }
}