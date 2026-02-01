using Toolbox.Domain;

namespace Toolbox.Information;

public static class SeasonDurationOutputHelper
{
    public static void OutputSeasonsDuration(Root data)
    {
        foreach (var season in data.Seasons.Where(x => x.Poems.Count > 0))
        {
            var dates = season.Poems.Select(x => x.Date).OrderBy(x => x.Date).ToList();
            var duration = dates[dates.Count() - 1] - dates[0];
            decimal nbDays = int.Parse(duration.ToString("%d"));
            var value = nbDays;
            var unit = "days";
            if (value > 30)
            {
                value = value / 30;
                unit = "months";

                if (value > 12)
                {
                    value = value / 12;
                    unit = "years";
                }
            }

            Console.WriteLine($"{season.NumberedName} ({season.Period}): {value} {unit}");
        }
    }
}