using Microsoft.Extensions.Configuration;
using Toolbox.Domain;
using Toolbox.Settings;

namespace Toolbox.Generators;

public class ContentFileGenerator(IConfiguration configuration)
{
    public string GenerateSeasonIndexFile(Root data, int seasonId)
    {
        var season = data.Seasons.First(x => x.Id == seasonId);
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(), configuration[Constants.CONTENT_ROOT_DIR]!);
        var contentDir = Path.Combine(rootDir, season.ContentDirectoryName);
        var indexFile = Path.Combine(contentDir, "_index.md");
        Directory.CreateDirectory(contentDir);
        File.WriteAllText(indexFile, season.IndexFileContent());

        rootDir = Path.Combine(Directory.GetCurrentDirectory(), configuration[Constants.CHART_DATA_FILES_ROOT_DIR]!);
        contentDir = Path.Combine(rootDir, $"season-{seasonId}");
        Directory.CreateDirectory(contentDir);
        
        return indexFile;
    }
    
    public IEnumerable<string> GenerateAllSeasonsIndexFile(Root data)
    {
        foreach (var season in data.Seasons)
        {
            yield return GenerateSeasonIndexFile(data, season.Id);
        }
    }
}