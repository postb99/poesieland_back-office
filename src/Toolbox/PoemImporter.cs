using Microsoft.Extensions.Configuration;
using Toolbox.Domain;
using Toolbox.Settings;
using Category = Toolbox.Domain.Category;

namespace Toolbox;

public class PoemImporter(IConfiguration configuration)
{
    private Poem _poem;
    private int _position;
    private bool _isInMetadata;
    private IPoemMetadataProcessor? _metadataProcessor;
    private PoemContentProcessor? _contentProcessor;
    private List<Metric> _metrics = configuration.GetSection(Constants.METRIC_SETTINGS).Get<MetricSettings>().Metrics;

    public const string YamlMarker = "---";
    public const string TomlMarker = "+++";

    public bool HasTomlMetadata { get; private set; }

    public bool HasYamlMetadata { get; private set; }

    public (Poem, int) Import(string contentFilePath)
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

    public IEnumerable<string> CheckAnomaliesAfterImport()
    {
        foreach (var p in CheckAnomalies(new ()
                 {
                     DetailedMetric = _poem.DetailedMetric,
                     HasVariableMetric = _poem.HasVariableMetric,
                     Tags = _metadataProcessor!.GetTags(),
                     PoemId = _poem.Id,
                     Year = _poem.Date.Year,
                     Info = _poem.Info
                 })) yield return p;
        
        if (!_poem.HasVerseLength)
            yield return "Metric cannot be empty or 0";
    }

    public IEnumerable<string> CheckAnomalies(PartialImport partialImport)
    {
        // Poem year should be found in tags
        if (!partialImport.Tags.Contains(partialImport.Year.ToString()))
        {
            yield return "Missing year tag";
        }

        // When metric is variable, "métrique variable" tag should be found and info should mention it
        if (partialImport.HasVariableMetric)
        {
            if (!partialImport.Tags.Contains("métrique variable"))
            {
                yield return "Missing 'métrique variable' tag";
            }
            
            if (!partialImport.Info.Contains("Métrique variable : "))
            {
                yield return "Missing 'Métrique variable : ' in Info";
            }
        }

        // Name of metric should be found in tags
        foreach (var metric in partialImport.DetailedMetric.Split(','))
        {
            var expectedTag = _metrics.FirstOrDefault(x => x.Length.ToString() == metric.Trim())?.Name.ToLowerInvariant();
            if (!partialImport.Tags.Contains(expectedTag))
            {
                yield return $"Missing '{expectedTag}' tag";
            }
        }
    }

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
            Info = _poem.Info
        };
    }

    public record PartialImport
    {
        public List<string> Tags { get; set; } = new();
        public int Year { get; set; }
        public string PoemId { get; set; }
        public bool HasVariableMetric { get; set; }
        
        public string DetailedMetric { get; set; }
        
        public string Info { get; set; }
    }

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
                    $"[{poemId}] No storage category found for metadata category {metadataCategory}");
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

    private void ProcessVerses(string line)
    {
        _contentProcessor ??= new();
        _contentProcessor.AddLine(line);
    }
}