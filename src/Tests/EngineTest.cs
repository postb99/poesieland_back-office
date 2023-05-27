using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Toolbox;

namespace Tests;

public class EngineTest
{
    [Fact]
    public void ShouldLoad()
    {
        var engine = Init();
        engine.Load();
        engine.Data.Should().NotBeNull();
    }

    private Engine Init()
    {
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        var configuration = configurationBuilder.Build();

        return new Engine(configuration);
    }
}