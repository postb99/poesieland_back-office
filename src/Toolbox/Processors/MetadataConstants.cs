namespace Toolbox.Processors;

/// <summary>Defines field names and syntax markers shared by the metadata processors.</summary>
public static class MetadataConstants
{
    /// <summary>Marks the start or end of a YAML front matter block.</summary>
    public const string YAML_MARKER = "---";
    /// <summary>Marks the start or end of a TOML front matter block.</summary>
    public const string TOML_MARKER = "+++";

    /// <summary>Field name for the poem or season title.</summary>
    public const string TITLE = "title";
    /// <summary>Field name for the poem identifier.</summary>
    public const string ID = "id";
    /// <summary>Field name for the poem date.</summary>
    public const string DATE = "date";
    /// <summary>Shared metadata field or syntax marker named CATEGORIES.</summary>
    public const string CATEGORIES = "categories";
    /// <summary>Shared metadata field or syntax marker named TAGS.</summary>
    public const string TAGS = "tags";
    /// <summary>Shared metadata field or syntax marker named PICTURES.</summary>
    public const string PICTURES = "pictures";
    /// <summary>Shared metadata field or syntax marker named INFO.</summary>
    public const string INFO = "info";
    /// <summary>Shared metadata field or syntax marker named DESCRIPTION.</summary>
    public const string DESCRIPTION = "description";
    /// <summary>Shared metadata field or syntax marker named ACROSTICHE.</summary>
    public const string ACROSTICHE = "acrostiche";
    /// <summary>Shared metadata field or syntax marker named DOUBLE_ACROSTICHE.</summary>
    public const string DOUBLE_ACROSTICHE = "doubleAcrostiche";
    /// <summary>Shared metadata field or syntax marker named VERSE_LENGTH.</summary>
    public const string VERSE_LENGTH = "verseLength";
    /// <summary>Shared metadata field or syntax marker named POEM_TYPE.</summary>
    public const string POEM_TYPE = "poemType";
    /// <summary>Shared metadata field or syntax marker named WEIGHT.</summary>
    public const string WEIGHT = "weight";
    /// <summary>Shared metadata field or syntax marker named LOCATIONS.</summary>
    public const string LOCATIONS = "locations";
    /// <summary>Shared metadata field or syntax marker named WORDCLOUD.</summary>
    public const string WORDCLOUD = "wordcloud";
    /// <summary>Shared metadata field or syntax marker named FOUR_SPACES_DASH.</summary>
    public const string FOUR_SPACES_DASH = "    - ";
    /// <summary>Shared metadata field or syntax marker named TWO_SPACES_DASH.</summary>
    public const string TWO_SPACES_DASH = "  - ";
    
    /// <summary>Shared metadata field or syntax marker named TRIPLE_DOUBLE_QUOTE.</summary>
    public const string TRIPLE_DOUBLE_QUOTE = "\"\"\"";
    /// <summary>Shared metadata field or syntax marker named CLOSING_BRACKET.</summary>
    public const string CLOSING_BRACKET = "]";
    /// <summary>Shared metadata field or syntax marker named DOUBLE_QUOTE.</summary>
    public const string DOUBLE_QUOTE = "\"";
    /// <summary>Shared metadata field or syntax marker named DOUBLE_QUOTE_PAIR.</summary>
    public const string DOUBLE_QUOTE_PAIR = "\"\"";
    /// <summary>Shared metadata field or syntax marker named PIPE_HYPHEN.</summary>
    public const string PIPE_HYPHEN = "|-";
    
    /// <summary>Shared metadata field or syntax marker named CALLOUT_START.</summary>
    public const string CALLOUT_START = "{{% callout";
    /// <summary>Shared metadata field or syntax marker named FM_SNIPPET_START.</summary>
    public const string FM_SNIPPET_START = "<!-- FM:Snippet:";
    /// <summary>Shared metadata field or syntax marker named MARKDOWN_IMAGE_START.</summary>
    public const string MARKDOWN_IMAGE_START = "![";
    /// <summary>Shared metadata field or syntax marker named HUGO_SHORTCODE_START.</summary>
    public const string HUGO_SHORTCODE_START = "{{<";
    /// <summary>Shared metadata field or syntax marker named FOOTNOTE_REFERENCE_START.</summary>
    public const string FOOTNOTE_REFERENCE_START = "[^";
    
    /// <summary>Shared metadata field or syntax marker named PARAGRAPH_BREAK_MARKER.</summary>
    public const string PARAGRAPH_BREAK_MARKER = "\\";
}
