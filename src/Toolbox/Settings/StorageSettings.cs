namespace Toolbox.Settings;

public class StorageSettings
{
    public List<Category> Categories { get; set; }
}

public class Category
{
    public string Name { get; set; }
    
    public List<string> Subcategories { get; set; }
}