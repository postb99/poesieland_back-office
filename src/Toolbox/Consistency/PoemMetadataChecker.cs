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
    public static void CheckPoemsWithoutVerseLength(Root data)
    {
        var poems = data.Seasons.SelectMany(x => x.Poems);
        var poemsWithVerseLength = poems.Count(x => x.HasVerseLength);
        if (poemsWithVerseLength == poems.Count())
            return;

        var incorrectPoem = poems.FirstOrDefault(x => !x.HasVerseLength);
        if (incorrectPoem is not null)
            throw new(
                $"[ERROR] First poem with unspecified metric or equal to '0': {incorrectPoem.Id}");
    }
    
    /// <summary>
    /// Checks that all poems whose metric is variable have a metric specified in Info.
    /// </summary>
    /// /// <exception cref="Exception">
    /// Thrown when the check fails.
    /// </exception>
    public static void CheckPoemsWithVariableMetric(Root data)
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
}