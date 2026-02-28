using Toolbox.Settings;
using Xunit;

namespace Tests.Generators;

public class WordCloudGeneratorTest(WithRealDataFixture fixture): IClassFixture<WithRealDataFixture>
{
  [Theory]
  [Trait("DataMining", "Helper")]
  [InlineData("janvier")]
  [InlineData("février")]
  [InlineData("mars")]
  [InlineData("avril")]
  [InlineData("mai")]
  [InlineData("juin")]
  [InlineData("juillet")]
  [InlineData("août")]
  [InlineData("septembre")]
  [InlineData("octobre")]
  [InlineData("novembre")]
  [InlineData("décembre")]
  public void ShouldGenerateWordCloudFiles(string monthName)
  {
    var rootDir = Path.Combine(fixture.Configuration[Constants.CONTENT_ROOT_DIR]!, "..", "other-perspectives", "les-mois", monthName);
    var filePath = Path.Combine(rootDir, "wordcloud.txt");
    var poems = fixture.Data.Seasons.SelectMany(x => x.Poems).Where(p => p.ExtraTags.Contains(monthName));
    File.WriteAllText(filePath, string.Empty);
    foreach (var poem in poems)
    {
      File.AppendAllText(filePath, poem.Description.ToLowerInvariant());
      File.AppendAllText(filePath, Environment.NewLine);
    }
  }  
}