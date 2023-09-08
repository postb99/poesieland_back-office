namespace Toolbox.Domain;

public static class StringExtensions
{
    public static string Escaped(this string s) => s.Replace("\"", "\\\"");

    public static string Unaccented(this string s) =>
        $"{System.Text.Encoding.UTF8.GetString(System.Text.Encoding.GetEncoding("ISO-8859-8").GetBytes(s.ToLowerInvariant())).Replace(' ', '_').Replace('\'', '_').Replace(".", string.Empty)}";
}