namespace Toolbox.Settings;

public class RequiredDescriptionSettings
{
    public List<RequiredDescription> RequiredDescriptions { get; set; } = [];
}

public class RequiredDescription
{
    public required string ExtraTag { get; set; }
    public bool Bold { get; set; }
}