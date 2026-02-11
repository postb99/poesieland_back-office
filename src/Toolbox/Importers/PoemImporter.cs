using Microsoft.Extensions.Configuration;
using Toolbox.Consistency;
using Toolbox.Domain;
using Toolbox.Processors;
using Toolbox.Settings;
using Category = Toolbox.Domain.Category;

namespace Toolbox.Importers;

public interface IPoemImporter
{
    (Poem, int) Import(string contentFilePath);
}

public class PoemImporter(IConfiguration configuration): IPoemImporter
{
    private Poem _poem;
    private int _position;
    private bool _isInMetadata;
    private IPoemMetadataProcessor? _metadataProcessor;
    private PoemContentProcessor? _contentProcessor;
    private List<Metric> _metrics = configuration.GetSection(Constants.METRIC_SETTINGS).Get<MetricSettings>().Metrics;
    private RequiredDescriptionSettings _requiredDescriptionSettings = configuration.GetSection(Constants.REQUIRED_DESCRIPTION_SETTINGS).Get<RequiredDescriptionSettings>();
    
    public const string YamlMarker = "---";
    public const string TomlMarker = "+++";

    public bool HasTomlMetadata { get; private set; }

    public bool HasYamlMetadata { get; private set; }

    /// <summary>
    /// Imports a poem based on its identifier and updates the provided data model with the poem information.
    /// </summary>
    /// <param name="poemId">The unique identifier of the poem to import. It should end with the season id.</param>
    /// <param name="data">The root data model that contains seasons and poems where the imported poem will be added or updated.</param>
    /// <returns>Returns the imported <see cref="Poem"/> object representing the poem's details.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when:
    /// - The <paramref name="poemId"/> does not end with a valid season id.
    /// - No content directory corresponding to the specified season id exists.
    /// - The poem content file is not found.
    /// </exception>
    public Poem ImportPoem(string poemId, Root data)
    {
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(), configuration[Constants.CONTENT_ROOT_DIR]!);
        var seasonId = poemId.Substring(poemId.LastIndexOf('_') + 1);
        if (!int.TryParse(seasonId, out _))
        {
            throw new ArgumentException($"'{poemId}' does not end with season id");
        }

        var seasonDirName = Directory.EnumerateDirectories(rootDir)
            .FirstOrDefault(x => Path.GetFileName(x).StartsWith($"{seasonId}_"));
        if (seasonDirName is null)
        {
            throw new ArgumentException(
                $"No such season content directory for id '{seasonId}'. Create season directory before importing poem");
        }

        var poemFileName = $"{poemId.Substring(0, poemId.LastIndexOf('_'))}.md";
        var poemContentPath = Path.Combine(rootDir, seasonDirName, poemFileName);
        if (!File.Exists(poemContentPath))
        {
            throw new ArgumentException($"Poem content file not found: {poemContentPath}");
        }

        var (poem, _) = Import(poemContentPath);
        // TODO put back Console.WriteLine($"[ERROR]: {anomaly}");
        VerifyAnomaliesAfterImportAsync();
        var targetSeason = data.Seasons.FirstOrDefault(x => x.Id == int.Parse(seasonId));

        if (targetSeason is null)
        {
            targetSeason = new()
            {
                Id = int.Parse(seasonId), Name = "TODO", NumberedName = "TODO", Introduction = "TODO", Summary = "TODO",
                Poems = []
            };
            data.Seasons.Add(targetSeason);
        }

        var existingPosition = targetSeason.Poems.FindIndex(x => x.Id == poemId);

        if (existingPosition > -1)
            targetSeason.Poems[existingPosition] = poem;
        else
            targetSeason.Poems.Add(poem);

