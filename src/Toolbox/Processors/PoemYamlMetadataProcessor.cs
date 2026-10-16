using Toolbox.Domain;

namespace Toolbox.Processors;

/// <summary>Parses poem metadata written in the project's YAML front matter format.</summary>
public sealed class PoemYamlMetadataProcessor : PoemMetadataProcessorBase
{
    /// <summary>YAML field values begin after a colon and one following space.</summary>
    protected override int FieldSuffixLength => 2;

    /// <summary>YAML continuation values use the shared processing rules without extra format-specific handling.</summary>
    protected override bool TryAddFormatSpecificValue(string lineValue) => false;

    /// <summary>Returns YAML scalar text unchanged because YAML titles and identifiers are commonly unquoted.</summary>
    protected override string ParseString(string value) => value;

    /// <summary>Recognizes YAML list fields and inline scalar or block fields.</summary>
    protected override bool TryProcessFormatSpecificField(string line)
    {
        if (StartsWithField(line, MetadataConstants.CATEGORIES)) SetCurrentType(MultilineMetadataProcessingType.Categories);
        else if (StartsWithField(line, MetadataConstants.TAGS)) SetCurrentType(MultilineMetadataProcessingType.Tags);
        else if (StartsWithField(line, MetadataConstants.PICTURES)) SetCurrentType(MultilineMetadataProcessingType.Pictures);
        else if (StartsWithField(line, MetadataConstants.INFO))
            ProcessTextField(line, MetadataConstants.INFO, MultilineMetadataProcessingType.InfoLines, isMultiline: false);
        else if (StartsWithField(line, MetadataConstants.DESCRIPTION))
            ProcessTextField(line, MetadataConstants.DESCRIPTION, MultilineMetadataProcessingType.DescriptionLines, isMultiline: false);
        else if (StartsWithField(line, MetadataConstants.WORDCLOUD))
            ProcessTextField(line, MetadataConstants.WORDCLOUD, MultilineMetadataProcessingType.WordCloudLines, isMultiline: false);
        else if (StartsWithField(line, MetadataConstants.LOCATIONS)) SetCurrentType(MultilineMetadataProcessingType.Locations);
        else return false;
        return true;
    }

    /// <summary>Treats an empty YAML scalar or a pair of quotes as an absent optional value.</summary>
    protected override string? ParseOptionalString(string value)
    {
        var parsed = value.Trim('"');
        return parsed is "" or MetadataConstants.DOUBLE_QUOTE_PAIR ? null : parsed;
    }

    /// <summary>Decodes an inline YAML text-field scalar using YAML's optional-value rules.</summary>
    protected override string? DecodeTextFieldValue(string value) => ParseOptionalString(value);

    /// <summary>Recognizes YAML's pipe marker as the start of a multiline text block.</summary>
    protected override bool IsMultilineTextField(string fieldValue) => fieldValue.TrimStart().StartsWith("|", StringComparison.Ordinal);

    /// <summary>Gets YAML's explicit empty scalar marker used by text fields.</summary>
    protected override string? EmptyTextFieldMarker => MetadataConstants.PIPE_HYPHEN;

    /// <summary>Splits the YAML double-acrostic value at its first pipe separator.</summary>
    protected override DoubleAcrostiche? ParseDoubleAcrostiche(string value)
    {
        var parts = value.Split('|');
        return parts.Length < 2 ? null : new() { First = parts[0].Trim(), Second = parts[1].Trim() };
    }

}
