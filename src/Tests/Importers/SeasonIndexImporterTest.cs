using Shouldly;
using Toolbox.Importers;
using Toolbox.Settings;
using Xunit;

namespace Tests.Importers;

public class SeasonIndexImporterTest(BasicFixture fixture): IClassFixture<BasicFixture>
{
    [Fact]
    [Trait("UnitTest", "ContentImport")]
    public void ShouldImport()
    {
        var seasonIndexContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            fixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "7_septieme_saison/_index.md");
        var seasonIndexImporter = new SeasonIndexImporter();
        var season = seasonIndexImporter.Import(seasonIndexContentFilePath);
        season.Id.ShouldBe(7);
        season.Name.ShouldBe("Croire");
        season.NumberedName.ShouldBe("Septième");
        season.Description.ShouldBe($"Mois obsédés où la création le dispute à l'être et l'art musical, tandis qu'un amour éperdu se partage entre romantisme et passion. La saison qui a le plus alimenté mon recueil.{Environment.NewLine}{Environment.NewLine}Juin et juillet 1998");
    }
}