using Microsoft.Extensions.Configuration;
using Toolbox.Domain;
using Toolbox.Importers;
using Toolbox.Settings;

namespace Toolbox.Consistency;

public class YamlMetadataChecker(IConfiguration configuration, Root data)
{
    /// <summary>
    /// Verifies that no missing tags exist in the YAML metadata of poem content files
    /// contained within the directory structure defined by the provided data.
    /// The method iterates through seasons and validates content files for
    /// anomalies in their YAML metadata.
    /// </summary>
    public void VerifyMissingTagsInYamlMetadata()
    {
        // TODO put back output of coufght exceptions
        var metrics = configuration.GetSection(Constants.METRIC_SETTINGS).Get<MetricSettings>().Metrics;
        var requiredDescriptionSettings = configuration.GetSection(Constants.REQUIRED_DESCRIPTION_SETTINGS).Get<RequiredDescriptionSettings>();
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(), configuration[Constants.CONTENT_ROOT_DIR]!);
        var poemContentImporter = new PoemImporter(configuration);

        var seasonMaxId = data.Seasons.Count;
        for (var i = 21; i < seasonMaxId + 1; i++)
        {
            var season = data.Seasons.First(x => x.Id == i);
            var contentDir = Path.Combine(rootDir, season.ContentDirectoryName);
            var poemContentPaths = Directory.EnumerateFiles(contentDir).Where(x => !x.EndsWith("_index.md"));
            foreach (var poemContentPath in poemContentPaths)
            {
                var partialImport = poemContentImporter.GetPartialImport(poemContentPath);
                if (!poemContentImporter.HasYamlMetadata) continue;
                try
                {
                    PoemMetadataChecker.VerifyAnomalies(partialImport, metrics, requiredDescriptionSettings);
                }
                catch (MetadataConsistencyException ex)
                {
                    throw new MetadataConsistencyException(
                        $"{ex.Message} in {poemContentPath.Substring(poemContentPath.IndexOf("seasons"))}");
                }
            }
        }
    }
}
