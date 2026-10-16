using System.Globalization;
using Toolbox.Domain;

namespace Toolbox.Processors;

/// <summary>
/// Provides shared parsing state and helpers for poem and season metadata processors.
/// Concrete processors recognize the syntax specific to YAML or TOML.
/// </summary>
public abstract class PoemMetadataProcessorBase : IPoemMetadataProcessor
{
    /// <summary>Stores each collected list or multiline field under its logical field type.</summary>
    private readonly Dictionary<MultilineMetadataProcessingType, List<string>> _values = new()
    {
        [MultilineMetadataProcessingType.Categories] = [],
        [MultilineMetadataProcessingType.Tags] = [],
        [MultilineMetadataProcessingType.Pictures] = [],
        [MultilineMetadataProcessingType.InfoLines] = [],
        [MultilineMetadataProcessingType.DescriptionLines] = [],
        [MultilineMetadataProcessingType.Locations] = [],
        [MultilineMetadataProcessingType.WordCloudLines] = []
    };

    /// <summary>Gets the number of separator characters between a field name and its value.</summary>
    protected abstract int FieldSuffixLength { get; }

    /// <summary>Gets whether this processor exposes the full set of poem metadata fields.</summary>
    protected virtual bool IncludesPoemFields => true;

    /// <summary>Gets or sets the field that receives subsequent multiline values.</summary>
    protected MultilineMetadataProcessingType CurrentMultilineType { get; set; }

    /// <summary>Gets category values collected from the metadata.</summary>
    public List<string> Categories => _values[MultilineMetadataProcessingType.Categories];

    /// <summary>Gets tag values collected from the metadata.</summary>
    public List<string> Tags => _values[MultilineMetadataProcessingType.Tags];

    /// <summary>Gets picture values collected from the metadata.</summary>
    public List<string> Pictures => _values[MultilineMetadataProcessingType.Pictures];

    /// <summary>Gets information lines collected from the metadata.</summary>
    public List<string> InfoLines => _values[MultilineMetadataProcessingType.InfoLines];

    /// <summary>Gets description lines collected from the metadata.</summary>
    public List<string> DescriptionLines => _values[MultilineMetadataProcessingType.DescriptionLines];

    /// <summary>Gets location values collected from the metadata.</summary>
    public List<string> Locations => _values[MultilineMetadataProcessingType.Locations];

    /// <summary>Gets word-cloud lines collected from the metadata.</summary>
    public List<string> WordCloudLines => _values[MultilineMetadataProcessingType.WordCloudLines];

    /// <summary>
    /// Processes one metadata line as either a field declaration or a continuation of the active multiline field.
    /// </summary>
    /// <param name="line">The complete metadata line to parse.</param>
    /// <param name="indentation">The list-item indentation width, or -2 when no list prefix is present.</param>
    public MetadataFieldUpdate? ProcessLine(string line, int indentation = -2)
    {
        if (TryProcessField(line, out var update)) return update;
        AddValue(line, indentation);
        return null;
    }

    /// <summary>Processes a field declaration specific to the concrete metadata format.</summary>
    /// <param name="line">The complete field declaration line.</param>
    /// <returns>True when this processor recognized and consumed the line.</returns>
    protected abstract bool TryProcessFormatSpecificField(string line);

    /// <summary>Processes a multiline value using format-specific cleanup or terminator rules.</summary>
    /// <param name="lineValue">The line after its list indentation prefix has been removed.</param>
    /// <returns>True when this processor consumed the line.</returns>
    protected abstract bool TryAddFormatSpecificValue(string lineValue);

    /// <summary>Processes the value written on an info, description, or word-cloud field declaration.</summary>
    /// <param name="line">The complete field declaration.</param>
    /// <param name="fieldName">The metadata field name to extract.</param>
    /// <param name="type">The collection that receives inline or following multiline values.</param>
    /// <param name="isMultiline">
    /// True when the caller's format treats this text field as multiline regardless of the current value,
    /// as with TOML fields that use triple-quoted strings. The value itself is also checked by
    /// <see cref="IsMultilineTextField"/>, which detects an opening marker such as YAML's <c>|</c> or TOML's <c>"""</c>.
    /// Set this to false when multiline behavior depends on the value's opening marker, as with YAML.
    /// </param>
    protected void ProcessTextField(string line, string fieldName, MultilineMetadataProcessingType type, bool isMultiline)
    {
        SetCurrentType(type);
        var rawValue = ExtractFieldValue(line, fieldName);
        var isMultilineValue = IsMultilineTextField(rawValue);
        var value = DecodeTextFieldValue(rawValue);
        if (string.IsNullOrEmpty(value) || value == EmptyTextFieldMarker)
        {
            SetCurrentType(MultilineMetadataProcessingType.None);
            return;
        }

        if (isMultiline || isMultilineValue)
        {
            // Keep the selected field active; later lines will be added until their format-specific terminator.
            return;
        }

        AddCurrentValue(value);
        SetCurrentType(MultilineMetadataProcessingType.None);
    }

