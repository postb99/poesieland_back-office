using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Toolbox.Domain;
using Toolbox.Settings;

namespace Toolbox.Consistency;

public class CustomPageChecker(IConfiguration configuration)
{
    /// <summary>
    /// Verifies that poems in the "Ciel" category that start with specific words are listed on the "Ciel" category index page.
    /// </summary>
    /// <param name="importedPoem">The specific poem to check. If null, all poems in the "Ciel" category are evaluated.</param>
    /// <param name="data">The root data containing seasons and their associated poems.</param>
    /// <exception cref="CustomPageConsistencyException">
    /// Thrown when the check fails.
    /// </exception>
    public void VerifyPoemOfSkyCategoryStartingWithSpecificWordsIsListedOnCustomPage(Poem? importedPoem,
        Root data)
    {
        var errors = new List<string>();
        if (importedPoem is not null && !importedPoem.Categories.SelectMany(c => c.SubCategories).Contains("Ciel"))
            return;

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
                continue;
            if (poem.Paragraphs.First().Verses.First().StartsWith("Les cieux sont gris"))
                continue;

            errors.Add($"Poem {poem.Id} should be listed on 'Ciel' category index page!");

            if (errors.Any())
                throw new CustomPageConsistencyException(errors);
        }
    }

    /// <summary>
    /// Verifies that poems associated with more than one season are listed on the "saisons" tag index page.
    /// </summary>
    /// <param name="importedPoem">The specific poem to validate. If null, all poems with more than one season are validated.</param>
    /// <param name="data">The root data containing seasons and their associated poems.</param>
    /// <exception cref="CustomPageConsistencyException">
    /// Thrown when the check fails.
    /// </exception>
    public void VerifyPoemOfMoreThanOneSeasonIsListedOnCustomPage(Poem? importedPoem, Root data)
    {
        var errors = new List<string>();
        if (importedPoem is not null &&
            !importedPoem.Categories.Any(x => x is { Name: "Saisons", SubCategories.Count: > 1 }))
            return;

        var rootDir = Path.Combine(Directory.GetCurrentDirectory(), configuration[Constants.CONTENT_ROOT_DIR]!);
        var pageFile = Path.Combine(rootDir, "..", "tags", "saisons", "_index.md");
        var pageContent = File.ReadAllText(pageFile);

        var poems = importedPoem is not null
            ? [importedPoem]
            : data.Seasons.SelectMany(x =>
                x.Poems.Where(x => x.Categories.Any(x => x is { Name: "Saisons", SubCategories.Count: > 1 }))).ToList();

        foreach (var poem in poems)
        {
            var seasonId = poem.SeasonId;
            var poemFileName = poem.Id.Substring(0, poem.Id.LastIndexOf('_'));
            var regexp = new Regex($"(../../seasons/{seasonId}\\w*/{poemFileName})");
            var match = regexp.Match(pageContent);
            if (!match.Success)
            {
                errors.Add($"Poem {poem.Id} should be listed on 'saisons' tag index page!");
            }
        }
        
        if (errors.Any())
            throw new CustomPageConsistencyException(errors);
    }
}