using System.Globalization;
using Microsoft.Extensions.Configuration;
using Toolbox.Domain;
using Toolbox.Settings;

namespace Toolbox.Charts;

/// <summary>
/// Provides chart-specific data file generation methods for the application's chart data files.
/// </summary>
public partial class ChartDataFileGenerator
{
    /// <summary>
    /// Generates the Markdown listing of the ten most frequently associated pairs of subcategories.
    /// The pairs are ordered by their occurrence count and rendered as category links.
    /// </summary>
    /// <param name="dataDict">The association dictionary whose keys contain the two associated subcategory names and whose values contain their occurrence counts.</param>
    private void GenerateTopMostAssociatedCategoriesListing(Dictionary<KeyValuePair<string, string>, int> dataDict)
    {
        var sortedDict = dataDict.OrderByDescending(x => x.Value).Take(10).ToList();
        var subcategories = StorageSettings.Categories
            .SelectMany(x => x.Subcategories)
            .ToDictionary(x => x.Name);

        WriteMarkdownListFile("associated_categories.md", "Associations privilégiées",
            sortedDict.Select(x =>
                $"- {subcategories[x.Key.Key].MarkdownLink("categories")} et {subcategories[x.Key.Value].MarkdownLink("categories")}"));
    }

    /// <summary>
    /// Generates a Markdown listing containing the ten most frequently represented subcategories
    /// among the supplied poems.
    /// </summary>
    /// <param name="poems">The poems from which subcategory occurrence counts are calculated.</param>
    /// <param name="fileName">The name of the Markdown include file to create.</param>
    private void GenerateTopMostCategoriesListing(IEnumerable<Poem> poems, string fileName)
    {
        var dict = new Dictionary<string, int>();
        foreach (var poem in poems)
        {
            foreach (var cat in poem.Categories.SelectMany(x => x.SubCategories))
            {
                if (dict.TryGetValue(cat, out var count))
                    dict[cat] = ++count;
                else
                    dict[cat] = 1;
            }
        }

        var topMost = dict.OrderByDescending(x => x.Value).Take(10).ToList();
        var subcategories = StorageSettings.Categories
            .SelectMany(x => x.Subcategories)
            .ToDictionary(x => x.Name);

        WriteMarkdownListFile(fileName, "Associations privilégiées",
            topMost.Select(x => $"- {subcategories[x.Key].MarkdownLink("categories")}"));
    }
    /// <summary>
    /// Generates all Markdown listings related to category associations.
    /// The listings include the most frequently associated category pairs and
    /// the most frequently represented categories for poems matching specific
    /// properties such as the refrain tag, the "la mort" tag, or the sonnet form.
    /// </summary>
    /// <param name="categoriesDataDictionary">The category association data already calculated for the associated-categories bubble chart.</param>
    /// <param name="poems">The poems used to generate the category-specific listings.</param>
    private void GenerateCategoryAssociationListings(
        Dictionary<KeyValuePair<string, string>, int> categoriesDataDictionary,
        IEnumerable<Poem> poems)
    {
        GenerateTopMostAssociatedCategoriesListing(categoriesDataDictionary);
        GenerateTopMostCategoriesListing(
            poems.Where(x => x.ExtraTags.Contains("refrain")), "refrain_categories.md");
        GenerateTopMostCategoriesListing(
            poems.Where(x => x.ExtraTags.Contains("la mort")), "la_mort_categories.md");
        GenerateTopMostCategoriesListing(
            poems.Where(x => x.IsSonnet), "sonnet_categories.md");
    }

}
