using System.Text;
using AutoFixture.Xunit3;
using Tests.Customizations;
using Toolbox.Domain;
using Toolbox.Generators;
using Toolbox.Settings;
using Xunit;

namespace Tests.Generators;

public class ContentFileGeneratorTest(BasicFixture fixture, ITestOutputHelper testOutputHelper)
    : IClassFixture<BasicFixture>
{
    [Theory]
    [Trait("UnitTest", "ContentGeneration")]
    [AutoDomainData]
    public void ShouldGenerateSeasonIndexFile(Root data)
    {
        var generatedFilePath =
            new ContentFileGenerator(fixture.Configuration).GenerateSeasonIndexFile(data, data.Seasons.First().Id);
        testOutputHelper.WriteLine(File.ReadAllText(generatedFilePath));
        DeleteGeneratedFiles(generatedFilePath, data.Seasons.First().Id);
    }

    [Theory]
    [Trait("UnitTest", "ContentGeneration")]
    [AutoDomainData]
    public void ShouldGeneratePoemIndexFile(Root data)
    {
        data.Seasons.First().Id = data.Seasons.First().Poems.First().SeasonId;
        var generatedFilePath =
            new ContentFileGenerator(fixture.Configuration).GeneratePoemFile(data, data.Seasons.First().Poems.First());
        testOutputHelper.WriteLine(File.ReadAllText(generatedFilePath));
        DeleteGeneratedFiles(generatedFilePath, data.Seasons.First().Id);
    }
    
    [Theory]
    [Trait("UnitTest", "ContentGeneration")]
    [AutoDomainData]
    public void ShouldGenerateSeasonAllPoemFiles(Root data)
    {
        data.Seasons.First().Id = data.Seasons.First().Poems.First().SeasonId;
        var generatedFilesPaths =
            new ContentFileGenerator(fixture.Configuration).GenerateSeasonAllPoemFiles(data, data.Seasons.First().Id);
        foreach (var generatedFilePath in generatedFilesPaths)
        {
            testOutputHelper.WriteLine(File.ReadAllText(generatedFilePath));
            DeleteGeneratedFiles(generatedFilePath, data.Seasons.First().Id);
        }
    }

    private void DeleteGeneratedFiles(string seasonIndexFile, int seasonId)
    {
        File.Delete(seasonIndexFile);
        Directory.Delete(Path.GetDirectoryName(seasonIndexFile));
        var chartDir = Path.Combine(Directory.GetCurrentDirectory(), fixture.Configuration[Constants.CHART_DATA_FILES_ROOT_DIR]!, $"season-{seasonId}");
        Directory.Delete(chartDir);
    }
}