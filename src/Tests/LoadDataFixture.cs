using Toolbox;

namespace Tests;

public class LoadDataFixture : IDisposable
{
    public Engine Engine { get; private set; }

    public LoadDataFixture()
    {
        // Do "global" initialization here; Only called once.
        Engine = Helpers.CreateEngine();
    }

    public void Dispose()
    {
        // Do "global" teardown here; Only called once.
    }
}