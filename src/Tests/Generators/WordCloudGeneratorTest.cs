using Toolbox.Generators;
using Xunit;

namespace Tests.Generators;

public class WordCloudGeneratorTest(WithRealDataFixture fixture): IClassFixture<WithRealDataFixture>
{
  [Fact]
  [Trait("UnitTest", "FileGeneration")]
  public void ShouldGenerateWordCloudFiles()
  {
    new WordCloudGenerator(fixture.Configuration).GenerateTextFiles(fixture.Data);
  }  
}