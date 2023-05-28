using Microsoft.Extensions.Configuration;
using Toolbox;

namespace Tests;

public static class Helpers
{
    public static IConfiguration GetConfiguration()
    {
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        return configurationBuilder.Build();
    }

    public static Engine CreateEngine(bool load = true)
    {
        var engine = new Engine(GetConfiguration());

        if (load)
        {
            engine.Load();
        }

        return engine;
    }
}