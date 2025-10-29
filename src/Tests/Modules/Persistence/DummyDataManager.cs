using Microsoft.Extensions.Configuration;
using Toolbox.Domain;
using Toolbox.Modules.Persistence;

namespace Tests.Modules.Persistence;

public class DummyDataManager : IDataManager
{
    public DummyDataManager(IConfiguration configuration)
    {
    }

    public void Load(out Root data, out Root dataEn)
    {
        data = new();
        dataEn = new();
    }

    public void Save(Root data)
    {
    }

    public void SaveEn(Root dataEn)
    {
    }
}