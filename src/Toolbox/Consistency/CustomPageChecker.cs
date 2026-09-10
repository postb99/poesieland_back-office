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
    private static readonly Regex SeasonPathRegex = new(@"\.\./\.\./seasons/(?<season>[^/]+)/(?<file>[^/]+)",
        RegexOptions.CultureInvariant);
    
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

        var allMatches = SeasonPathRegex.Matches(pageContent); // calculé UNE fois, hors boucle

        foreach (var poem in poems)
        {
            var poemFileName = poem.Id.Substring(0, poem.Id.LastIndexOf('_'));

            var isListed = allMatches.Any(m =>
                m.Groups["season"].Value.StartsWith(poem.SeasonId.ToString())
                && m.Groups["file"].Value == poemFileName);

            if (!isListed)
            {
                errors.Add($"Poem {poem.Id} should be listed on 'saisons' tag index page!");
            }
        }
        
        if (errors.Any())
            throw new CustomPageConsistencyException(errors);
    }
}