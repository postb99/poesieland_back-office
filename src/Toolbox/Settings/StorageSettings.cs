namespace Toolbox.Settings;

public class StorageSettings
{
    public List<Category> Categories { get; set; }
}

public class Category
{
    public string Name { get; set; }

    public List<SubCategory> Subcategories { get; set; }
}

public class SubCategory
{
    public string Name { get; set; }

    public string Color { get; set; }
}