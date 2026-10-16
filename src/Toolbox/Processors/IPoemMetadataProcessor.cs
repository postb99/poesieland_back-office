namespace Toolbox.Processors;

/// <summary>Parses metadata one line at a time and exposes the accumulated poem values.</summary>
public interface IPoemMetadataProcessor
{
    /// <summary>Gets category values collected from the metadata.</summary>
    List<string> Categories { get; }

    /// <summary>Gets tag values collected from the metadata.</summary>
    List<string> Tags { get; }

    /// <summary>Gets picture values collected from the metadata.</summary>
    List<string> Pictures { get; }

    /// <summary>Gets information lines collected from the metadata.</summary>
    List<string> InfoLines { get; }

    /// <summary>Gets description lines collected from the metadata.</summary>
    List<string> DescriptionLines { get; }

    /// <summary>Gets location values collected from the metadata.</summary>
    List<string> Locations { get; }

    /// <summary>Gets word-cloud lines collected from the metadata.</summary>
    List<string> WordCloudLines { get; }

    /// <summary>Processes a field declaration or continuation line, preserving parser state between calls.</summary>
    /// <param name="line">The metadata line to process.</param>
    /// <param name="indentation">The list-item indentation width, or -2 when no list prefix is present.</param>
    /// <returns>The scalar field parsed from this line, or null for collection fields and continuation lines.</returns>
    MetadataFieldUpdate? ProcessLine(string line, int indentation = -2);
}
