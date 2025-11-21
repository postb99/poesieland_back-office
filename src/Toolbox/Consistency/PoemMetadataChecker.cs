using Toolbox.Domain;

namespace Toolbox.Consistency;

public static class PoemMetadataChecker
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
}