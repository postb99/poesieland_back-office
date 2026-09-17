namespace Toolbox.Settings;

public class StorageSettings
{
    public List<Category> Categories { get; set; } = [];

    public List<string> SubcategorieNames => Categories.SelectMany(x => x.Subcategories).Select(x => x.Name).ToList();
}

public class Category
{
    public required string Name { get; set; }
    
    public required string Color { get; set; }

    public List<SubCategory> Subcategories { get; set; } = [];
}

public class SubCategory
{
    /// <summary>
    /// Name, whose normalized string gives frontmatter value.
    /// </summary>
    public required string Name { get; set; }
    
    /// <summary>
    /// Optional label.
    /// </summary>
    public string Label { get; set; }
    
    /// <summary>
    /// Title, whose value (Label, defaulting to Name) is used for chart titles.
    /// </summary>
    public string Title => Label ?? Name;

    public required string Color { get; set; }
    
    public string? Alias { get; set; }
}
