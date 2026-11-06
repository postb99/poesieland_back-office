using System.Globalization;
using System.Text;
using Toolbox.Domain;

namespace Toolbox.Charts;

public static class ChartDataFileDataHelper
{
    /// <summary>Formats axis labels and titles for a categories bubble chart.</summary>
    public static string FormatCategoriesBubbleChartLabelOptions(List<string>? xAxisLabelsForCallback,
        List<string>? yAxisLabelsForCallback = null, string? xAxisTitle = null, string? yAxisTitle = null)
    {
        var xAxisTitleOption = xAxisTitle is null ? " " : $", title: {{display:true, text:'{xAxisTitle.JavaScriptString()}'}} ";
        var yAxisTitleOption = yAxisTitle is null ? " " : $", title: {{display:true, text:'{yAxisTitle.JavaScriptString()}'}} ";

        // https://www.chartjs.org/docs/latest/axes/labelling.html
        var sb = new StringBuilder("scales: { x: { ")
            .Append("ticks: { stepSize: 1, ")
            .Append("autoSkip: false");
        if (xAxisLabelsForCallback is not null)
        {
            sb.Append(", callback: function(value, index, ticks) { return [")
                .Append(string.Join(',', xAxisLabelsForCallback.Select(x => $"'{x.JavaScriptString()}'")))
                .Append("][index]; }");
        }
        sb.Append(" }")
          .Append(xAxisTitleOption)
          .Append('}');

        sb.Append(", y: { ")
            .Append("ticks: { stepSize: 1, ")
            .Append("autoSkip: false");
        if (yAxisLabelsForCallback is not null)
        {
            sb.Append(", callback: function(value, index, ticks) { return [")
                .Append(string.Join(',', yAxisLabelsForCallback.Select(x => $"'{x.JavaScriptString()}'")))
                .Append("][index]; }");
        }
        sb.Append(" }")
            .Append(yAxisTitleOption)
            .Append('}');

        sb.Append(" }");
        return sb.ToString();
    }
    /// <summary>Creates a zero-initialized dictionary for each month and day.</summary>
    public static Dictionary<string, int> InitMonthDayDictionary()
    {
        var dataDict = new Dictionary<string, int>();
        for (var i = 1; i < 32; i++) dataDict.Add(i < 10 ? $"01-0{i}" : $"01-{i}", 0);
        for (var i = 1; i < 30; i++) dataDict.Add(i < 10 ? $"02-0{i}" : $"02-{i}", 0);
        for (var i = 1; i < 32; i++) dataDict.Add(i < 10 ? $"03-0{i}" : $"03-{i}", 0);
        for (var i = 1; i < 31; i++) dataDict.Add(i < 10 ? $"04-0{i}" : $"04-{i}", 0);
        for (var i = 1; i < 32; i++) dataDict.Add(i < 10 ? $"05-0{i}" : $"05-{i}", 0);
        for (var i = 1; i < 31; i++) dataDict.Add(i < 10 ? $"06-0{i}" : $"06-{i}", 0);
        for (var i = 1; i < 32; i++) dataDict.Add(i < 10 ? $"07-0{i}" : $"07-{i}", 0);
        for (var i = 1; i < 32; i++) dataDict.Add(i < 10 ? $"08-0{i}" : $"08-{i}", 0);
        for (var i = 1; i < 31; i++) dataDict.Add(i < 10 ? $"09-0{i}" : $"09-{i}", 0);
        for (var i = 1; i < 32; i++) dataDict.Add(i < 10 ? $"10-0{i}" : $"10-{i}", 0);
        for (var i = 1; i < 31; i++) dataDict.Add(i < 10 ? $"11-0{i}" : $"11-{i}", 0);
        for (var i = 1; i < 32; i++) dataDict.Add(i < 10 ? $"12-0{i}" : $"12-{i}", 0);
        return dataDict;
    }
    /// <summary>Returns the French month or season first day label for a radar chart date.</summary>
    public static string GetRadarChartLabel(string monthDay)
    {
        var day = monthDay.Substring(3);
        var month = monthDay.Substring(0, 2);
        return month switch
        {
            "01" => day == "01" ? "Janvier" : string.Empty,
            "02" => day == "01" ? "Février" : string.Empty,
            "03" => day == "01" ? "Mars" : day == "20" ? "Printemps" : string.Empty,
            "04" => day == "01" ? "Avril" : string.Empty,
            "05" => day == "01" ? "Mai" : string.Empty,
            "06" => day == "01" ? "Juin" : day == "21" ? "Eté" : string.Empty,
            "07" => day == "01" ? "Juillet" : string.Empty,
            "08" => day == "01" ? "Août" : string.Empty,
            "09" => day == "01" ? "Septembre" : day == "23" ? "Automne" : string.Empty,
            "10" => day == "01" ? "Octobre" : string.Empty,
            "11" => day == "01" ? "Novembre" : string.Empty,
            "12" => day == "01" ? "Décembre" : day == "21" ? "Hiver" : string.Empty,
            _ => string.Empty
        };
    }

