using Tests.Customizations;
using Toolbox.Domain;
using Toolbox.Importers;
using Xunit;

namespace Tests.Importers;

public class SeasonMetadataImporterTest(BasicFixture fixture): IClassFixture<BasicFixture>
{
    [Theory]
    [Trait("UnitTest", "ContentImport")]
    [AutoDomainData]
    public void ShouldImportSeasonMetadata(Root data)
    {
        var seasonMetadataImporter = new SeasonMetadataImporter(fixture.Configuration);
        seasonMetadataImporter.ImportSeasonMetadata(16, data);
    }
}