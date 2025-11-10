using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Toolbox.Domain;
using Toolbox.Settings;

namespace Toolbox.Consistency;

public class CustomPageChecker(IConfiguration configuration)
{
    /// <summary>
    /// Checks if poems with the "les mois" extra tag are properly listed on the "les mois" tag index page.
    /// </summary>
    /// <param name="importedPoem">The specific poem to check. If null, all poems with the "les mois" extra tag are checked.</param>
    /// <param name="data">The root data containing seasons and their associated poems.</param>
    /// <returns>A collection of error messages for poems with the "les mois" extra tag that are not listed on the "les mois" tag index page.</returns>
    public IEnumerable<string> GetPoemWithLesMoisExtraTagNotListedOnCustomPage(Poem? importedPoem, Root data)
    {
        if (importedPoem is not null && !importedPoem.ExtraTags.Contains("les mois"))
            yield return string.Empty;

        var rootDir = Path.Combine(Directory.GetCurrentDirectory(), configuration[Constants.CONTENT_ROOT_DIR]!);
        var pageFile = Path.Combine(rootDir, "..", "tags", "les-mois", "_index.md");
        var pageContent = File.ReadAllText(pageFile);

        var poems = importedPoem is not null
            ? [importedPoem]
            : data.Seasons.SelectMany(x => x.Poems.Where(x => x.ExtraTags.Contains("les mois"))).ToList();

        foreach (var poem in poems)
        {
            var seasonId = poem.SeasonId;
            var poemFileName = poem.Id.Substring(0, poem.Id.LastIndexOf('_'));
            var regexp = new Regex($"(../../seasons/{seasonId}\\w*/{poemFileName})");
            var match = regexp.Match(pageContent);
            if (!match.Success)
            {
                yield return $"[ERROR]: Poem {poem.Id} should be listed on 'les mois' tag index page!";
            }
        }
    }

    /// <summary>
    /// Checks if poems in the "Ciel" category that start with specific words are listed on the "Ciel" category index page.
    /// </summary>
    /// <param name="importedPoem">The specific poem to check. If null, all poems in the "Ciel" category are evaluated.</param>
    /// <param name="data">The root data containing seasons and their associated poems.</param>
    /// <returns>A collection of error messages for poems in the "Ciel" category that are not listed on the "Ciel" category index page.</returns>
    public IEnumerable<string> GetPoemOfSkyCategoryStartingWithSpecificWordsNotListedOnCustomPage(Poem? importedPoem,
        Root data)
    {
        if (importedPoem is not null && !importedPoem.Categories.SelectMany(c => c.SubCategories).Contains("ciel"))
            yield return string.Empty;

        var rootDir = Path.Combine(Directory.GetCurrentDirectory(), configuration[Constants.CONTENT_ROOT_DIR]!);
        var pageFile = Path.Combine(rootDir, "..", "categories", "ciel", "_index.md");
        var pageContent = File.ReadAllText(pageFile);

        var poems = importedPoem is not null
            ? [importedPoem]
            : data.Seasons.SelectMany(x => x.Poems.Where(x =>
                x.Categories.SelectMany(c => c.SubCategories).Contains("Ciel") &&
                (x.Paragraphs.First().Verses.First().StartsWith("Le ciel est ") ||
                 x.Paragraphs.First().Verses.First().StartsWith("Les cieux sont ")))).ToList();

        foreach (var poem in poems)
        {
            var seasonId = poem.SeasonId;
            var poemFileName = poem.Id.Substring(0, poem.Id.LastIndexOf('_'));
            var regexp = new Regex($"(../../seasons/{seasonId}\\w*/{poemFileName})");
            var match = regexp.Match(pageContent);
            if (match.Success) continue;
            
            // When starting with "Le ciel est gris" or "Les cieux sont gris" they are listed on special repeats custom listing
            if (poem.Paragraphs.First().Verses.First().StartsWith("Le ciel est gris"))
                yield return string.Empty;
            else if (poem.Paragraphs.First().Verses.First().StartsWith("Les cieux sont gris"))
                yield return string.Empty;
            else
                yield return $"[ERROR]: Poem {poem.Id} should be listed on 'Ciel' category index page!";
        }
    }

    /// <summary>
    /// Validates that poems associated with more than one season are listed on the "saisons" tag index page.
    /// </summary>
    /// <param name="importedPoem">The specific poem to validate. If null, all poems with more than one season are validated.</param>
    /// <param name="data">The root data containing seasons and their associated poems.</param>
    /// <returns>A collection of error messages for poems with more than one season that are not listed on the "saisons" tag index page.</returns>
    public IEnumerable<string> GetPoemOfMoreThanOneSeasonNotListedOnCustomPage(Poem? importedPoem, Root data)
    {
        if (importedPoem is not null && !importedPoem.Categories.Any(x => x is { Name: "Saisons", SubCategories.Count: > 1 }))
            yield return string.Empty;

        var rootDir = Path.Combine(Directory.GetCurrentDirectory(), configuration[Constants.CONTENT_ROOT_DIR]!);
        var pageFile = Path.Combine(rootDir, "..", "tags", "saisons", "_index.md");
        var pageContent = File.ReadAllText(pageFile);

        var poems = importedPoem is not null
            ? [importedPoem]
            : data.Seasons.SelectMany(x => x.Poems.Where(x => x.Categories.Any(x => x is { Name: "Saisons", SubCategories.Count: > 1 }))).ToList();

        foreach (var poem in poems)
        {
            var seasonId = poem.SeasonId;
            var poemFileName = poem.Id.Substring(0, poem.Id.LastIndexOf('_'));
            var regexp = new Regex($"(../../seasons/{seasonId}\\w*/{poemFileName})");
            var match = regexp.Match(pageContent);
            if (!match.Success)
            {
                yield return $"[ERROR]: Poem {poem.Id} should be listed on 'saisons' tag index page!";
            }
        }
    }
}