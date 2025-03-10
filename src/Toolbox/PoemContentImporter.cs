using Microsoft.Extensions.Configuration;
using Toolbox.Domain;
using Toolbox.Settings;
using Category = Toolbox.Domain.Category;

namespace Toolbox;

public class PoemContentImporter(IConfiguration configuration)
{
    private Poem _poem;
    private int _position;
    private bool _isInMetadata;
    private IMetadataProcessor? _metadataProcessor;
    private ContentProcessor? _contentProcessor;

    public const string YamlMarker = "---";
    public const string TomlMarker = "+++";

    public bool HasTomlMetadata { get; private set; }

    public bool HasYamlMetadata { get; private set; }

    public (Poem, int) Import(string contentFilePath)
    {
        _poem = new Poem();
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
        } while (line != null);

        _poem.Categories = GetCategories(_metadataProcessor!.GetCategories());
        _poem.Pictures = _metadataProcessor.GetPictures();
        var poemInfo = _metadataProcessor.GetInfoLines().Count == 0 ? null : string.Join(Environment.NewLine, _metadataProcessor.GetInfoLines());
        _poem.Info = poemInfo;
        _poem.Paragraphs = _contentProcessor!.Paragraphs;
        _poem.ExtraTags = FindExtraTags(_metadataProcessor.GetTags());

        // Copy for XML save
        _poem.VerseLength = _poem.DetailedVerseLength;

        return (_poem, _position);
    }

    public List<string> FindExtraTags(List<string> tags)
    {
        var tagsToIgnore = new List<string>();
        
        // Should not be a storage category
        tagsToIgnore.AddRange(configuration.GetSection(Constants.STORAGE_SETTINGS).Get<StorageSettings>().Categories.Select(x => x.Name.ToLowerInvariant()));

        // Nor a year
        tagsToIgnore.AddRange(Enumerable.Range(1994, DateTime.Now.Year - 1993).Select(x => x.ToString()));

        // Nor a specific tag
        tagsToIgnore.AddRange(["métrique variable", "pantoun", "sonnet"]);
        
        return tags.Where(x => !tagsToIgnore.Contains(x)).ToList();
    }

    public (List<string>, int, string, bool) GetTagsYearVariableMetric(string contentFilePath)
    {
        _poem = new Poem();
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
        } while (line != null);
        
        var poemInfo = _metadataProcessor.GetInfoLines().Count == 0 ? null : string.Join(Environment.NewLine, _metadataProcessor.GetInfoLines());
        _poem.Info = poemInfo;

        return (_metadataProcessor.GetTags(), _poem.Date.Year, _poem.Id, _poem.HasVariableMetric);
    }

    private void ProcessLine(string? line)
    {
        if (line == null)
            return;

        if (line.StartsWith(TomlMarker))
        {
            HasTomlMetadata = true;
            _metadataProcessor ??= new TomlMetadataProcessor();
            _isInMetadata = !_isInMetadata;
        }
        else if (line.StartsWith(YamlMarker))
        {
            HasYamlMetadata = true;
            _metadataProcessor ??= new YamlMetadataProcessor();
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

    private List<Category> GetCategories(List<string> metadataCategories)
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
                    $"No storage category found for metadata category {metadataCategory}");
            }

            storageCategories.TryGetValue(settingsCategory.Name, out var storageCategory);
            if (storageCategory == null)
            {
                storageCategory = new Category { Name = settingsCategory.Name, SubCategories = new List<string>() };
                storageCategories.Add(storageCategory.Name, storageCategory);
            }

            storageCategory.SubCategories.Add(metadataCategory);
        }

        return storageCategories.Values.ToList();
    }

    private void ProcessVerses(string line)
    {
        _contentProcessor ??= new ContentProcessor();
        _contentProcessor.AddLine(line);
    }
}