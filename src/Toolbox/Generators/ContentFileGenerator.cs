using Microsoft.Extensions.Configuration;
using Toolbox.Domain;
using Toolbox.Settings;

namespace Toolbox.Generators;

public class ContentFileGenerator(IConfiguration configuration)
{
    /// <summary>
    /// Generates _index.md file for a season.
    /// </summary>
    /// <param name="data">The root object containing season data.</param>
    /// <param name="seasonId">An existing season id.</param>
    /// <returns>The generated _index.md file path.</returns>
    public string GenerateSeasonIndexFile(Root data, int seasonId)
    {
        var season = data.Seasons.First(x => x.Id == seasonId);
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(), configuration[Constants.CONTENT_ROOT_DIR]!);
        var contentDir = Path.Combine(rootDir, season.ContentDirectoryName);
        var indexFile = Path.Combine(contentDir, "_index.md");
        Directory.CreateDirectory(contentDir);
        File.WriteAllText(indexFile, season.IndexFileContent());

        rootDir = Path.Combine(Directory.GetCurrentDirectory(), configuration[Constants.CHART_DATA_FILES_ROOT_DIR]!);
        contentDir = Path.Combine(rootDir, $"season-{seasonId}");
        Directory.CreateDirectory(contentDir);

        return indexFile;
    }

    /// <summary>
    /// Generates _index.md files for all seasons.
    /// </summary>
    /// <param name="data">The root object containing season data.</param>
    /// <returns>A collection of file paths for the generated _index.md files.</returns>
    public IEnumerable<string> GenerateAllSeasonsIndexFile(Root data)
    {
        foreach (var season in data.Seasons)
        {
            yield return GenerateSeasonIndexFile(data, season.Id);
        }
    }

    /// <summary>
    /// Generates a content file for a specified poem.
    /// </summary>
    /// <param name="data">The root object containing season and poem data.</param>
    /// <param name="poem">The poem object for which the content file is generated.</param>
    /// <returns>The file path of the generated poem content file.</returns>
    public string GeneratePoemFile(Root data, Poem poem)
    {
        var metricSettings = configuration.GetSection(Constants.METRIC_SETTINGS).Get<MetricSettings>();
        var season = data.Seasons.First(x => x.Id == poem.SeasonId);
        var poemIndex = season.Poems.IndexOf(poem);
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(), configuration[Constants.CONTENT_ROOT_DIR]!);
        var contentDir = Path.Combine(rootDir, season.ContentDirectoryName);
        Directory.CreateDirectory(contentDir);
        var indexFile = Path.Combine(contentDir, poem.ContentFileName);
        File.WriteAllText(indexFile, poem.FileContent(poemIndex, metricSettings!));

        return indexFile;
    }

    /// <summary>
    /// Generates content files for all poems within a specified season.
    /// </summary>
    /// <param name="data">The root object containing season and poem data.</param>
    /// <param name="seasonId">The ID of the season whose poems' content files will be generated.</param>
    /// /// <returns>The file path of the generated poem content files.</returns>
    public IEnumerable<string> GenerateSeasonAllPoemFiles(Root data, int seasonId)
    {
        var season = data.Seasons.First(x => x.Id == seasonId);
        foreach (var poem in season.Poems)
            yield return GeneratePoemFile(data, poem);
    }

    /// <summary>
    /// Generates content files for all poems within all seasons.
    /// </summary>
    /// <param name="data">The root object containing season and poem data.</param>
    public void GenerateAllPoemFiles(Root data)
    {
        var poems = data.Seasons.SelectMany(x => x.Poems).ToList();
        foreach (var poem in poems)
        {
            GeneratePoemFile(data, poem);
        }
    }

    /// <summary>
    /// Generates files containing the total count of French poems and variable metric poems.
    /// Output files:
    /// - "poem_count.md"
    /// - "variable_metric_poem_count.md".
    /// </summary>
    /// <param name="data">The root object containing French seasons and their respective poems.</param>
    public void GeneratePoemCountFile(Root data)
    {
        var poemCount = data.Seasons.Select(x => x.Poems.Count).Sum();
        var poemCountFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            configuration[Constants.CONTENT_ROOT_DIR]!, "../../common", "poem_count.md");
        File.WriteAllText(poemCountFilePath, poemCount.ToString());

        // And for variable verse
        var variableMetricPoemCount = data.Seasons.SelectMany(x => x.Poems.Where(x => x.HasVariableMetric)).Count();
        var variableMetricPoemCountFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            configuration[Constants.CONTENT_ROOT_DIR]!, "../../common", "variable_metric_poem_count.md");
        File.WriteAllText(variableMetricPoemCountFilePath, variableMetricPoemCount.ToString());
    }

    /// <summary>
    /// Generates a file containing the English poems total count.
    /// Output file: "poem_count_en.md".
    /// </summary>
    /// <param name="dataEn">The root object containing English poems arranged by season.</param>
    public void GeneratePoemEnCountFile(Root dataEn)
    {
        var poemCount = dataEn.Seasons.Select(x => x.Poems.Count).Sum();
        var poemCountFilePath = Path.Combine(Directory.GetCurrentDirectory(),
            configuration[Constants.CONTENT_ROOT_DIR]!, "../../common", "poem_count_en.md");
        File.WriteAllText(poemCountFilePath, poemCount.ToString());
    }
}