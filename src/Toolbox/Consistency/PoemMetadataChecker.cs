using Microsoft.Extensions.Configuration;
using Toolbox.Domain;
using Toolbox.Importers;
using Toolbox.Settings;

namespace Toolbox.Consistency;

public class PoemMetadataChecker(IConfiguration configuration, IPoemImporter poemImporter)
{
    /// <summary>
    /// Checks that all poems have a metric specified.
    /// </summary>
    /// /// <exception cref="Exception">
    /// Thrown when the check fails.
    /// </exception>
    public static void CheckPoemsWithoutMetricSpecified(Root data)
    {
        var incorrectPoem = data.Seasons.SelectMany(x => x.Poems).FirstOrDefault(x => !x.HasVerseLength);

        if (incorrectPoem is not null)
            throw new(
                $"[ERROR] First poem with unspecified metric or equal to '0': {incorrectPoem.Id}");
    }

    /// <summary>
    /// Checks that all poems whose metric is variable have metric specified as expected in Info.
    /// </summary>
    /// /// <exception cref="Exception">
    /// Thrown when the check fails.
    /// </exception>
    public static void CheckPoemsWithVariableMetricNotPresentInInfo(Root data)
    {
        var poems = data.Seasons.SelectMany(x => x.Poems.Where(x => x.HasVariableMetric));
        var incorrectPoem = poems.FirstOrDefault(x => !x.Info.StartsWith("Métrique variable : "));

        if (incorrectPoem is not null)
            throw new(
                $"[ERROR] First poem with variable metric unspecified in Info: {incorrectPoem.Id}");
    }

    /// <summary>
    /// Verifies that all poems in the specified season have the correct position (weight)
    /// within their corresponding season in the file system based on the ordering in the data structure.
    /// </summary>
    /// <param name="data">The root object containing seasons and their poems.</param>
    /// <param name="seasonId">
    /// The ID of the season to be verified. If null, the verification is performed for the last two seasons in the data.
    /// </param>
    /// <exception cref="Exception">
    /// Thrown if a poem's position (weight) in the file system does not match its expected position in the data structure.
    /// </exception>
    public void VerifySeasonHaveCorrectWeightInPoemFile(Root data, int? seasonId)
    {
        if (seasonId is null)
        {
            VerifySeasonHaveCorrectWeightInPoemFile(data, data.Seasons.Last().Id);
            VerifySeasonHaveCorrectWeightInPoemFile(data, data.Seasons.Last().Id - 1);
            return;
        }

        var season = data.Seasons.First(s => s.Id == seasonId);
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(), configuration[Constants.CONTENT_ROOT_DIR]!);
        var seasonDirName = Directory.EnumerateDirectories(rootDir)
            .FirstOrDefault(x => Path.GetFileName(x).StartsWith($"{seasonId}_"));
        var poemFiles = Directory.EnumerateFiles(seasonDirName!).Where(x => !x.EndsWith("index.md"));

        foreach (var poemFile in poemFiles)
        {
            var (poem, position) = poemImporter.Import(poemFile);
            var poemInSeason = season.Poems.FirstOrDefault(x => x.Id == poem.Id);
            var poemIndex = poemInSeason == null ? -1 : season.Poems.IndexOf(poemInSeason);
            if (poemIndex != -1 && poemIndex != position)
            {
                throw new($"Poem {poem.Id} should have weight {poemIndex + 1}!");
            }
        }
    }

    /// <summary>
    /// Checks for anomalies in a partial poem import.
    /// Possible anomalies:
    /// - Poem metric is unspecified
    /// - Poem year is not found in tags
    /// - Poem metric is not found in tags
    /// - Poem 'métrique variable' tag is missing
    /// - Poem variable metric value is missing in Info
    /// </summary>
    /// <param name="partialImport">An object containing partial import data, including metadata tags, poem year, detailed metric, and additional information.</param>
    /// <param name="metrics">A list of all available metrics.</param>
    /// <returns>A collection of strings describing anomalies found in the partial import.</returns>
    public static IEnumerable<string> CheckAnomalies(PoemImporter.PartialImport partialImport, List<Metric> metrics)
    {
        if (string.IsNullOrEmpty(partialImport.DetailedMetric) || partialImport.DetailedMetric == "0")
        {
            yield return "Poem metric is unspecified";
        }

        // Poem year should be found in tags
        if (!partialImport.Tags.Contains(partialImport.Year.ToString()))
        {
            yield return "Missing year tag";
        }

        // When metric is variable, "métrique variable" tag should be found and info should mention it
        if (partialImport.HasVariableMetric)
        {
            if (!partialImport.Tags.Contains("métrique variable"))
            {
                yield return "Missing 'métrique variable' tag";
            }

            if (!partialImport.Info.Contains("Métrique variable : "))
            {
                yield return "Missing 'Métrique variable : ' in Info";
            }
        }

        // Name of metric should be found in tags
        foreach (var metric in partialImport.DetailedMetric.Split(','))
        {
            if (metric == "poème en prose")
                break;
            var expectedTag = metrics.FirstOrDefault(x => x.Length.ToString() == metric.Trim())?.Name
                .ToLowerInvariant();
            if (!partialImport.Tags.Contains(expectedTag))
            {
                yield return $"Missing '{expectedTag}' tag";
            }
        }
    }
}