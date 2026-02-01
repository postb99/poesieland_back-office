using System.Text;
using Microsoft.Extensions.Configuration;
using Tests.Persistence;
using Toolbox.Persistence;

namespace Tests;

public class BasicFixture : IDisposable
{
    public IConfiguration Configuration { get; }
    
    public IDataManager DataManager { get; }

    public BasicFixture()
    {
        // Do "global" initialization here; Only called once.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
        configurationBuilder.AddJsonFile("appsettings.Test.json", optional: false, reloadOnChange: true);
        configurationBuilder.AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true, reloadOnChange: true);
        Configuration = configurationBuilder.Build();
        DataManager = new DummyDataManager(Configuration);
    }

    public void Dispose()
    {
        // Do "global" teardown here; Only called once.
    }
}