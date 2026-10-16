namespace Toolbox.Processors;

/// <summary>Identifies which metadata field receives continuation lines during parsing.</summary>
public enum MultilineMetadataProcessingType
{
    /// <summary>No multiline field is currently active.</summary>
    None,
    /// <summary>Category values are being collected.</summary>
    Categories,
    /// <summary>Tag values are being collected.</summary>
    Tags,
    /// <summary>Picture values are being collected.</summary>
    Pictures,
    /// <summary>Information text lines are being collected.</summary>
    InfoLines,
    /// <summary>Description text lines are being collected.</summary>
    DescriptionLines,
    /// <summary>Location values are being collected.</summary>
    Locations,
    /// <summary>Word-cloud text lines are being collected.</summary>
    WordCloudLines
}
