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

    /// <summary>
    /// Expect a quoted string, cleanup the quotes around the string and the escaping of any quote into the string.
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static string? CleanedContent(this string? s)
    {
        if (s.Length < 2) return null;
        var unescaped = s.Substring(1, s.Length - 2).Unescaped();
        if (unescaped == null) return null;
        return unescaped.Replace("\\", "");
    }
}