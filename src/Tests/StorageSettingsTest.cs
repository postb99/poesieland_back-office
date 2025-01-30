using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Toolbox.Settings;

namespace Tests;

public class StorageSettingsTest
{
    [Fact]
    [Trait("UnitTest", "Computation")]
    public void ShouldGetCorrectSubcategorieNames()
    {
        var storageSettings = Helpers.GetConfiguration().GetSection(Constants.STORAGE_SETTINGS).Get<StorageSettings>();
        storageSettings.SubcategorieNames.Count.Should().Be(35);
    }
}