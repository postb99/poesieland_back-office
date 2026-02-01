using Shouldly;
using Tests.Customizations;
using Toolbox.Consistency;
using Toolbox.Domain;
using Xunit;

namespace Tests.Consistency;

public class CustomPageCheckerTest(BasicFixture fixture, ITestOutputHelper testOutputHelper)
    : IClassFixture<BasicFixture>
{
    [Theory]
    [Trait("UnitTest", "ContentConsistencyCheck")]
    [AutoDomainData]
    public void ShouldNotFailWhenCheckingGloballyForPoemsNotListedOnLesMoisCustomPage(Root data)
    {
        data.Seasons.First().Poems.First().ExtraTags.Add("les mois");
        var customPageChecker = new CustomPageChecker(fixture.Configuration);
        var outputs = customPageChecker.GetPoemWithLesMoisExtraTagNotListedOnCustomPage(null, data);
        foreach (var output in outputs)
        {
            testOutputHelper.WriteLine(output);
        }
    }

    [Theory]
    [Trait("UnitTest", "ContentConsistencyCheck")]
    [AutoDomainData]
    public void ShouldNotFailWhenCheckingForPoemNotListedOnLesMoisCustomPage(Root data, Poem poem)
    {
        poem.ExtraTags.Add("les mois");
        var customPageChecker = new CustomPageChecker(fixture.Configuration);
        var output = customPageChecker.GetPoemWithLesMoisExtraTagNotListedOnCustomPage(poem, data);
        output.ShouldHaveSingleItem();
        testOutputHelper.WriteLine(output.FirstOrDefault());
    }

    [Theory]
    [Trait("UnitTest", "ContentConsistencyCheck")]
    [AutoDomainData]
    public void ShouldNotFailWhenCheckingGloballyForPoemsNotListedOnCielCustomPage(Root data)
    {
        data.Seasons.First().Poems[0].Categories.First().SubCategories.Add("Ciel");
        data.Seasons.First().Poems[0].Paragraphs.First().Verses[0] = "Le ciel est beau";
        
        data.Seasons.First().Poems[1].Categories.First().SubCategories.Add("Ciel");
        data.Seasons.First().Poems[1].Paragraphs.First().Verses[0] = "Le ciel est gris";
        
        var customPageChecker = new CustomPageChecker(fixture.Configuration);
        var outputs = customPageChecker.GetPoemOfSkyCategoryStartingWithSpecificWordsNotListedOnCustomPage(null, data);
        outputs.Where(x => !string.IsNullOrEmpty(x)).ShouldHaveSingleItem();
        foreach (var output in outputs)
        {
            testOutputHelper.WriteLine(output);
        }
    }

    [Theory]
    [Trait("UnitTest", "ContentConsistencyCheck")]
    [AutoDomainData]
    public void ShouldNotFailWhenCheckingForPoemNotListedOnCielCustomPage(Root data, Poem poem)
    {
        poem.Categories.First().SubCategories.Add("Ciel");
        poem.Paragraphs.First().Verses[0] = "Le ciel est beau";
        
        var customPageChecker = new CustomPageChecker(fixture.Configuration);
        var output = customPageChecker.GetPoemWithLesMoisExtraTagNotListedOnCustomPage(poem, data);
        testOutputHelper.WriteLine(output.FirstOrDefault());
    }
    
    [Theory]
    [Trait("UnitTest", "ContentConsistencyCheck")]
    [AutoDomainData]
    public void ShouldNotFailWhenCheckingGloballyForPoemsNotListedOnSaisonsCustomPage(Root data)
    {
        data.Seasons.First().Poems[0].Categories.Add(new Category {Name = "Saisons", SubCategories = ["Printemps", "Automne"]});

        data.Seasons.First().Poems[1].Categories.Add(new Category {Name = "Saisons", SubCategories = ["Printemps"]});
        
        var customPageChecker = new CustomPageChecker(fixture.Configuration);
        var outputs = customPageChecker.GetPoemOfMoreThanOneSeasonNotListedOnCustomPage(null, data);
        outputs.Where(x => !string.IsNullOrEmpty(x)).ShouldHaveSingleItem();
        foreach (var output in outputs)
        {
            testOutputHelper.WriteLine(output);
        }
    }
    
    [Theory]
    [Trait("UnitTest", "ContentConsistencyCheck")]
    [AutoDomainData]
    public void ShouldNotFailWhenCheckingForPoemNotListedOnSaisonsCustomPage(Root data, Poem poem)
    {
        poem.Categories.Add(new Category {Name = "Saisons", SubCategories = ["Printemps", "Automne"]});
        
        var customPageChecker = new CustomPageChecker(fixture.Configuration);
        var output = customPageChecker.GetPoemOfMoreThanOneSeasonNotListedOnCustomPage(poem, data);
        testOutputHelper.WriteLine(output.FirstOrDefault());
    }
}