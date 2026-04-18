using Microsoft.Extensions.Configuration;
using Toolbox.Domain;
using Toolbox.Settings;

namespace Toolbox.Generators;

public class WordCloudTextGenerator(IConfiguration configuration)
{
    private static List<string> _months =
    [
        "janvier", "février", "mars", "avril", "mai", "juin", "juillet", "août", "septembre", "octobre", "novembre",
        "décembre"
    ];

    public void GenerateWordCloudFiles(Root data)
    {
        foreach (var month in _months)
        {
            var rootDir = Path.Combine(configuration[Constants.CONTENT_ROOT_DIR]!, "..", "other-perspectives",
                "les-mois", month);
            var filePath = Path.Combine(rootDir, "wordcloud.txt");
            var poems = data.Seasons.SelectMany(x => x.Poems).Where(p => p.ExtraTags.Contains(month));
            File.WriteAllText(filePath, string.Empty);
            foreach (var poem in poems)
            {
                File.AppendAllText(filePath, poem.Description.ToLowerInvariant());
                File.AppendAllText(filePath, Environment.NewLine);
            }
        }
    }
}