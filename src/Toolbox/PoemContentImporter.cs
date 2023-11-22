﻿using Microsoft.Extensions.Configuration;
using Toolbox.Domain;
using Toolbox.Settings;
using Category = Toolbox.Domain.Category;

namespace Toolbox;

public class PoemContentImporter
{
    private IConfiguration _configuration;
    private Poem _poem;
    private int _position;
    private bool _isInMetadata;
    private IMetadataProcessor? _metadataProcessor;
    private ContentProcessor? _contentProcessor;

    public const string YamlMarker = "---";
    public const string TomlMarker = "+++";

    public bool HasTomlMetadata { get; private set; }

    public bool HasYamlMetadata { get; private set; }

    public (Poem, int) Import(string contentFilePath, IConfiguration configuration)
    {
        _configuration = configuration;
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
        _poem.Paragraphs = _contentProcessor!.Paragraphs;

        if (_poem.VerseLength == "-1")
        {
            if (_poem.Info == null || !_poem.Info!.StartsWith("Vers variable : "))
            {
                throw new InvalidOperationException(
                    "When verse length is -1, info should give variable length: 'Vers variable : ...'");
            }

            _poem.VerseLength = _poem.Info.Substring(16);
        }

        return (_poem, _position);
    }

    public (List<string>, int, string) GetTagsAndYear(string contentFilePath, IConfiguration configuration)
    {
        _configuration = configuration;
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

        return (_metadataProcessor.GetTags(), _poem.Date.Year, _poem.Id);
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
            _metadataProcessor!.BuildTags();
        }
        else if (line.StartsWith("pictures"))
        {
            _metadataProcessor!.BuildPictures(line);
        }
        else if (line.StartsWith("    - "))
        {
            _metadataProcessor!.AddValue(line, 4);
        }
        else if (line.StartsWith("  - "))
        {
            _metadataProcessor!.AddValue(line, 2);
        }
        else if (line.StartsWith("  ")) // double space only
        {
            _metadataProcessor!.AddValue(line, 0);
        }
        else if (line.StartsWith("info"))
        {
            _poem.Info = _metadataProcessor!.GetInfo(line);
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
        else if (line.StartsWith("type"))
        {
            _poem.PoemType = _metadataProcessor!.GetType(line);
        }
        else if (line.StartsWith("weight"))
        {
            _position = _metadataProcessor!.GetWeight(line) - 1;
        }
    }

    private List<Category> GetCategories(List<string> metadataCategories)
    {
        var storageCategories = new Dictionary<string, Category>();
        var storageSettings = _configuration.GetSection(Constants.STORAGE_SETTINGS).Get<StorageSettings>();

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