    /// <summary>Returns the English month or season first day label for a radar chart date.</summary>
    public static string GetRadarEnChartLabel(string monthDay)
    {
        var day = monthDay.Substring(3);
        var month = monthDay.Substring(0, 2);
        return month switch
        {
            "01" => day == "01" ? "January" : string.Empty,
            "02" => day == "01" ? "February" : string.Empty,
            "03" => day == "01" ? "March" : day == "20" ? "Spring" : string.Empty,
            "04" => day == "01" ? "April" : string.Empty,
            "05" => day == "01" ? "May" : string.Empty,
            "06" => day == "01" ? "June" : day == "21" ? "Summer" : string.Empty,
            "07" => day == "01" ? "July" : string.Empty,
            "08" => day == "01" ? "August" : string.Empty,
            "09" => day == "01" ? "September" : day == "23" ? "Fall" : string.Empty,
            "10" => day == "01" ? "October" : string.Empty,
            "11" => day == "01" ? "November" : string.Empty,
            "12" => day == "01" ? "December" : day == "21" ? "Winter" : string.Empty,
            _ => string.Empty
        };
    }

    /// <summary>Returns the French name of a month number.</summary>
    public static string GetMonthLabel(string month) => month switch
    {
        "01" => "janvier", "02" => "février", "03" => "mars", "04" => "avril",
        "05" => "mai", "06" => "juin", "07" => "juillet", "08" => "août",
        "09" => "septembre", "10" => "octobre", "11" => "novembre", "12" => "décembre",
        _ => "?"
    };

    /// <summary>Adds a bubble data line to the size-based quarter and assigns its color.</summary>
    public static void AddDataLine(int x, int y, int value, List<BubbleChartDataLine>[] quarterBubbleChartDataLines,
        int maxValue, int bubbleMaxRadiusPixels)
    {
        decimal bubbleSize = (decimal)bubbleMaxRadiusPixels * value / maxValue;
        var quarter = bubbleSize < bubbleMaxRadiusPixels / 4m ? 0
            : bubbleSize < bubbleMaxRadiusPixels / 2m ? 1
            : bubbleSize < bubbleMaxRadiusPixels * 3m / 4m ? 2 : 3;
        bubbleSize *= quarter switch { 0 => 4, 1 => 2, 2 => 1.5m, _ => 1 };
        var color = quarter switch
        {
            0 => "rgba(121, 248, 248, 1)", 1 => "rgba(119, 181, 254, 1)",
            2 => "rgba(0, 127, 255, 1)", _ => "rgba(50, 122, 183, 1)"
        };
        quarterBubbleChartDataLines[quarter].Add(new(x, y, bubbleSize.ToString(CultureInfo.InvariantCulture), color));
    }

    /// <summary>Computes normalized poem metric values for each non-empty season.</summary>
    public static Dictionary<int, List<decimal>> FillMetricDataDict(Root data, out List<string> xLabels)
    {
        var metricRange = Enumerable.Range(1, 14);
        var dataDict = metricRange.ToDictionary(metric => metric, _ => new List<decimal>());
        xLabels = [];
        foreach (var season in data.Seasons.Where(x => x.Poems.Count > 0))
        {
            var multiple = 50m / season.Poems.Count;
            xLabels.Add(season.TitleForChartsWithYears);
            foreach (var metric in metricRange)
                dataDict[metric].Add(Decimal.Round(season.Poems.Count(x => x.HasMetric(metric)) * multiple, 1));
        }
        return dataDict;
    }

    /// <summary>Counts co-occurring poem subcategories and collects their axis labels.</summary>
    public static void FillCategoriesBubbleChartDataDict(Dictionary<KeyValuePair<string, string>, int> dictionary,
        SortedSet<string> xLabels, SortedSet<string> yLabels, Poem poem)
    {
        var subCategories = poem.Categories.SelectMany(x => x.SubCategories).ToList();
        if (subCategories.Count == 1) return;
        for (var i = 0; i < subCategories.Count; i++)
        for (var j = subCategories.Count - 1; j > -1; j--)
        {
            if (string.Compare(subCategories[i], subCategories[j]) >= 0) continue;
            var key = new KeyValuePair<string, string>(subCategories[i], subCategories[j]);
            if (dictionary.TryGetValue(key, out _)) dictionary[key]++;
            else { dictionary.Add(key, 1); xLabels.Add(key.Key); yLabels.Add(key.Value); }
        }
    }

    /// <summary>Counts poems by subcategory and metric, using zero for variable metrics.</summary>
    public static void FillCategoryMetricBubbleChartDataDict(Dictionary<KeyValuePair<string, int>, int> dictionary,
        SortedSet<string> xLabels, Poem poem)
    {
        var subCategories = poem.Categories.SelectMany(x => x.SubCategories).ToList();
        var metric = poem.HasVariableMetric ? 0 : int.Parse(poem.VerseLength!);
        foreach (var key in subCategories.Select(subCategory => new KeyValuePair<string, int>(subCategory, metric)))
        {
            if (dictionary.TryGetValue(key, out _)) dictionary[key]++;
            else { dictionary.Add(key, 1); xLabels.Add(key.Key); }
        }
    }}



