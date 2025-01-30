using Toolbox;

namespace Tests;

public class LoadDataFixture : IDisposable
{
    public Engine Engine { get; private set; }

    public LoadDataFixture()
    {
        // Do "global" initialization here; Only called once.
        Engine = new Engine(Helpers.GetConfiguration());
        Engine.Load();
    }

    public void Dispose()
    {
        // Do "global" teardown here; Only called once.
    }
}