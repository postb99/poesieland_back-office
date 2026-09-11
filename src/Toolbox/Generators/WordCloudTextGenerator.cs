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
        // Index local à cette génération : un seul parcours des poèmes, sans cache périmé
        // après un import. Distinct préserve l'ancien Contains si un tag est répété.
        var poemsByMonth = data.Seasons.SelectMany(x => x.Poems)
            .SelectMany(poem => (poem.ExtraTags ?? []).Distinct(StringComparer.Ordinal)
                .Where(tag => _months.Contains(tag)).Select(month => (month, poem)))
            .ToLookup(x => x.month, x => x.poem, StringComparer.Ordinal);
        foreach (var month in _months)
        {
            var rootDir = Path.Combine(configuration[Constants.CONTENT_ROOT_DIR]!, "..", "other-perspectives",
                "les-mois", month);
            var filePath = Path.Combine(rootDir, "wordcloud.txt");
            // Un flux tamponné par mois remplace deux ouvertures/fermetures par poème.
            // WriteLine conserve l'ordre, l'encodage UTF-8 et la ligne vide sans WordCloud.
            using var writer = new StreamWriter(filePath, append: false);
            foreach (var poem in poemsByMonth[month])
            {
                writer.WriteLine(poem.WordCloud?.ToLowerInvariant());
            }
        }
    }
}
