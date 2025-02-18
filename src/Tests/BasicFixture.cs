using Microsoft.Extensions.Configuration;

namespace Tests;

public class BasicFixture : IDisposable
{
    public IConfiguration Configuration { get; }

    public BasicFixture()
    {
        // Do "global" initialization here; Only called once.
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
        configurationBuilder.AddJsonFile("appsettings.Test.json", optional: false, reloadOnChange: true);
        configurationBuilder.AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true, reloadOnChange: true);
        Configuration = configurationBuilder.Build();
    }

    public void Dispose()
    {
        // Do "global" teardown here; Only called once.
    }
}