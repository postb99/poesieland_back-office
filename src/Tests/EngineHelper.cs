using Microsoft.Extensions.Configuration;
using Toolbox;

namespace Tests;

public static class EngineHelper
{
    public static Engine CreateEngine(bool load = true)
    {
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        var configuration = configurationBuilder.Build();

        var engine = new Engine(configuration);

        if (load)
        {
            engine.Load();
        }

        return engine;
    }
}