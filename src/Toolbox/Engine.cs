using System.Globalization;
using Microsoft.Extensions.Configuration;
using Toolbox.Charts;
using Toolbox.Domain;
using Toolbox.Importers;
using Toolbox.Persistence;
using Toolbox.Settings;

namespace Toolbox;

[Obsolete("Will be replaced by direct use of modules")]
public class Engine
{
    private readonly IConfiguration _configuration;
    private readonly IDataManager _dataManager;
    public Root Data { get; private set; } = default!;
    public Root DataEn { get; private set; } = default!;

    private PoemImporter? _poemContentImporter;
    

    public Engine(IConfiguration configuration, IDataManager dataManager)
    {
        _configuration = configuration;
        _dataManager = dataManager;
        Data = new() { Seasons = [] };
    }

    [Obsolete]
    public void Load()
    {
        _dataManager.Load(out var data, out var dataEn);
        Data = data;
        DataEn = dataEn;
    }
    




    public void OutputSeasonsDuration()
    {
        foreach (var season in Data.Seasons.Where(x => x.Poems.Count > 0))
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

    private void AddDataLine(int x, int y, int value,
        List<BubbleChartDataLine>[] quarterBubbleChartDatalines, int maxValue,
        int bubbleMaxRadiusPixels)
    {
        // Bubble radius and color
        decimal bubbleSize = (decimal)bubbleMaxRadiusPixels * value / maxValue;
        var bubbleColor = string.Empty;
        if (bubbleSize < (bubbleMaxRadiusPixels / 4))
        {
            // First quarter
            bubbleSize *= 4;
            bubbleColor = "rgba(121, 248, 248, 1)";
            quarterBubbleChartDatalines[0].Add(new(x, y,
                bubbleSize.ToString(new NumberFormatInfo { NumberDecimalSeparator = "." }), bubbleColor));
        }
        else if (bubbleSize < (bubbleMaxRadiusPixels / 2))
        {
            // Second quarter
            bubbleSize *= 2;
            bubbleColor = "rgba(119, 181, 254, 1)";
            quarterBubbleChartDatalines[1].Add(new(x, y,
                bubbleSize.ToString(new NumberFormatInfo { NumberDecimalSeparator = "." }), bubbleColor));
        }
        else if (bubbleSize < (bubbleMaxRadiusPixels * 3 / 4))
        {
            // Third quarter
            bubbleSize *= 1.5m;
            bubbleColor = "rgba(0, 127, 255, 1)";
            quarterBubbleChartDatalines[2].Add(new(x, y,
                bubbleSize.ToString(new NumberFormatInfo { NumberDecimalSeparator = "." }), bubbleColor));
        }
        else
        {
            // Fourth quarter
            bubbleColor = "rgba(50, 122, 183, 1)";
            quarterBubbleChartDatalines[3].Add(new(x, y,
                bubbleSize.ToString(new NumberFormatInfo { NumberDecimalSeparator = "." }), bubbleColor));
        }
    }
  }