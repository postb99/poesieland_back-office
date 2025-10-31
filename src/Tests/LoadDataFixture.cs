using Toolbox;
using Toolbox.Domain;
using Toolbox.Persistence;

namespace Tests;

public class LoadDataFixture : BasicFixture
{
    [Obsolete("To be removed")]
    public Engine Engine { get; private set; }
    
    public Root Data { get; private set; }
    public Root DataEn { get; private set; }

    public LoadDataFixture()
    {
        // Do "global" initialization here; Only called once.
        Engine = new(Configuration, new DataManager(Configuration));
        Engine.Load();
        
        new DataManager(Configuration).Load(out Root data, out Root dataEn);
        Data = data;
        DataEn = dataEn;
    }

    public void Dispose()
    {
        // Do "global" teardown here; Only called once.
    }
}