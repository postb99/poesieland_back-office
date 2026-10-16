namespace Toolbox.Processors;

/// <summary>Parses poem metadata written in the project's TOML front matter format.</summary>
public sealed class PoemTomlMetadataProcessor : PoemMetadataProcessorBase
{
    /// <summary>TOML field values begin after an equals sign and one following space.</summary>
    protected override int FieldSuffixLength => 3;

    /// <summary>Recognizes TOML arrays, scalar fields, and multiline string declarations.</summary>
    protected override bool TryProcessFormatSpecificField(string line)
    {
        if (StartsWithField(line, MetadataConstants.CATEGORIES))
            SetValues(MultilineMetadataProcessingType.Categories, ExtractArrayValues(ExtractFieldValue(line, MetadataConstants.CATEGORIES)));
        else if (StartsWithField(line, MetadataConstants.TAGS))
            ProcessArrayField(line, MetadataConstants.TAGS, MultilineMetadataProcessingType.Tags);
        else if (StartsWithField(line, MetadataConstants.PICTURES))
            ProcessArrayField(line, MetadataConstants.PICTURES, MultilineMetadataProcessingType.Pictures);
        else if (StartsWithField(line, MetadataConstants.LOCATIONS))
            SetValues(MultilineMetadataProcessingType.Locations, ExtractArrayValues(ExtractFieldValue(line, MetadataConstants.LOCATIONS)));
        else if (StartsWithField(line, MetadataConstants.INFO))
            ProcessTextField(line, MetadataConstants.INFO, MultilineMetadataProcessingType.InfoLines, isMultiline: false);
        else if (StartsWithField(line, MetadataConstants.DESCRIPTION))
            ProcessTextField(line, MetadataConstants.DESCRIPTION, MultilineMetadataProcessingType.DescriptionLines, isMultiline: false);
        else if (StartsWithField(line, MetadataConstants.WORDCLOUD))
            ProcessTextField(line, MetadataConstants.WORDCLOUD, MultilineMetadataProcessingType.WordCloudLines, isMultiline: false);
        else return false;
        return true;
    }

    /// <summary>Consumes TOML continuation lines and closes arrays or multiline strings at their terminators.</summary>
    protected override bool TryAddFormatSpecificValue(string lineValue)
    {
        if (CurrentMultilineType is MultilineMetadataProcessingType.InfoLines or MultilineMetadataProcessingType.DescriptionLines or MultilineMetadataProcessingType.WordCloudLines)
        {
            if (TryConsumeTripleQuoteTerminator(lineValue)) return true;

            // Preserve the indentation that is part of the TOML multiline string.
            AddCurrentValue(lineValue);
            return true;
        }
        else if (CurrentMultilineType is MultilineMetadataProcessingType.Tags or MultilineMetadataProcessingType.Pictures)
        {
            if (lineValue == MetadataConstants.CLOSING_BRACKET)
            {
                SetCurrentType(MultilineMetadataProcessingType.None);
                return true;
            }
            // TOML arrays use one quoted item per continuation line in the supported content files.
            AddCurrentValue(lineValue.TrimStart(' ').TrimEnd(',').Trim('"'));
            return true;
        }
        return false;
    }

    /// <summary>Extracts quoted array elements and drops separators and empty split segments.</summary>
    private static IEnumerable<string> ExtractArrayValues(string value) => value.Trim('[', ']').Trim(' ').Split('"').Where(x => x != string.Empty && x != ", ");

    /// <summary>Processes either an inline array or the opening line of a multiline array.</summary>
    private void ProcessArrayField(string line, string field, MultilineMetadataProcessingType type)
    {
        if (line == $"{field} = [") SetCurrentType(type);
        else SetValues(type, ExtractArrayValues(ExtractFieldValue(line, field)));
    }

    /// <summary>Removes TOML quotes and escape sequences from a text-field value.</summary>
    protected override string? DecodeTextFieldValue(string value) => value.CleanedContent();

    /// <summary>Recognizes TOML triple quotes as the opener for a multiline string.</summary>
    protected override bool IsMultilineTextField(string fieldValue) => fieldValue.StartsWith(MetadataConstants.TRIPLE_DOUBLE_QUOTE, StringComparison.Ordinal);

    /// <summary>Preserves TOML verse lengths as raw numeric or comma-separated values.</summary>
    protected override string? ParseVerseLength(string value) => value;
}
