using Microsoft.Extensions.Configuration;

namespace Tests;

public static class Helpers
{
    public static IConfiguration GetConfiguration()
    {
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
        configurationBuilder.AddJsonFile("appsettings.Test.json", optional: false, reloadOnChange: true);
        configurationBuilder.AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true, reloadOnChange: true);
        return configurationBuilder.Build();
    }
}