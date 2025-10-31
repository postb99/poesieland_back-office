using Shouldly;
using Xunit;

namespace Tests.Persistence;

public class DataManagerTest(LoadDataFixture fixture, ITestOutputHelper testOutputHelper)
    : IClassFixture<LoadDataFixture>
{
    [Fact]
    [Trait("UnitTest", "Persistence")]
    public void ShouldLoad()
    {
        fixture.Data.ShouldNotBeNull();
        fixture.DataEn.ShouldNotBeNull();
    }
}