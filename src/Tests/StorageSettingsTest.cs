using Microsoft.Extensions.Configuration;
using Shouldly;
using Toolbox.Settings;
using Xunit;

namespace Tests;

public class StorageSettingsTest : IClassFixture<BasicFixture>
{
    private readonly BasicFixture _fixture;
    private static readonly StorageSettings StorageSettings = new();

    public StorageSettingsTest(BasicFixture fixture)
    {
        _fixture = fixture;
        fixture.Configuration.GetSection(Constants.STORAGE_SETTINGS).Bind(StorageSettings);
    }

    [Fact]
    [Trait("UnitTest", "Computation")]
    public void ShouldGetCorrectSubcategorieNames()
    {
        StorageSettings.SubcategorieNames.Count.ShouldBe(35);
    }
}