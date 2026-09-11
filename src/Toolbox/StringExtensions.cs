using System.Globalization;
using System.Text;

namespace Toolbox;

public static class StringExtensions
{
    /// <summary>
    /// Escape double quote by \ sign.
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static string Escaped(this string s)
    {
        if (string.IsNullOrEmpty(s) || !s.Contains('"')) return s;
        return s.Replace("\"", "\\\"");
    }

    /// <summary>
    /// Remove any \ placed before a double quote.
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static string? Unescaped(this string s) => string.IsNullOrEmpty(s) ? null : s.Replace("\\\"", "\"");

    /// <summary>
    /// Put lowercase, remove accents and replace space, ' and - signs by underscore.
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static string UnaccentedCleaned(this string s)
    {
        var normalized = s.Normalize(NormalizationForm.FormD);
        Span<char> buffer = normalized.Length <= 256
            ? stackalloc char[normalized.Length]
            : new char[normalized.Length];

        int pos = 0;
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue; // removed accent

            var lower = char.ToLowerInvariant(c);

            if (lower is >= 'a' and <= 'z')
            {
                buffer[pos++] = lower;
            }
            else if (lower is '\'' or '-' or ' ')
            {
                if (pos > 0 && buffer[pos - 1] != '_')
                    buffer[pos++] = '_';
            }
        }

        while (pos > 0 && buffer[pos - 1] == '_') pos--;
        int start = 0;
        while (start < pos && buffer[start] == '_') start++;

        return pos == start ? string.Empty : new string(buffer[start..pos]);
    }
    
    /// <summary>
    /// Returns the substring before the last underscore ('_') in the input string.
    /// If no underscore is found, the original string is returned unchanged.
    /// </summary>
    /// <param name="s">The source string to process.</param>
    /// <returns>The portion of the string preceding the last underscore.</returns>
    public static string GetStringBeforeLastUnderscore(this string s)
    {
        if (string.IsNullOrEmpty(s))
            return s;

        int lastIndex = s.LastIndexOf('_');
        return lastIndex == -1 ? s : s.Substring(0, lastIndex);
    }

    /// <summary>
    /// Expect a quoted string, cleanup the quotes around the string, and the escaping of any quote into the string.
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static string? CleanedContent(this string? s)
    {
        if (string.IsNullOrEmpty(s) || s.Length < 2) return null;

        var inner = s.AsSpan(1, s.Length - 2);
        if (inner.IsEmpty) return null;

        Span<char> buffer = inner.Length <= 256 ? stackalloc char[inner.Length] : new char[inner.Length];
        int pos = 0;
        foreach (var c in inner)
        {
            if (c != '\\') buffer[pos++] = c;
        }

        return pos == 0 ? null : new string(buffer[..pos]);
    }
    
    /// <summary>
    /// Échappe le contenu d'une chaîne JavaScript entre apostrophes. Les appelants fournissent
    /// du texte brut : l'échappement se fait une seule fois, au point d'émission du code.
    /// Les antislashs doivent être traités avant les apostrophes pour qu'une entrée comme
    /// \\';alert(1) ne puisse pas fermer la chaîne. Les contrôles, séparateurs Unicode et
    /// chevrons sont encodés aussi, y compris si le fichier est ensuite inséré dans du HTML.
    /// customScalesOptions reste une option de code réservée aux appelants de confiance.
    /// </summary>
    public static string JavaScriptString(this string? s)
    {
        if (s is null) return string.Empty;
        var result = new StringBuilder(s.Length);
        foreach (var c in s)
        {
            if (c is '\\' or '\'') result.Append('\\').Append(c);
            else if (char.IsControl(c) || c is '\u2028' or '\u2029' or '<' or '>')
                result.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
            else result.Append(c);
        }
        return result.ToString();
    }
    
    /// <summary>
    /// Parses to a date using "dd.MM.yyyy" format.
    /// </summary>
    /// <param name="textDate"></param>
    /// <returns></returns>
    public static DateTime ToDateTime(this string textDate) => DateTime.ParseExact(textDate, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None);

    /// <summary>
    /// Renders a markdown link.
    /// </summary>
    /// <param name="item">For example a category name, as given by storage values</param>
    /// <param name="itemTypes">For example "categories"</param>
    /// <returns></returns>
    public static string MarkdownLink(this string item, string itemTypes) => $"[{item}](/{itemTypes}/{item.ToLowerInvariant().Replace(' ', '-')})";
    
    /// <summary>
    /// Parses a string of comma separated integers.
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static int[] ToIntArray(this string s)
    {
        var span = s.AsSpan();
        int count = 1;
        foreach (var c in span) if (c == ',') count++;

        var result = new int[count];
        // La taille vient des métadonnées : une allocation de pile non bornée permettrait
        // à une longue entrée de provoquer un StackOverflowException non récupérable.
        // Les entrées courantes restent sur la pile ; les grandes utilisent le tas.
        Span<char> buffer = span.Length <= 256 ? stackalloc char[span.Length] : new char[span.Length];
        int idx = 0, bufLen = 0;

        for (int i = 0; i <= span.Length; i++)
        {
            if (i == span.Length || span[i] == ',')
            {
                result[idx++] = int.Parse(buffer[..bufLen], CultureInfo.InvariantCulture);
                bufLen = 0;
            }
            else if (span[i] != ' ')
            {
                buffer[bufLen++] = span[i];
            }
        }
        return result;
    }
}
