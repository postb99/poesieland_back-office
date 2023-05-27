using FluentAssertions;

namespace Tests;

public class EngineTest
{
    [Fact]
    public void ShouldLoad()
    {
        var engine = EngineHelper.CreateEngine();
        engine.Data.Should().NotBeNull();
    }
}