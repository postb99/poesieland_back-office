using Toolbox;
using Toolbox.Modules.Persistence;

namespace Tests;

public class LoadDataFixture : BasicFixture
{
    public Engine Engine { get; private set; }

    public LoadDataFixture()
    {
        // Do "global" initialization here; Only called once.
        Engine = new(Configuration, new DataManager(Configuration));
        Engine.Load();
    }

    public void Dispose()
    {
        // Do "global" teardown here; Only called once.
    }
}