using System.Text;
using AutoFixture.Xunit3;
using Toolbox.Domain;
using Toolbox.Generators;
using Xunit;

namespace Tests.Generators;

public class ContentFileGeneratorTest(BasicFixture basicFixture, ITestOutputHelper testOutputHelper)
    : IClassFixture<BasicFixture>
{
    [Theory]
    [Trait("UnitTest", "ContentGeneration")]
    [AutoData]
    public void ShouldGenerateSeasonIndexFile(Root data)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var generatedFilePath =
            new ContentFileGenerator(basicFixture.Configuration).GenerateSeasonIndexFile(data, data.Seasons.First().Id);
        testOutputHelper.WriteLine(File.ReadAllText(generatedFilePath));
        File.Delete(generatedFilePath);
        Directory.Delete(Path.GetDirectoryName(generatedFilePath));
    }

    [Theory]
    [Trait("UnitTest", "ContentGeneration")]
    [AutoData]
    public void ShouldGenerateAllSeasonsIndexFile(Root data)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var generatedFilePaths = new ContentFileGenerator(basicFixture.Configuration).GenerateAllSeasonsIndexFile(data);
        foreach (var generatedFilePath in generatedFilePaths)
        {
            testOutputHelper.WriteLine(File.ReadAllText(generatedFilePath));
            File.Delete(generatedFilePath);
            Directory.Delete(Path.GetDirectoryName(generatedFilePath));
        }
    }
}