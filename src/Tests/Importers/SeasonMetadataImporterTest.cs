using Tests.Customizations;
using Toolbox.Domain;
using Toolbox.Importers;
using Xunit;

namespace Tests.Importers;

public class SeasonMetadataImporterTest(BasicFixture basicFixture): IClassFixture<BasicFixture>
{
    [Theory]
    [Trait("UnitTest", "ContentImport")]
    [AutoDomainData]
    public void ShouldImportSeasonMetadata(Root data)
    {
        var seasonMetadataImporter = new SeasonMetadataImporter(basicFixture.Configuration);
        seasonMetadataImporter.ImportSeasonMetadata(16, data);
    }
}