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
    /// /// <exception cref="MetadataConsistencyException">
    /// Thrown when the check fails.
    /// </exception>
    public static void CheckPoemsWithoutMetricSpecified(Root data)
    {
        var incorrectPoem = data.Seasons.SelectMany(x => x.Poems).FirstOrDefault(x => !x.HasVerseLength);

        if (incorrectPoem is not null)
            throw new MetadataConsistencyException($"[ERROR] First poem with unspecified metric or equal to '0': {incorrectPoem.Id}");
    }

    /// <summary>
    /// Checks that all poems whose metric is variable have metric specified as expected in Info.
    /// </summary>
    /// /// <exception cref="MetadataConsistencyException">
    /// Thrown when the check fails.
    /// </exception>
    public static void CheckPoemsWithVariableMetricNotPresentInInfo(Root data)
    {
        var poems = data.Seasons.SelectMany(x => x.Poems.Where(x => x.HasVariableMetric));
        var incorrectPoem = poems.FirstOrDefault(x => !x.Info.StartsWith("Métrique variable : "));

        if (incorrectPoem is not null)
            throw new MetadataConsistencyException($"[ERROR] First poem with variable metric unspecified in Info: {incorrectPoem.Id}");
    }

    /// <summary>
    /// Verifies that all poems in the specified season have the correct position (weight)
    /// within their corresponding season in the file system based on the ordering in the data structure.
    /// </summary>
    /// <param name="data">The root object containing seasons and their poems.</param>
    /// <param name="seasonId">
    /// The ID of the season to be verified. If null, the verification is performed for the last two seasons in the data.
    /// </param>
    /// <exception cref="MetadataConsistencyException">
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
                throw new MetadataConsistencyException($"Poem {poem.Id} should have weight {poemIndex + 1}!");
            }
        }
    }

    /// <summary>
    /// Verifies that all expected metadata consistency checks pass for a partial poem import.
    /// </summary>
    /// <param name="partialImport">An object containing partial import data, including metadata tags, poem year, detailed metric, and additional information.</param>
    /// <param name="metrics">A list of all available metrics.</param>
    /// <param name="descriptionSettings">Settings for required descriptions.</param>
    public static void VerifyAnomalies(PoemImporter.PartialImport partialImport, List<Metric> metrics, RequiredDescriptionSettings descriptionSettings)
    {
        // TODO gather the thrown exceptions and return them as a list
        VerifyMetricIsSpecified(partialImport);
        VerifyYearTagIsPresent(partialImport);
        VerifyVariableMetricTagIsPresent(partialImport);
        VerifyVariableMetricInfoIsPresent(partialImport);
        VerifyMetricTagsArePresent(partialImport, metrics);
        VerifyRequiredDescription(partialImport, descriptionSettings);
    }

    /// <summary>
    /// Verifies that a poem's metric is specified.
    /// </summary>
    /// <param name="partialImport"></param>
    /// <exception cref="MetadataConsistencyException"></exception>
    public static void VerifyMetricIsSpecified(PoemImporter.PartialImport partialImport)
    {
        if (!string.IsNullOrEmpty(partialImport.DetailedMetric) && partialImport.DetailedMetric != "0")
            return;

        throw new MetadataConsistencyException("Poem metric is unspecified");
    }

    /// <summary>
    /// Verifies that a poem's year tag is present and matches the poem's year.
    /// </summary>
    /// <param name="partialImport"></param>
    /// <exception cref="MetadataConsistencyException"></exception>
    public static void VerifyYearTagIsPresent(PoemImporter.PartialImport partialImport)
    {
        if (partialImport.Tags.Contains(partialImport.Year.ToString()))
            return;

        throw new MetadataConsistencyException($"Missing year tag: {partialImport.Year}");
    }

    /// <summary>
    /// Verifies that a poem has the 'métrique variable' tag if its metric is variable.
    /// </summary>
    /// <param name="partialImport"></param>
    /// <exception cref="MetadataConsistencyException"></exception>
    public static void VerifyVariableMetricTagIsPresent(PoemImporter.PartialImport partialImport)
    {
        if (!partialImport.HasVariableMetric || partialImport.Tags.Contains("métrique variable"))
            return;

        throw new MetadataConsistencyException("Missing 'métrique variable' tag");
    }

    /// <summary>
    /// Verifies that a poem's Info specifies the variable metric value if its metric is variable.
    /// </summary>
    /// <param name="partialImport"></param>
    /// <exception cref="MetadataConsistencyException"></exception>
    public static void VerifyVariableMetricInfoIsPresent(PoemImporter.PartialImport partialImport)
    {
        if (!partialImport.HasVariableMetric)
            return;

        if (partialImport.Info?.Contains("Métrique variable : ") == true)
            return;

        throw new MetadataConsistencyException("Missing 'Métrique variable : ' in Info");
    }

    /// <summary>
    /// Verifies that all expected metric tags are present for a poem, depending on its detailed metric.
    /// </summary>
    /// <param name="partialImport"></param>
    /// <param name="metrics"></param>
    /// <exception cref="MetadataConsistencyException"></exception>
    public static void VerifyMetricTagsArePresent(PoemImporter.PartialImport partialImport, List<Metric> metrics)
    {
        foreach (var metric in partialImport.DetailedMetric.Split(','))
        {
            if (metric == "poème en prose")
                break;

            var expectedTag = metrics.FirstOrDefault(x => x.Length.ToString() == metric.Trim())?.Name
                .ToLowerInvariant();
            if (!partialImport.Tags.Contains(expectedTag))
                throw new MetadataConsistencyException($"Missing '{expectedTag}' tag");
        }
    }

    /// <summary>
    /// Verifies that a description is present and meets the requirements based on extra tags and settings.
    /// </summary>
    /// <param name="partialImport"></param>
    /// <param name="settings"></param>
    /// <exception cref="MetadataConsistencyException">
    /// Thrown if the description is missing or required bold formatting is missing when expected.
    /// </exception>
    public static void VerifyRequiredDescription(PoemImporter.PartialImport partialImport, RequiredDescriptionSettings settings)
    {
        foreach (var extraTag in partialImport.Tags)
        {
            var setting = settings.RequiredDescriptions.FirstOrDefault(x => x.ExtraTag == extraTag);
            if (setting is null)
                continue;

            if (string.IsNullOrWhiteSpace(partialImport.Description))
                throw new MetadataConsistencyException($"Poem {partialImport.PoemId} is missing description because of extra tag '{extraTag}'");

            if (setting.Bold && !partialImport.Description.Contains("**"))
                throw new MetadataConsistencyException($"Poem {partialImport.PoemId} description is missing bold formatting because of extra tag '{extraTag}'");
        }
    }
}
