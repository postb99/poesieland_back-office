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
    public required string Name { get; set; }

    public required string Color { get; set; }
    
    public string? Alias { get; set; }
}
