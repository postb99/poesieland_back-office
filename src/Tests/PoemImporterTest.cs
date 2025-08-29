using Shouldly;
using Toolbox;
using Toolbox.Settings;
using Xunit;

namespace Tests;

public class PoemImporterTest(BasicFixture basicFixture): IClassFixture<BasicFixture>
{
    [Fact]
    [Trait("UnitTest", "ContentImport")]
    public void ShouldImportVariableVerseLength()
    {
        var poemContentFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            basicFixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "3_troisieme_saison/jeux_de_nuits.md");
        var poemContentImporter = new PoemImporter(basicFixture.Configuration);
        var (poem, _) = poemContentImporter.Import(poemContentFilePath);
        poem.Info.ShouldBe("Métrique variable : 8, 6, 4, 2");
        poem.DetailedMetric.ShouldBe("8, 6, 4, 2");
        // Because it has been copied from DetailedVerseLength by poemContentImporter.
        poem.VerseLength.ShouldBe("8, 6, 4, 2");
        var anomalies = poemContentImporter.CheckAnomaliesAfterImport();
        anomalies.ShouldBeEmpty();
    }

    [Fact]
    [Trait("UnitTest", "Computation")]
    public void ShouldFindExtraTags()
    {
        var poemContentImporter = new PoemImporter(basicFixture.Configuration);
        poemContentImporter.FindExtraTags(["lovecat", "2025", "nature", "sonnet", "métrique variable", "other", "octosyllabe"]).ShouldBe(["lovecat", "other"]);
    }
}