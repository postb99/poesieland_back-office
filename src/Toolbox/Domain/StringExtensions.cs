using System.Text.RegularExpressions;

namespace Toolbox.Domain;

public static class StringExtensions
{
    public static string Escaped(this string s) => s.Replace("\"", "\\\"");
    
    public static string Unescaped(this string s) => string.IsNullOrEmpty(s) ? null : s.Replace("\\\"", "\"");

    public static string UnaccentedCleaned(this string s)
    {
        var unaccented =
            System.Text.Encoding.UTF8.GetString(System.Text.Encoding.GetEncoding("ISO-8859-8").GetBytes(s.ToLowerInvariant()));

        // Replace ' and - then keep a to z and _ then clean multiple _
        var cleaned = Regex.Replace(Regex.Replace(Regex.Replace(unaccented, @"[' -]+", "_"), @"[^a-z_]+", ""), "_+", "_").Trim('_');
        return cleaned;
    }

    public static string? CleanedContent(this string? s)
    {
        var unescaped = s.Unescaped();
        if (unescaped == null) return null;
        var cleaned = s.Replace("\\", "").Replace("\"\"", "\"");
        if (cleaned.StartsWith("\"") && !cleaned.EndsWith("\"")) return cleaned.TrimEnd('"');
        if (!cleaned.StartsWith("\"") && cleaned.EndsWith("\"")) return cleaned.TrimStart('"');
        return cleaned.Trim('"');
    }
}