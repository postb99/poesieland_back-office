using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Toolbox.Settings;

namespace Tests;

public class StorageSettingsTest(BasicFixture basicFixture) : IClassFixture<BasicFixture>
{
    [Fact]
    [Trait("UnitTest", "Computation")]
    public void ShouldGetCorrectSubcategorieNames()
    {
        var storageSettings = basicFixture.Configuration.GetSection(Constants.STORAGE_SETTINGS).Get<StorageSettings>();
        storageSettings.Should().NotBeNull();
        storageSettings!.SubcategorieNames.Count.Should().Be(35);
    }
}