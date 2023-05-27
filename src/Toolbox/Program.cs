using Microsoft.Extensions.Configuration;
using Toolbox;

public class Program
{
    private static IConfiguration _configuration;
    public static void Main(string[] args)
    {
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        _configuration = configurationBuilder.Build();

        var engine = new Engine(_configuration);
        engine.Load();
    }

    
}



  
