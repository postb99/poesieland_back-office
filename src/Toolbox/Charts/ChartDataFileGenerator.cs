using System.Globalization;
using Microsoft.Extensions.Configuration;
using Toolbox.Domain;
using Toolbox.Settings;

namespace Toolbox.Charts;

/// <summary>
/// Provides chart-specific data file generation methods for the application's chart data files.
/// </summary>
public partial class ChartDataFileGenerator
{
    /// <summary>
    /// Stores the application configuration used to resolve chart and content output directories.
    /// </summary>
    private readonly IConfiguration _configuration;
    /// <summary>
    /// Stores the configured metric definitions used to assign chart labels and colors.
    /// </summary>
    private static readonly MetricSettings MetricSettings = new();
    /// <summary>
    /// Stores the configured categories and subcategories used to build chart labels and colors.
    /// </summary>
    private static readonly StorageSettings StorageSettings = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartDataFileGenerator"/> class and
    /// binds the metric and storage settings used by chart generation methods.
    /// </summary>
    /// <param name="configuration">The application configuration containing chart, metric, and storage settings.</param>
    public ChartDataFileGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
        _configuration.GetSection(Constants.METRIC_SETTINGS).Bind(MetricSettings);
        _configuration.GetSection(Constants.STORAGE_SETTINGS).Bind(StorageSettings);
    }

    /// <summary>
    /// Creates and initializes a writer for a chart data file below the configured chart data root directory.
    /// The returned writer is positioned after the chart helper's opening data section has been written.
    /// </summary>
    /// <param name="subDir">The subdirectory below the configured chart data root directory.</param>
    /// <param name="fileName">The name of the chart data file to create.</param>
    /// <param name="chartType">The type of chart for which the data file is being generated.</param>
    /// <param name="chartDataFileHelper">When this method returns, the initialized helper associated with the returned writer.</param>
    /// <param name="nbDatasets">The number of datasets expected by the chart helper.</param>
    /// <returns>A <see cref="StreamWriter"/> that writes to the requested chart data file.</returns>
    private StreamWriter OpenChartWriter(
        string subDir,
        string fileName,
        ChartType chartType,
        out ChartDataFileHelper chartDataFileHelper,
        int nbDatasets = 1)
    {
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(),
            _configuration[Constants.CHART_DATA_FILES_ROOT_DIR]!);

        return OpenChartWriter(rootDir, subDir, fileName, chartType, out chartDataFileHelper, nbDatasets);
    }

    /// <summary>
    /// Creates and initializes a writer for a chart data file below the specified root directory.
    /// The target directory is created when it does not already exist.
    /// </summary>
    /// <param name="rootDir">The root directory containing chart data files.</param>
    /// <param name="subDir">The subdirectory below <paramref name="rootDir"/> in which the file is created.</param>
    /// <param name="fileName">The name of the chart data file to create.</param>
    /// <param name="chartType">The type of chart for which the data file is being generated.</param>
    /// <param name="chartDataFileHelper">When this method returns, the initialized helper associated with the returned writer.</param>
    /// <param name="nbDatasets">The number of datasets expected by the chart helper.</param>
    /// <returns>A <see cref="StreamWriter"/> that writes to the requested chart data file.</returns>
    private static StreamWriter OpenChartWriter(
        string rootDir,
        string subDir,
        string fileName,
        ChartType chartType,
        out ChartDataFileHelper chartDataFileHelper,
        int nbDatasets = 1)
    {
        var directory = Path.Combine(rootDir, subDir);
        Directory.CreateDirectory(directory);

        var streamWriter = new StreamWriter(Path.Combine(directory, fileName));
        chartDataFileHelper = new ChartDataFileHelper(streamWriter, chartType, nbDatasets);
        chartDataFileHelper.WriteBeforeData();

        return streamWriter;
    }

    /// <summary>
    /// Writes a Markdown include file with TOML-style front matter and one line per supplied item.
    /// </summary>
    /// <param name="fileName">The name of the Markdown file to create in the configured includes directory.</param>
    /// <param name="title">The title written to the file's front matter.</param>
    /// <param name="lines">The lines to write after the front matter.</param>
    private void WriteMarkdownListFile(string fileName, string title, IEnumerable<string> lines)
    {
        var filePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            _configuration[Constants.CONTENT_ROOT_DIR]!,
            "../includes",
            fileName);

        using var streamWriter = new StreamWriter(filePath);
        streamWriter.WriteLine("+++");
        streamWriter.WriteLine($"title = \"{title}\"");
        streamWriter.WriteLine("+++");

        foreach (var line in lines)
            streamWriter.WriteLine(line);
    }
}
