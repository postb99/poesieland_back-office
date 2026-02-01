using Toolbox.Domain;

namespace Toolbox.Consistency;

public class ReusedTitlesChecker(Root data)
{
    /// <summary>
    /// Retrieves a collection of reused titles across different poems.
    /// A title is considered reused if it appears more than once among all poems.
    /// Each returned entry includes detailed information about the title,
    /// the count of reuse, and the IDs of the poems where it is reused.
    /// Titles specified in the allowed reuse configuration file are excluded from the output.
    /// </summary>
    /// <returns>
    /// An enumerable collection of strings containing information about reused titles and their occurrences.
    /// </returns>
    public IEnumerable<string> GetReusedTitles()
    {
        var allowedReuses = File.ReadAllLines("./allowed_reuses.txt");
        foreach (var group in data.Seasons.SelectMany(x => x.Poems).GroupBy(x => x.Title))
        {
            var count = group.Count();
            if (count <= 1) continue;
            var outputLine =
                $"Reused title {group.Key} {count} times ({string.Join(", ", group.Select(g => g.Id))})";
            if (!allowedReuses.Contains(outputLine))
                yield return outputLine;
        }
    }
}