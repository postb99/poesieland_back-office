using Toolbox.Domain;

namespace Toolbox.Consistency;

public static class SeasonChecker
{
    /// <summary>
    /// Verifies that the seasons in the given data object meet specific poem count requirements.
    /// Each season except the last must contain exactly 50 poems.
    /// The last season must not exceed 50 poems. Throws an exception if these conditions are not met.
    /// </summary>
    /// <param name="data">The root data object containing the list of seasons to verify.</param>
    /// <exception cref="Exception">
    /// Thrown when a season (excluding the last season) contains a number of poems other than 50
    /// or when the last season contains more than 50 poems.
    /// </exception>
    public static void VerifySeasonHaveCorrectPoemCount(Root data)
    {
        var seasons = data.Seasons.Where(x => x.Poems.Count > 0).ToList();
        var seasonCount = seasons.Count;
        for (int i = 0; i < seasonCount; i++)
        {
            var season = seasons[i];
            var desc = $"[{season.Id} - {season.Name}]: {season.Poems.Count}";
            if (i < seasonCount - 1 && season.Poems.Count != 50)
            {
                throw new($"Not last season. Not 50 poems for {desc}!");
            }

            if (i == seasonCount - 1 && season.Poems.Count > 50)
            {
                throw new($"Last season. More than 50 poems for {desc}!");
            }
        }
    }
}