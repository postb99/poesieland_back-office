using Microsoft.Extensions.Configuration;
using Toolbox.Settings;

namespace Toolbox.Generators;

public class WordCloudImageGenerator(IConfiguration configuration)
{
    private static readonly string[] Months =
    [
        "janvier", "février", "mars", "avril", "mai", "juin", "juillet", "août", "septembre", "octobre",
        "novembre", "décembre"
    ];

    private const string DownloadButtonName = "Télécharger le nuage";
    private const string TextAreaSelector = "#word-cloud-keywords-text";

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

        // TODO
    }

    private string GetMonthDirectory(string month)
    {
        return Path.Combine(configuration[Constants.CONTENT_ROOT_DIR]!, "..", "other-perspectives", "les-mois", month);
    }
}