        return poem;
    }

    /// <summary>
    /// Imports all poems associated with a specified season and updates the provided data model.
    /// </summary>
    /// <param name="seasonId">The unique identifier of the season whose poems are to be imported.</param>
    /// <param name="data">The root data model containing seasons and poems, where the imported poems will be added or updated.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the content directory or season directory corresponding to the specified <paramref name="seasonId"/> is not found.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown when file access errors occur during the import operation.
    /// </exception>
    public void ImportPoemsOfSeason(int seasonId, Root data)
    {
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(), configuration[Constants.CONTENT_ROOT_DIR]!);
        var seasonDirName = Directory.EnumerateDirectories(rootDir)
            .FirstOrDefault(x => Path.GetFileName(x).StartsWith($"{seasonId}_"));
        var targetSeason = data.Seasons.FirstOrDefault(x => x.Id == seasonId);
        var poemFilePaths = Directory.EnumerateFiles(seasonDirName!).Where(x => !x.EndsWith("_index.md"));
        var poemsByPosition = new Dictionary<int, Poem>(50);
        foreach (var poemContentPath in poemFilePaths)
        {
            var (poem, position) =Import(poemContentPath);
            // TODO put back Console.WriteLine($"[ERROR]: {anomaly}");
            VerifyAnomaliesAfterImportAsync();

            poemsByPosition.Add(position, poem);
        }

        if (targetSeason is not null)
        {
            targetSeason.Poems.Clear();
        }
        else
        {
            targetSeason = new() { Id = seasonId, Poems = [] };
            data.Seasons.Add(targetSeason);
        }

        for (var i = 0; i < 50; i++)
        {
            if (poemsByPosition.TryGetValue(i, out var poem))
                targetSeason.Poems.Add(poem);
        }
    }

    /// <summary>
    /// Imports a poem in French from the specified content file, processes its metadata and content,
    /// and returns a tuple with the constructed poem and its positional index.
    /// </summary>
    /// <param name="contentFilePath">The full file path to the poem content file to be imported. This should include any metadata and content related to the poem.</param>
    /// <returns>Returns a tuple where the first element is the <see cref="Poem"/> object containing the processed poem information, and the second element is an integer representing the position or index of the poem.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the specified content file does not exist.</exception>
    /// <exception cref="InvalidDataException">Thrown when the content file contains invalid or malformed data.</exception>
    /// <exception cref="IOException">Thrown when there is an issue reading the content file.</exception>
    public (Poem, int) Import(string contentFilePath)
    {
        Init();

        using var streamReader = new StreamReader(contentFilePath);
        string line;
        do
        {
            line = streamReader.ReadLine();
            ProcessLine(line);
        } while (line is not null);

        _poem.Categories = GetCategories(_metadataProcessor!.GetCategories(), _poem.Id);
        _poem.Pictures = _metadataProcessor.GetPictures();
        var poemInfo = _metadataProcessor.GetInfoLines().Count == 0 ? null : string.Join(Environment.NewLine, _metadataProcessor.GetInfoLines());
        _poem.Info = poemInfo;
        _poem.Paragraphs = _contentProcessor!.Paragraphs;
        _poem.ExtraTags = FindExtraTags(_metadataProcessor.GetTags());
        _poem.Locations = _metadataProcessor.GetLocations();

        // Copy for XML save
        _poem.VerseLength = _poem.DetailedMetric;

        return (_poem, _position);
    }
    
    /// <summary>
    /// Imports a poem in English from the specified content file, processes its YAML metadata and content,
    /// and returns a tuple with the constructed poem and its positional index.
    /// </summary>
    /// <param name="contentFilePath">The full file path to the poem content file to be imported. This should include any metadata and content related to the poem.</param>
    /// <returns>Returns a tuple where the first element is the <see cref="Poem"/> object containing the processed poem information, and the second element is an integer representing the position or index of the poem.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the specified content file does not exist.</exception>
    /// <exception cref="InvalidDataException">Thrown when the content file contains invalid or malformed data.</exception>
    /// <exception cref="IOException">Thrown when there is an issue reading the content file.</exception>
    public (Poem, int) ImportEnYaml(string contentFilePath)
    {
       Init();

        using var streamReader = new StreamReader(contentFilePath);
        string line;
        do
        {
            line = streamReader.ReadLine();
            ProcessLine(line);
        } while (line is not null);

        _poem.Categories = GetCategoriesEn(_metadataProcessor!.GetCategories(), _poem.Id);
        var poemInfo = _metadataProcessor.GetInfoLines().Count == 0 ? null : string.Join(Environment.NewLine, _metadataProcessor.GetInfoLines());
        _poem.Info = poemInfo;
        _poem.Paragraphs = _contentProcessor!.Paragraphs;
        _poem.Locations = _metadataProcessor.GetLocations();

        // Copy for XML save
        _poem.VerseLength = _poem.DetailedMetric;

        return (_poem, _position);
    }

    /// <summary>
    /// Imports poems in English from the designated content directory and updates the provided data model with the imported poems, organizing them by their respective seasons and positions.
    /// </summary>
    /// <param name="dataEn">The root data model containing seasons and poems to be updated with imported English poems.</param>
    /// <exception cref="DirectoryNotFoundException">
    /// Thrown when the content root directory for English poems cannot be found.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown when there is an error accessing files in the content directory.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when there is an issue with parsing the content of a poem file, such as missing or malformed metadata.
    /// </exception>
    public void ImportPoemsEn(Root dataEn)
    {
        var rootDir = Path.Combine(Directory.GetCurrentDirectory(), configuration[Constants.CONTENT_ROOT_DIR_EN]!);

        foreach (var season in dataEn.Seasons)
        {
            season.Poems.Clear();
        }

        var yearDirNames = Directory.EnumerateDirectories(rootDir);

        foreach (var yearDirName in yearDirNames)
        {
            var poemFilePaths = Directory.EnumerateFiles(yearDirName).Where(x => !x.EndsWith("_index.md"));
            var poemsByPosition = new Dictionary<int, Poem>(50);

            foreach (var poemContentPath in poemFilePaths)
            {
                var (poem, position) = ImportEnYaml(poemContentPath);

                poemsByPosition.Add(position, poem);
            }

            for (var i = 0; i < 50; i++)
            {
                if (poemsByPosition.TryGetValue(i, out var poem))
                {
                    var seasonId = poem.SeasonId;
                    var targetSeason = dataEn.Seasons.FirstOrDefault(x => x.Id == seasonId);
                    if (targetSeason is null)
                    {
                        targetSeason = new() { Id = seasonId, Poems = [] };
                        dataEn.Seasons.Add(targetSeason);
                    }

                    targetSeason.Poems.Add(poem);
                }
            }
        }
    }

    /// <summary>
    /// Filters out specific tags by removing those that match predefined categories, metrics, certain year ranges, or other specific tags to ignore.
    /// </summary>
    /// <param name="tags">A list of tags to be evaluated and filtered.</param>
    /// <returns>Returns a list of tags that do not fall under the predefined exclusion criteria.</returns>
    public List<string> FindExtraTags(List<string> tags)
    {
        var tagsToIgnore = new List<string>();
        
        // Should neither be a storage category
        tagsToIgnore.AddRange(configuration.GetSection(Constants.STORAGE_SETTINGS).Get<StorageSettings>().Categories.Select(x => x.Name.ToLowerInvariant()));
        
        // Nor a metric name
        tagsToIgnore.AddRange(_metrics.Select(x => x.Name.ToLowerInvariant()));

        // Nor a year
        tagsToIgnore.AddRange(Enumerable.Range(1994, DateTime.Now.Year - 1993).Select(x => x.ToString()));

        // Nor a specific tag
        tagsToIgnore.AddRange(["pantoun", "sonnet", "acrostiche", "doubleAcrostiche"]);
        
        return tags.Where(x => !tagsToIgnore.Contains(x)).ToList();
    }

    /// <summary>
    /// Verifies that no anomalies exist in the imported poem data by calling <see cref="PoemMetadataChecker"/>.
    /// </summary>
    /// <returns>An enumerable collection of strings, where each string represents a specific anomaly detected during the import process.</returns>
    public async Task<IEnumerable<string>> VerifyAnomaliesAfterImportAsync()
    {
        var partialImport = new PartialImport
        {
            DetailedMetric = _poem.DetailedMetric,
            HasVariableMetric = _poem.HasVariableMetric,
            Tags = _metadataProcessor!.GetTags(),
            PoemId = _poem.Id,
            Year = _poem.Date.Year,
            Info = _poem.Info,
            Description = _poem.Description
        };
        return await PoemMetadataChecker.VerifyAnomaliesAsync(partialImport, _metrics, _requiredDescriptionSettings);
    }

    /// <summary>
    /// Processes a poem content file to generate a partial import object, extracting metadata, tags, and poem-specific details.
    /// </summary>
    /// <param name="contentFilePath">The file path of the content file to process for partial import.</param>
    /// <returns>Returns an instance of <see cref="PartialImport"/> that contains the extracted metadata, tags, and other details of the poem.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the specified <paramref name="contentFilePath"/> does not exist.</exception>
    /// <exception cref="InvalidDataException">Thrown when the content file contains invalid or unexpected format preventing metadata processing.</exception>
    public PartialImport GetPartialImport(string contentFilePath)
    {
        _poem = new();
        _isInMetadata = false;
        _metadataProcessor = null;
        _contentProcessor = null;
        HasYamlMetadata = false;
        HasTomlMetadata = false;

        using var streamReader = new StreamReader(contentFilePath);
        string line;
        do
        {
            line = streamReader.ReadLine();
            ProcessLine(line);
        } while (line is not null);
        
        var poemInfo = _metadataProcessor.GetInfoLines().Count == 0 ? null : string.Join(Environment.NewLine, _metadataProcessor.GetInfoLines());
        _poem.Info = poemInfo;

        return new()
        {
            Tags = _metadataProcessor.GetTags(),
            PoemId = _poem.Id,
            Year = _poem.Date.Year,
            HasVariableMetric = _poem.HasVariableMetric,
            DetailedMetric = _poem.DetailedMetric,
            Info = _poem.Info,
            Description = _poem.Description
        };
    }

    /// <summary>
    /// Represents a partial import of a poem during content processing.
    /// Contains extracted metadata, tags, and poem-specific information.
    /// </summary>
    public record PartialImport
    {
        public List<string> Tags { get; set; } = new();
        public int Year { get; set; }
        public string PoemId { get; set; }
        public bool HasVariableMetric { get; set; }
        
        public string DetailedMetric { get; set; }
        
        public string? Info { get; set; }
        public string? Description { get; set; }
    }

    /// <summary>
    /// Initializes the fields and properties required for processing a poem,
    /// setting default values and clearing any existing state for a new import operation.
    /// </summary>
    private void Init()
    {
        _poem = new();
        _isInMetadata = false;
        _metadataProcessor = null;
        _contentProcessor = null;
        HasYamlMetadata = false;
        HasTomlMetadata = false;
    }

    /// <summary>
    /// Processes a single line of poem content, identifying metadata and verses, and updates the internal state accordingly.
    /// </summary>
    /// <param name="line">The line of poem content to process. It may represent a metadata marker, a metadata line, or a verse.</param>
    private void ProcessLine(string? line)
    {
        if (line == null)
            return;

        if (line.StartsWith(TomlMarker))
        {
            HasTomlMetadata = true;
            _metadataProcessor ??= new PoemTomlMetadataProcessor();
            _isInMetadata = !_isInMetadata;
        }
        else if (line.StartsWith(YamlMarker))
        {
            HasYamlMetadata = true;
            _metadataProcessor ??= new PoemYamlMetadataProcessor();
            _isInMetadata = !_isInMetadata;
        }

        if (_isInMetadata)
            ProcessMetadataLine(line);
        else
            ProcessVerses(line);
    }

    /// <summary>
    /// Processes a line of metadata and extracts or builds corresponding details for the poem.
    /// </summary>
    /// <param name="line">The metadata line to process. This line could define various attributes of the poem such as title, id, categories, tags, or other properties.</param>
    private void ProcessMetadataLine(string line)
    {
        if (line.StartsWith("title"))
        {
            _poem.Title = _metadataProcessor!.GetTitle(line);
        }
        else if (line.StartsWith("id"))
        {
            _poem.Id = _metadataProcessor!.GetId(line);
        }
        else if (line.StartsWith("date"))
        {
            _poem.TextDate = _metadataProcessor!.GetTextDate(line);
        }
        else if (line.StartsWith("categories"))
        {
            _metadataProcessor!.BuildCategories(line);
        }
        else if (line.StartsWith("tags"))
        {
            _metadataProcessor!.BuildTags(line);
        }
        else if (line.StartsWith("pictures"))
        {
            _metadataProcessor!.BuildPictures(line);
        }
        else if (line.StartsWith("info"))
        {
            _metadataProcessor!.BuildInfoLines(line);
        }
        else if (line.StartsWith("description"))
        {
            _poem.Description = _metadataProcessor!.GetDescription(line);
        }
        else if (line.StartsWith("acrostiche"))
        {
            _poem.Acrostiche = _metadataProcessor!.GetAcrostiche(line);
        }
        else if (line.StartsWith("doubleAcrostiche"))
        {
            _poem.DoubleAcrostiche = _metadataProcessor!.GetDoubleAcrostiche(line);
        }
        else if (line.StartsWith("verseLength"))
        {
            _poem.VerseLength = _metadataProcessor!.GetVerseLength(line);
        }
        else if (line.StartsWith("poemType"))
        {
            _poem.PoemType = _metadataProcessor!.GetType(line);
        }
        else if (line.StartsWith("weight"))
        {
            _position = _metadataProcessor!.GetWeight(line) - 1;
        }
        else if (line.StartsWith("locations"))
        {
            _metadataProcessor!.BuildLocations(line);
        }
        else if (line.StartsWith("    - "))
        {
            _metadataProcessor!.AddValue(line, 4);
        }
        else if (line.StartsWith("  - "))
        {
            _metadataProcessor!.AddValue(line, 2);
        }
        else
        {
            // blank line or any text line, starting with spaces or not
            _metadataProcessor!.AddValue(line, -2);
        }
    }

    /// <summary>
    /// Maps the provided metadata categories to their corresponding storage categories and organizes them
    /// based on the storage settings configuration.
    /// </summary>
    /// <param name="metadataCategories">A list of category names obtained from the metadata of a poem.</param>
    /// <param name="poemId">The unique identifier of the poem used for error tracking during the mapping process.</param>
    /// <returns>Returns a list of <see cref="Category"/> objects representing the categories and their associated subcategories.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a metadata category does not match any storage category defined in the storage settings.
    /// The exception includes the <paramref name="poemId"/> and the unmatched metadata category for debugging purposes.
    /// </exception>
    private List<Category> GetCategories(List<string> metadataCategories, string poemId)
    {
        var storageCategories = new Dictionary<string, Category>();
        var storageSettings = configuration.GetSection(Constants.STORAGE_SETTINGS).Get<StorageSettings>();

        foreach (var metadataCategory in metadataCategories)
        {
            var settingsCategory =
                storageSettings.Categories.FirstOrDefault(x =>
                    x.Subcategories.Select(x => x.Name).Contains(metadataCategory));
            if (settingsCategory == null)
            {
                throw new InvalidOperationException(
                    $"[{poemId}] No storage category found for metadata category '{metadataCategory}', maybe you intended to add this value to tags instead of categories?");
            }

            storageCategories.TryGetValue(settingsCategory.Name, out var storageCategory);
            if (storageCategory == null)
            {
                storageCategory = new() { Name = settingsCategory.Name, SubCategories = new() };
                storageCategories.Add(storageCategory.Name, storageCategory);
            }

            storageCategory.SubCategories.Add(metadataCategory);
        }

        return storageCategories.Values.ToList();
    }
    
    /// <summary>
    /// Maps the provided metadata categories to their corresponding storage categories and organizes them
    /// based on the storage settings configuration.
    /// </summary>
    /// <param name="metadataCategories">A list of category names obtained from the metadata of a poem.</param>
    /// <param name="poemId">The unique identifier of the poem used for error tracking during the mapping process.</param>
    /// <returns>Returns a list of <see cref="Category"/> objects representing the categories and their associated subcategories.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a metadata category does not match any storage category defined in the storage settings.
    /// The exception includes the <paramref name="poemId"/> and the unmatched metadata category for debugging purposes.
    /// </exception>
    private List<Category> GetCategoriesEn(List<string> metadataCategories, string poemId)
    {
        var storageCategories = new Dictionary<string, Category>();
        var storageSettings = configuration.GetSection(Constants.STORAGE_SETTINGS).Get<StorageSettings>();

        foreach (var metadataCategory in metadataCategories)
        {
            var settingsCategory =
                storageSettings.Categories.FirstOrDefault(x =>
                    x.Subcategories.Select(x => x.Alias).Contains(metadataCategory));
             
            if (settingsCategory == null)
            {
                throw new InvalidOperationException(
                    $"[{poemId}] No storage category found for metadata category {metadataCategory}");
            }
            storageCategories.TryGetValue(settingsCategory.Name, out var storageCategory);
            if (storageCategory == null)
            {
                storageCategory = new() { Name = settingsCategory.Name, SubCategories = new() };
                storageCategories.Add(storageCategory.Name, storageCategory);
            }

            storageCategory.SubCategories.Add(settingsCategory.Subcategories.First(x => x.Alias == metadataCategory).Name);
        }

        return storageCategories.Values.ToList();
    }

    /// <summary>
    /// Processes a single line of verse by delegating it to the content processor for handling poem content.
    /// </summary>
    /// <param name="line">The line of verse to process.</param>
    private void ProcessVerses(string line)
    {
        _contentProcessor ??= new();
        _contentProcessor.AddLine(line);
    }
}
