using System.Globalization;
using Microsoft.Extensions.Configuration;
using Toolbox.Charts;
using Toolbox.Domain;
using Toolbox.Importers;
using Toolbox.Persistence;
using Toolbox.Settings;

namespace Toolbox;

[Obsolete("Will be replaced by direct use of modules")]
public class Engine
{
    private readonly IConfiguration _configuration;
    private readonly IDataManager _dataManager;
    public Root Data { get; private set; } = default!;
    public Root DataEn { get; private set; } = default!;

    private PoemImporter? _poemContentImporter;


    public Engine(IConfiguration configuration, IDataManager dataManager)
    {
        _configuration = configuration;
        _dataManager = dataManager;
        Data = new() { Seasons = [] };
    }

    [Obsolete]
    public void Load()
    {
        _dataManager.Load(out var data, out var dataEn);
        Data = data;
        DataEn = dataEn;
    }
}