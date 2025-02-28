using Microsoft.Extensions.Configuration;
using Shouldly;
using Toolbox.Settings;
using Xunit;

namespace Tests;

public class StorageSettingsTest(BasicFixture basicFixture) : IClassFixture<BasicFixture>
{
    [Fact]
    [Trait("UnitTest", "Computation")]
    public void ShouldGetCorrectSubcategorieNames()
    {
        var storageSettings = basicFixture.Configuration.GetSection(Constants.STORAGE_SETTINGS).Get<StorageSettings>();
        storageSettings.ShouldNotBeNull();
        storageSettings!.SubcategorieNames.Count.ShouldBe(35);
    }
}