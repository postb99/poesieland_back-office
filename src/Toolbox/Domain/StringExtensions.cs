using System.Globalization;
using System.Text.RegularExpressions;

namespace Toolbox.Domain;

public static class StringExtensions
{
    /// <summary>
    /// Escape double quote by \ sign.
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static string Escaped(this string s) => s.Replace("\"", "\\\"");
    
    /// <summary>
    /// Remove any \ placed before a double quote.
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static string? Unescaped(this string s) => string.IsNullOrEmpty(s) ? null : s.Replace("\\\"", "\"");

    /// <summary>
    /// Convert to lowercase and replace ' and - by _.
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
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
        return string.IsNullOrEmpty(unescaped) ? null : unescaped.Replace("\\", "");
    }
    /// <summary>
    /// Parses to a date using "dd.MM.yyyy" format.
    /// </summary>
    /// <param name="textDate"></param>
    /// <returns></returns>
    public static DateTime ToDateTime(this string textDate) => DateTime.ParseExact(textDate, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None);
}