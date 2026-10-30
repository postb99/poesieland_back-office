using KnowledgePicker.WordCloud;
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

        // 0.Collect data
        var wordEntries = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Select(word => word.ToLowerInvariant())
            .GroupBy(word => word, StringComparer.Ordinal)
            .Select(group => new WordCloudEntry(group.Key, group.Count()))
            .ToList();
        
        // 1. Configuration
        var wordCloud = new WordCloudInput(wordEntries) { Width = 600, Height = 400 };
        var sizer = new LogSizer(wordCloud);
        using var engine = new SkGraphicEngine(sizer, wordCloud);
        var layout = new SpiralLayout(wordCloud);
        var wcg = new WordCloudGenerator(wordCloud, engine, layout); // TODO fix compilation

        // 2. Préparation du Canvas SKBitmap
        using var final = new SKBitmap(wordCloud.Width, wordCloud.Height);
        using var canvas = new SKCanvas(final);
        canvas.Clear(SKColors.White);

        // 3. Dessin du nuage de mots
        using var bitmap = wcg.Draw(); // TODO fix compilation
        canvas.DrawBitmap(bitmap, 0, 0, SKSamplingOptions.Default); // TODO fix compilation

        // 4. Exportation au format PNG
        using var data = final.Encode(SKEncodedImageFormat.Png, 100);
        await using var writer = File.Create(Path.Combine(monthDirectory, "featured.png"));
        data.SaveTo(writer);
    }

    private string GetMonthDirectory(string month)
    {
        return Path.Combine(configuration[Constants.CONTENT_ROOT_DIR]!, "..", "other-perspectives", "les-mois", month);
    }
}
