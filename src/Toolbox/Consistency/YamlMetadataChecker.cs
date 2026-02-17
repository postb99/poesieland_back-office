using Microsoft.Extensions.Configuration;
using Toolbox.Domain;
using Toolbox.Importers;
using Toolbox.Settings;

namespace Toolbox.Consistency;

public class YamlMetadataChecker(IConfiguration configuration, Root data)
{
    /// <summary>
    /// Gathers any anomaly met in YAML metadata of poem content files
    /// contained within the directory structure defined by the provided data.
    /// The method iterates through seasons, starting from season 21 and validates content files for
    /// anomalies in their YAML metadata.
    /// </summary>
    /// <returns>
    /// An enumerable collection of strings indicating the anomalies found in the YAML metadata.
    /// Each string specifies the anomaly and the associated file path.
    /// </returns>
    public async IAsyncEnumerable<string> GetYamlMetadataAnomaliesAcrossSeasonsAsync()
    {
        var metrics = configuration.GetSection(Constants.METRIC_SETTINGS).Get<MetricSettings>()!.Metrics;
        var requiredDescriptions = configuration.GetSection(Constants.REQUIRED_DESCRIPTION_SETTINGS)
            .Get<RequiredDescriptionSettings>()!.RequiredDescriptions;
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

                var anomalies = await PoemMetadataChecker.GetAnomaliesAsync(partialImport, metrics, requiredDescriptions);
                foreach (var p in anomalies)
                    yield return
                        $"{p} in {poemContentPath.Substring(poemContentPath.IndexOf("seasons"))}";
            }
        }
    }
}