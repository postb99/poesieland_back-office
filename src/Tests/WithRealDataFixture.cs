using Toolbox;
using Toolbox.Domain;
using Toolbox.Persistence;

namespace Tests;

public class WithRealDataFixture : BasicFixture
{
    public Root Data { get; }
    public Root DataEn { get; }

    public WithRealDataFixture()
    {
        // Do "global" initialization here; Only called once.
        new DataManager(Configuration).Load(out Root data, out Root dataEn);
        Data = data;
        DataEn = dataEn;
    }

    public void Dispose()
    {
        // Do "global" teardown here; Only called once.
    }
}