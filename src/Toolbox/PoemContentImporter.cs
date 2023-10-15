using Microsoft.Extensions.Configuration;
using Toolbox.Domain;
using Toolbox.Settings;
using Category = Toolbox.Domain.Category;

namespace Toolbox;

public class PoemContentImporter
{
    private IConfiguration _configuration;
    private Poem _poem;
    private bool _isInMetadata;
    private IMetadataProcessor _metadataProcessor;

    public const string YamlMarker = "---";
    public const string TomlMarker = "+++";

    public Poem Import(string contentFilePath, IConfiguration configuration)
    {
        _configuration = configuration;
        _poem = new Poem();

        using var streamReader = new StreamReader(contentFilePath);
        string line;
        do
        {
            line = streamReader.ReadLine();
            ProcessLine(line);
        } while (line != null);

        return _poem;
    }

    private void ProcessLine(string? line)
    {
        if (line == null)
            return;

        if (line.StartsWith(TomlMarker))
        {
            _metadataProcessor = new TomlMetadataProcessor();
            _isInMetadata = !_isInMetadata;
        }
        else if (line.StartsWith(YamlMarker))
        {
            _metadataProcessor = new YamlMetadataProcessor();
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
            _poem.Title = _metadataProcessor.GetTitle(line);
        }
        else if (line.StartsWith("id"))
        {
            _poem.Id = _metadataProcessor.GetId(line);
        }
        else if (line.StartsWith("date"))
        {
            _poem.TextDate = _metadataProcessor.GetTextDate(line);
        }
        else if (line.StartsWith("categories"))
        {
            _metadataProcessor.BuildCategories(line);
        }
        else if (line.StartsWith("    - "))
        {
            _metadataProcessor.AddValue(line);
        }
        else if (line.StartsWith("info"))
        {
            _poem.Info = _metadataProcessor.GetInfo(line);
        }
        else if (line.StartsWith("acrostiche"))
        {
            _poem.Acrostiche = _metadataProcessor.GetAcrostiche(line);
        }
        else if (line.StartsWith("doubleAcrostiche"))
        {
            _poem.DoubleAcrostiche = _metadataProcessor.GetDoubleAcrostiche(line);
        }
        else if (line.StartsWith("verseLength"))
        {
            _poem.VerseLength = _metadataProcessor.GetVerseLength(line);
        }
        else if (line.StartsWith("type"))
        {
            _poem.PoemType = _metadataProcessor.GetType(line);
        }

        _poem.Categories = GetCategories(_metadataProcessor.GetCategories());
    }

    private List<Category> GetCategories(List<string> metadataCategories)
    {
        var storageCategories = new Dictionary<string, Category>();
        var storageSettings = _configuration.GetSection(Constants.STORAGE_SETTINGS).Get<StorageSettings>();

        foreach (var metadataCategory in metadataCategories)
        {
            var cleanMetadataCategory = metadataCategory.CleanedContent();
            var settingsCategory =
                storageSettings.Categories.FirstOrDefault(x => x.Subcategories.Contains(cleanMetadataCategory));
            if (settingsCategory == null)
            {
                throw new InvalidOperationException(
                    $"No storage category found for metadata category {cleanMetadataCategory}");
            }

            storageCategories.TryGetValue(settingsCategory.Name, out var storageCategory);
            if (storageCategory == null)
            {
                storageCategory = new Category { Name = settingsCategory.Name, SubCategories = new List<string>() };
                storageCategories.Add(storageCategory.Name, storageCategory);
            }

            storageCategory.SubCategories.Add(cleanMetadataCategory);
        }

        return storageCategories.Values.ToList();
    }

    private void ProcessVerses(string line)
    {
        // TODO create a ContentProcessor.
    }
}