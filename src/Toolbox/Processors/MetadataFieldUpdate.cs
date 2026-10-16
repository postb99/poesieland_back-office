using Toolbox.Domain;

namespace Toolbox.Processors;

/// <summary>Identifies a scalar poem metadata field produced by processing one line.</summary>
public enum PoemMetadataField
{
    /// <summary>The poem title.</summary>
    Title,
    /// <summary>The poem identifier.</summary>
    Id,
    /// <summary>The poem date in display format.</summary>
    TextDate,
    /// <summary>The poem acrostic.</summary>
    Acrostiche,
    /// <summary>The poem's two-part acrostic.</summary>
    DoubleAcrostiche,
    /// <summary>The declared verse length or metric.</summary>
    VerseLength,
    /// <summary>The poem type.</summary>
    PoemType,
    /// <summary>The file ordering weight.</summary>
    Weight
}

/// <summary>Contains a scalar metadata field and its parsed value returned for one input line.</summary>
/// <param name="Field">The scalar field represented by the result.</param>
/// <param name="Value">The parsed string, acrostic object, integer, or null optional value.</param>
public sealed record MetadataFieldUpdate(PoemMetadataField Field, object? Value);
