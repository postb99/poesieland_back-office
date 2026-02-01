using Microsoft.Extensions.Configuration;
using Toolbox.Domain;
using Toolbox.Settings;

namespace Toolbox.Importers;

public class SeasonMetadataImporter(IConfiguration configuration)
{
    /// <summary>
    /// Imports metadata for a given season into the specified root data structure.
    /// </summary>
    /// <param name="seasonId">The unique identifier for the season being imported.</param>
    /// <param name="data">The root data structure where the season metadata will be imported.</param>
    /// <exception cref="ArgumentException">Thrown when the season index file is not found.</exception>
    public void ImportSeasonMetadata(int seasonId, Root data)
    {
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(), configuration[Constants.CONTENT_ROOT_DIR]!);
        var seasonDirName = Directory.EnumerateDirectories(rootDir)
            .FirstOrDefault(x => Path.GetFileName(x).StartsWith($"{seasonId}_"));

        var seasonIndexPath = Path.Combine(rootDir, seasonDirName, "_index.md");
        if (!File.Exists(seasonIndexPath))
        {
            throw new ArgumentException($"Season index file not found: {seasonIndexPath}");
        }

        var importedSeason = new SeasonIndexImporter().Import(seasonIndexPath);
        var targetSeason = data.Seasons.FirstOrDefault(x => x.Id == seasonId);

        if (targetSeason is null)
        {
            targetSeason = new()
            {
                Id = seasonId
            };
            data.Seasons.Add(targetSeason);
        }

        targetSeason.Update(importedSeason);
    }
}