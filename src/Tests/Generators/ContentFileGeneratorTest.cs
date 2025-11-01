using System.Text;
using AutoFixture.Xunit3;
using Tests.Customizations;
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
        var generatedFilesPaths = new ContentFileGenerator(basicFixture.Configuration).GenerateAllSeasonsIndexFile(data);
        foreach (var generatedFilePath in generatedFilesPaths)
        {
            testOutputHelper.WriteLine(File.ReadAllText(generatedFilePath));
            File.Delete(generatedFilePath);
            Directory.Delete(Path.GetDirectoryName(generatedFilePath));
        }
    }
    
    [Theory]
    [Trait("UnitTest", "ContentGeneration")]
    [AutoDomainData]
    public void ShouldGeneratePoemIndexFile(Root data)
    {
        data.Seasons.First().Id = data.Seasons.First().Poems.First().SeasonId;
        var generatedFilePath =
            new ContentFileGenerator(basicFixture.Configuration).GeneratePoemFile(data, data.Seasons.First().Poems.First());
        testOutputHelper.WriteLine(File.ReadAllText(generatedFilePath));
        File.Delete(generatedFilePath);
        Directory.Delete(Path.GetDirectoryName(generatedFilePath));
    }
    
    [Theory]
    [Trait("UnitTest", "ContentGeneration")]
    [AutoDomainData]
    public void ShouldGenerateSeasonAllPoemFiles(Root data)
    {
        data.Seasons.First().Id = data.Seasons.First().Poems.First().SeasonId;
        var generatedFilesPaths =
            new ContentFileGenerator(basicFixture.Configuration).GenerateSeasonAllPoemFiles(data, data.Seasons.First().Id);
        foreach (var generatedFilePath in generatedFilesPaths)
        {
            testOutputHelper.WriteLine(File.ReadAllText(generatedFilePath));
            File.Delete(generatedFilePath);
            Directory.Delete(Path.GetDirectoryName(generatedFilePath));
        }
    }
}