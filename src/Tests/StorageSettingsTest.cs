using Microsoft.Extensions.Configuration;
using Shouldly;
using Toolbox.Settings;
using Xunit;

namespace Tests;

public class StorageSettingsTest(BasicFixture fixture) : IClassFixture<BasicFixture>
{
    [Fact]
    [Trait("UnitTest", "Computation")]
    public void ShouldGetCorrectSubcategorieNames()
    {
        var storageSettings = fixture.Configuration.GetSection(Constants.STORAGE_SETTINGS).Get<StorageSettings>();
        storageSettings.ShouldNotBeNull();
        storageSettings!.SubcategorieNames.Count.ShouldBe(35);
    }
}