    /// <summary>Decodes the syntax around an inline text-field value for the current format.</summary>
    protected abstract string? DecodeTextFieldValue(string value);

    /// <summary>Gets whether the current text field uses a multiline representation.</summary>
    protected abstract bool IsMultilineTextField(string fieldValue);

    /// <summary>Gets the marker that represents an explicitly empty text field, if the format has one.</summary>
    protected virtual string? EmptyTextFieldMarker => null;

    /// <summary>Tries to consume a triple-quoted multiline string terminator shared by TOML processors.</summary>
    /// <param name="lineValue">The current continuation line.</param>
    /// <returns>True when the line ended the active triple-quoted field.</returns>
    protected bool TryConsumeTripleQuoteTerminator(string lineValue)
    {
        if (!lineValue.EndsWith(MetadataConstants.TRIPLE_DOUBLE_QUOTE, StringComparison.Ordinal)) return false;

        // TOML permits content on the same line as the closing marker, so retain that content.
        AddCurrentValue(RemoveTripleQuoteTerminator(lineValue));
        SetCurrentType(MultilineMetadataProcessingType.None);
        return true;
    }

    /// <summary>Extracts a field value using this format's field separator width.</summary>
    protected string ExtractFieldValue(string line, string fieldName) => line[(fieldName.Length + FieldSuffixLength)..];

