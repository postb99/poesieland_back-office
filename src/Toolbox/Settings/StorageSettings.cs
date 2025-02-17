namespace Toolbox.Settings;

public class StorageSettings
{
    public List<Category> Categories { get; set; } = [];

    public List<string> SubcategorieNames => Categories.SelectMany(x => x.Subcategories).Select(x => x.Name).ToList();
}

public class Category
{
    public string Name { get; set; } = string.Empty;
    
    public string Color { get; set; } = string.Empty;

    public List<SubCategory> Subcategories { get; set; } = [];
}

public class SubCategory
{
    public string Name { get; set; } = string.Empty;

    public string Color { get; set; } = string.Empty;
}