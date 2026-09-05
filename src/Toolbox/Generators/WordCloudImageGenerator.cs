using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
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

        var settings = configuration.GetSection(Constants.WORD_CLOUD_SETTINGS).Get<WordCloudSettings>() ?? new();

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = settings.Headless
        });
        var page = await browser.NewPageAsync(new() { AcceptDownloads = true });
        page.SetDefaultTimeout(30_000);

        await page.GotoAsync(settings.GeneratorUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator(TextAreaSelector).FillAsync(text);
        await SelectRadioAsync(page, "Rectangle");
        await SelectRadioAsync(page, "Horizontal");
        await page.Locator("#word-cloud-font").ClickAsync();
        await page.GetByText("Roboto", new() { Exact = true }).ClickAsync();
        await SelectRadioAsync(page, "Dégradé de bleu");

        var download = await StartDownloadAsync(page, settings);

        cancellationToken.ThrowIfCancellationRequested();
        await SaveSourceImageAsync(download, monthDirectory);
    }

    private string GetMonthDirectory(string month)
    {
        return Path.Combine(configuration[Constants.CONTENT_ROOT_DIR]!, "..", "other-perspectives", "les-mois", month);
    }

    private static async Task SelectRadioAsync(IPage page, string name)
    {
        var radio = page.GetByRole(AriaRole.Radio, new() { Name = name, Exact = true });
        if (await radio.IsCheckedAsync()) return;

        // The live preview sits over the options during redraw and can cancel pointer events. A native DOM click
        // activates the real radio and still dispatches the input/change events consumed by the site's UI.
        await radio.EvaluateAsync("element => element.click()");

        if (!await radio.IsCheckedAsync())
            throw new InvalidOperationException($"The word cloud option '{name}' could not be selected.");
    }

    private static async Task<IDownload> StartDownloadAsync(IPage page, WordCloudSettings settings)
    {
        // The download is awaited before the context closes because Playwright removes its temporary files then.
        var downloadTask = page.WaitForDownloadAsync();
        await page.GetByText(DownloadButtonName, new() { Exact = true }).ClickAsync();

        var emailInput = page.Locator("[role=dialog] input[type=email]");
        var emailPromptTask = emailInput.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        var completedTask = await Task.WhenAny(downloadTask, emailPromptTask);

        if (completedTask == emailPromptTask)
        {
            Console.WriteLine($"E-mail requested, using {settings.DownloadEmail} for download.");
 
            await emailInput.FillAsync(settings.DownloadEmail);
            await emailInput.Locator("xpath=ancestor::*[@role='dialog']")
                .Locator("button[type=submit], input[type=submit]").ClickAsync();
        }

        return await downloadTask;
    }

    private static async Task SaveSourceImageAsync(IDownload download, string monthDirectory)
    {
        var extension = Path.GetExtension(download.SuggestedFilename);
        if (string.IsNullOrEmpty(extension))
            throw new InvalidOperationException("The word cloud website did not provide a file extension for its download.");

        // Do not replace featured.png until the ImageSharp crop/conversion stage is implemented.
        var targetPath = Path.Combine(monthDirectory, $"wordcloud-source{extension.ToLowerInvariant()}");
        var temporaryPath = Path.Combine(monthDirectory, $".{Guid.NewGuid():N}{extension}");

        try
        {
            await download.SaveAsAsync(temporaryPath);
            File.Move(temporaryPath, targetPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }
}
