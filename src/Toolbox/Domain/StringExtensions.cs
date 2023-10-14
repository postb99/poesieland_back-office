using System.Text.RegularExpressions;

namespace Toolbox.Domain;

public static class StringExtensions
{
    public static string Escaped(this string s) => s.Replace("\"", "\\\"");

    public static string UnaccentedCleaned(this string s)
    {
        var unaccented =
            System.Text.Encoding.UTF8.GetString(System.Text.Encoding.GetEncoding("ISO-8859-8").GetBytes(s.ToLowerInvariant()));

        // Replace ' and - then keep a to z and _ then clean multiple _
        var cleaned = Regex.Replace(Regex.Replace(Regex.Replace(unaccented, @"[' -]+", "_"), @"[^a-z_]+", ""), "_+", "_").Trim('_');
        return cleaned;
    }

    public static string? ContentCleanString(this string? s)
    {
        var c = s?.Trim('\'');
        return string.IsNullOrEmpty(c) ? null : c;
    }
}