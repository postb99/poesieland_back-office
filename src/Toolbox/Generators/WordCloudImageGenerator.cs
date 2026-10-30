using KnowledgePicker.WordCloud;
using KnowledgePicker.WordCloud.Coloring;
using KnowledgePicker.WordCloud.Drawing;
using KnowledgePicker.WordCloud.Layouts;
using KnowledgePicker.WordCloud.Primitives;
using KnowledgePicker.WordCloud.Sizers;
using Microsoft.Extensions.Configuration;
using SkiaSharp;
using Toolbox.Settings;

namespace Toolbox.Generators;

public class WordCloudImageGenerator(IConfiguration configuration)
{
    private static readonly string[] Months =
    [
        "janvier", "février", "mars", "avril", "mai", "juin", "juillet", "août", "septembre", "octobre",
        "novembre", "décembre"
    ];

    public async Task GenerateAllAsync(CancellationToken cancellationToken = default)
    {
        foreach (var month in Months)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await GenerateAsync(month, cancellationToken);
        }
    }

    public async Task GenerateAsync(string month, CancellationToken cancellationToken = default)
    {
        if (!Months.Contains(month))
            throw new ArgumentOutOfRangeException(nameof(month), month, "Month must be one of the French month names.");

        var monthDirectory = GetMonthDirectory(month);
        var textPath = Path.Combine(monthDirectory, "wordcloud.txt");
        if (!File.Exists(textPath))
            throw new FileNotFoundException("The word cloud text file does not exist.", textPath);

        var text = await File.ReadAllTextAsync(textPath, cancellationToken);
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException($"The word cloud text file is empty: {textPath}");

        // 0. Collect data
        var wordEntries = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Select(word => word.ToLowerInvariant())
            .GroupBy(word => word, StringComparer.Ordinal)
            .Select(group => new WordCloudEntry(group.Key, group.Count()))
            .OrderByDescending(entry => entry.Count)
            .ToList();
        
        // 1. Configuration
        var wordCloud = new WordCloudInput(wordEntries)
        {
            Width = 600,
            Height = 400,
            MinFontSize = 24,
            MaxFontSize = 72,
            ItemMargin = 0
        };
        var sizer = new LogSizer(wordCloud);
        using var engine = new SkGraphicEngine(sizer, wordCloud);
        var layout = new SpiralLayout(wordCloud);
        var colorizer = new FrequencyBlueColorizer(wordEntries.Min(entry => entry.Count),
            wordEntries.Max(entry => entry.Count));
        var wcg = new WordCloudGenerator<SKBitmap>(wordCloud, engine, layout, colorizer);

        // 2. Préparation du Canvas SKBitmap
        using var final = new SKBitmap(wordCloud.Width, wordCloud.Height);
        using var canvas = new SKCanvas(final);

        // 3. Dessin du nuage de mots
        using var bitmap = wcg.Draw();
        canvas.DrawBitmap(bitmap, 0, 0, SKSamplingOptions.Default);

        // 4. Exportation au format WebP
        using var data = final.Encode(SKEncodedImageFormat.Webp, 100);
        await using var writer = File.Create(Path.Combine(monthDirectory, "featured.webp"));
        data.SaveTo(writer);
    }

    private string GetMonthDirectory(string month)
    {
        return Path.Combine(configuration[Constants.CONTENT_ROOT_DIR]!, "..", "other-perspectives", "les-mois", month);
    }

    private sealed class FrequencyBlueColorizer(int minCount, int maxCount) : IColorizer
    {
        private static readonly System.Drawing.Color LightBlue = System.Drawing.Color.FromArgb(164, 218, 255);
        private static readonly System.Drawing.Color DarkBlue = System.Drawing.Color.FromArgb(0, 80, 168);

        public System.Drawing.Color? GetColor(KnowledgePicker.WordCloud.Primitives.LayoutItem item)
        {
            var range = maxCount - minCount;
            var intensity = range == 0 ? 1d : (item.Entry.Count - minCount) / (double)range;
            intensity = Math.Pow(intensity, 0.5);
            return System.Drawing.Color.FromArgb(
                Interpolate(LightBlue.R, DarkBlue.R, intensity),
                Interpolate(LightBlue.G, DarkBlue.G, intensity),
                Interpolate(LightBlue.B, DarkBlue.B, intensity));
        }

        private static int Interpolate(int start, int end, double amount) =>
            (int)Math.Round(start + (end - start) * amount);
    }
}