    /// <summary>Parses an ISO date and formats it as expected by poem metadata consumers.</summary>
    protected static string FormatDate(string value) => DateTime.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None).ToString("dd.MM.yyyy");

    /// <summary>Parses the integer representation used by weight fields.</summary>
    protected static int ParseWeight(string value) => int.Parse(value);

    /// <summary>Removes the three quote characters that terminate a TOML multiline string.</summary>
    protected static string RemoveTripleQuoteTerminator(string value) => value[..^3];

    /// <summary>Selects the list that will receive subsequent multiline values.</summary>
    protected void SetCurrentType(MultilineMetadataProcessingType type) => CurrentMultilineType = type;

    /// <summary>Replaces the values for a field with values parsed from an inline declaration.</summary>
    protected void SetValues(MultilineMetadataProcessingType type, IEnumerable<string> values)
    {
        _values[type].Clear();
        _values[type].AddRange(values);
    }

    /// <summary>Parses a common poem field when the supplied line declares one.</summary>
    protected bool TrySetCommonField(string line, out MetadataFieldUpdate? update)
    {
        if (StartsWithField(line, MetadataConstants.TITLE)) update = CreateUpdate(PoemMetadataField.Title, ParseString(ExtractFieldValue(line, MetadataConstants.TITLE)));
        else if (StartsWithField(line, MetadataConstants.ID)) update = CreateUpdate(PoemMetadataField.Id, ParseString(ExtractFieldValue(line, MetadataConstants.ID)));
        else if (StartsWithField(line, MetadataConstants.DATE)) update = CreateUpdate(PoemMetadataField.TextDate, FormatDate(ExtractFieldValue(line, MetadataConstants.DATE)));
        else if (StartsWithField(line, MetadataConstants.ACROSTICHE)) update = CreateUpdate(PoemMetadataField.Acrostiche, ParseOptionalString(ExtractFieldValue(line, MetadataConstants.ACROSTICHE)));
        else if (StartsWithField(line, MetadataConstants.DOUBLE_ACROSTICHE)) update = CreateUpdate(PoemMetadataField.DoubleAcrostiche, ParseDoubleAcrostiche(ExtractFieldValue(line, MetadataConstants.DOUBLE_ACROSTICHE)));
        else if (StartsWithField(line, MetadataConstants.VERSE_LENGTH)) update = CreateUpdate(PoemMetadataField.VerseLength, ParseVerseLength(ExtractFieldValue(line, MetadataConstants.VERSE_LENGTH)));
        else if (StartsWithField(line, MetadataConstants.POEM_TYPE)) update = CreateUpdate(PoemMetadataField.PoemType, ParseOptionalString(ExtractFieldValue(line, MetadataConstants.POEM_TYPE)));
        else if (StartsWithField(line, MetadataConstants.WEIGHT)) update = CreateUpdate(PoemMetadataField.Weight, ParseWeight(ExtractFieldValue(line, MetadataConstants.WEIGHT)));
        else { update = null; return false; }
        return true;
    }

    /// <summary>Checks a field name followed by a YAML colon or a TOML equals sign.</summary>
    protected static bool StartsWithField(string line, string field) =>
        line.StartsWith(field + ":", StringComparison.Ordinal)
        || line.StartsWith(field + " =", StringComparison.Ordinal)
        || line.StartsWith(field + "=", StringComparison.Ordinal);

    /// <summary>Normalizes a non-empty string according to the active metadata format.</summary>
    protected string ParseString(string value) => value.CleanedContent()!;

    /// <summary>Normalizes an optional string according to the active metadata format.</summary>
    protected virtual string? ParseOptionalString(string value) => ParseString(value);

    /// <summary>Parses the verse-length field, whose representation may be numeric and unquoted.</summary>
    protected virtual string? ParseVerseLength(string value) => ParseOptionalString(value);

    /// <summary>Parses the first two pipe-separated components of a double-acrostic value.</summary>
    protected virtual DoubleAcrostiche? ParseDoubleAcrostiche(string value)
    {
        var parts = ParseString(value).Split('|');
        return parts.Length < 2 ? null : new DoubleAcrostiche { First = parts[0].Trim(), Second = parts[1].Trim() };
    }

    /// <summary>Adds a value to the list selected by the current multiline field type.</summary>
    protected void AddCurrentValue(string value) => _values[CurrentMultilineType].Add(value);

    /// <summary>Routes a line to common poem fields or the concrete format parser.</summary>
    /// <summary>Creates a result for a parsed scalar without retaining it in processor state.</summary>
    private static MetadataFieldUpdate CreateUpdate(PoemMetadataField field, object? value) => new(field, value);

    /// <summary>Routes scalar declarations to the shared parser and collection declarations to the active format.</summary>
    private bool TryProcessField(string line, out MetadataFieldUpdate? update)
    {
        update = null;
        // Season index files share TOML syntax but only use the title from the common field set.
        if (IncludesPoemFields && TrySetCommonField(line, out update)) return true;
        if (!IncludesPoemFields && StartsWithField(line, MetadataConstants.TITLE))
        {
            update = CreateUpdate(PoemMetadataField.Title, ParseString(ExtractFieldValue(line, MetadataConstants.TITLE)));
            return true;
        }
        return TryProcessFormatSpecificField(line);
    }

    /// <summary>Appends a continuation line to the active field after shared normalization rules.</summary>
    private void AddValue(string line, int indentation)
    {
        if (CurrentMultilineType == MultilineMetadataProcessingType.None) return;

        // A YAML list item must be indented; non-list YAML lines are ignored while collecting list values.
        if (indentation == -2 && line.Length > 0 && line[0] != ' ' && this is PoemYamlMetadataProcessor) return;

        var lineValue = line.Length == 0 ? line : line[(indentation + 2)..];

        // These generated acrostic callout lines are presentation markup, not poem metadata text.
        if (CurrentMultilineType == MultilineMetadataProcessingType.InfoLines
            && (lineValue is "{{% callout style=\"primary\" %}}" or "{{% /callout %}}"
                || lineValue.StartsWith("Acrostiche :", StringComparison.Ordinal))) return;

        if (TryAddFormatSpecificValue(lineValue)) return;

        switch (CurrentMultilineType)
        {
            // Categories and pictures retain their text after the list prefix has been removed.
            case MultilineMetadataProcessingType.Categories:
            case MultilineMetadataProcessingType.Pictures:
                AddCurrentValue(lineValue);
                break;

            // YAML list entries may be quoted, while TOML applies its own cleanup in the override above.
            case MultilineMetadataProcessingType.Tags:
            case MultilineMetadataProcessingType.Locations:
                AddCurrentValue(lineValue.StartsWith(MetadataConstants.DOUBLE_QUOTE, StringComparison.Ordinal) ? lineValue.CleanedContent()! : lineValue);
                break;

            // Text blocks discard indentation that belongs to the surrounding metadata layout.
            case MultilineMetadataProcessingType.InfoLines:
            case MultilineMetadataProcessingType.DescriptionLines:
            case MultilineMetadataProcessingType.WordCloudLines:
                AddCurrentValue(lineValue.TrimStart(' '));
                break;
        }
    }
}
