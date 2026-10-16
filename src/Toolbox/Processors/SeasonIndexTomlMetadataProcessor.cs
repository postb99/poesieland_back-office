namespace Toolbox.Processors;

public sealed class SeasonIndexTomlMetadataProcessor : PoemMetadataProcessorBase
{
    /// <summary>Uses TOML's equals sign and following space as the field separator.</summary>
    protected override int FieldSuffixLength => 3;

    /// <summary>Season index entries contain descriptions, but not poem-specific metadata fields.</summary>
    protected override bool IncludesPoemFields => false;

    /// <summary>Parses the season index weight without enabling poem-only scalar fields.</summary>
    protected override bool TryProcessFormatSpecificScalarField(string line, out MetadataFieldUpdate? update)
    {
        if (!StartsWithField(line, MetadataConstants.WEIGHT))
        {
            update = null;
            return false;
        }

        update = CreateUpdate(PoemMetadataField.Weight, ParseWeight(ExtractFieldValue(line, MetadataConstants.WEIGHT)));
        return true;
    }

    /// <summary>Recognizes and initializes a TOML season description field.</summary>
    protected override bool TryProcessFormatSpecificField(string line)
    {
        if (StartsWithField(line, MetadataConstants.DESCRIPTION))
        {
            ProcessTextField(line, MetadataConstants.DESCRIPTION, MultilineMetadataProcessingType.DescriptionLines, isMultiline: false);
            return true;
        }
        return false;
    }

    /// <summary>Collects description lines until the TOML triple-quote terminator is found.</summary>
    protected override bool TryAddFormatSpecificValue(string lineValue)
    {
        if (CurrentMultilineType != MultilineMetadataProcessingType.DescriptionLines) return false;
        if (TryConsumeTripleQuoteTerminator(lineValue)) return true;

        // Season descriptions preserve their TOML indentation just like poem text blocks.
        AddCurrentValue(lineValue);
        return true;
    }

    /// <summary>Removes TOML quotes and escapes from a season description field.</summary>
    protected override string? DecodeTextFieldValue(string value) => value.CleanedContent();

    /// <summary>Recognizes TOML triple quotes as the opener for a multiline description.</summary>
    protected override bool IsMultilineTextField(string fieldValue) => fieldValue.StartsWith(MetadataConstants.TRIPLE_DOUBLE_QUOTE, StringComparison.Ordinal);
}
