using Toolbox.Generators;
using Xunit;

namespace Tests.Generators;

public class WordCloudTextGeneratorTest(WithRealDataFixture fixture): IClassFixture<WithRealDataFixture>
{
  [Fact]
  [Trait("UnitTest", "FileGeneration")]
  public void ShouldGenerateWordCloudFiles()
  {
    new WordCloudTextGenerator(fixture.Configuration).GenerateWordCloudFiles(fixture.Data);
  }  
}