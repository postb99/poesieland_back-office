using Shouldly;
using Toolbox.Modules.Importers;
using Toolbox.Settings;
using Xunit;

namespace Tests.Modules.Importers;

public class SeasonIndexImporterTest(BasicFixture basicFixture): IClassFixture<BasicFixture>
{
    [Fact]
    [Trait("UnitTest", "ContentImport")]
    public void ShouldImport()
    {
        var seasonIndexContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            basicFixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "7_septieme_saison/_index.md");
        var seasonIndexImporter = new SeasonIndexImporter();
        var season = seasonIndexImporter.Import(seasonIndexContentFilePath);
        season.Id.ShouldBe(7);
        season.Name.ShouldBe("Croire");
        season.NumberedName.ShouldBe("Septième");
        season.Summary.ShouldBe("Mois obsédés, amour éperdu. Juin et juillet 1998");
        season.Introduction.ShouldBe($"Mois obsédés où la création le dispute à l'être et l'art musical, tandis qu'un amour éperdu se partage entre romantisme et passion. La saison qui a le plus alimenté mon recueil.{Environment.NewLine}{Environment.NewLine}Juin et juillet 1998");
    }
}