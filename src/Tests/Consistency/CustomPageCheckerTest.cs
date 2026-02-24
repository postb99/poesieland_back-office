using Shouldly;
using Tests.Customizations;
using Toolbox.Consistency;
using Toolbox.Domain;
using Xunit;

namespace Tests.Consistency;

public class CustomPageCheckerTest(BasicFixture fixture) : IClassFixture<BasicFixture>
{
    [Theory]
    [Trait("UnitTest", "ContentConsistencyCheck")]
    [AutoDomainData]
    public void ShouldThrowWhenCheckingGloballyForPoemsNotListedOnLesMoisCustomPage(Root data)
    {
        data.Seasons.First().Poems.First().ExtraTags.Add("les mois");
        var customPageChecker = new CustomPageChecker(fixture.Configuration);
        var act = () => customPageChecker.VerifyPoemsWithLesMoisExtraTagIsListedOnCustomPage(null, data);
        act.ShouldThrow<CustomPageConsistencyException>().Message.ShouldBe(
            $"Poem {data.Seasons.First().Poems.First().Id} should be listed on 'les mois' tag index page!");
    }

    [Theory]
    [Trait("UnitTest", "ContentConsistencyCheck")]
    [AutoDomainData]
    public void ShouldThrowFailWhenCheckingForPoemNotListedOnLesMoisCustomPage(Root data, Poem poem)
    {
        poem.ExtraTags.Add("les mois");
        var customPageChecker = new CustomPageChecker(fixture.Configuration);
        var act = () => customPageChecker.VerifyPoemsWithLesMoisExtraTagIsListedOnCustomPage(poem, data);
        act.ShouldThrow<CustomPageConsistencyException>().Message
            .ShouldBe($"Poem {poem.Id} should be listed on 'les mois' tag index page!");
    }

    [Theory]
    [Trait("UnitTest", "ContentConsistencyCheck")]
    [AutoDomainData]
    public void ShouldThrowWhenCheckingGloballyForPoemsNotListedOnCielCustomPage(Root data)
    {
        data.Seasons.First().Poems[0].Categories.First().SubCategories.Add("Ciel");
        data.Seasons.First().Poems[0].Paragraphs.First().Verses[0] = "Le ciel est beau";

        data.Seasons.First().Poems[1].Categories.First().SubCategories.Add("Ciel");
        data.Seasons.First().Poems[1].Paragraphs.First().Verses[0] = "Le ciel est gris";

        var customPageChecker = new CustomPageChecker(fixture.Configuration);
        var act = () =>
            customPageChecker.VerifyPoemOfSkyCategoryStartingWithSpecificWordsIsListedOnCustomPage(null, data);
        act.ShouldThrow<CustomPageConsistencyException>().Message.ShouldBe(
            $"Poem {data.Seasons.First().Poems.First().Id} should be listed on 'Ciel' category index page!");
    }

    [Theory]
    [Trait("UnitTest", "ContentConsistencyCheck")]
    [AutoDomainData]
    public void ShouldThrowWhenCheckingForPoemNotListedOnCielCustomPage(Root data, Poem poem)
    {
        poem.Categories.First().SubCategories.Add("Ciel");
        poem.Paragraphs.First().Verses[0] = "Le ciel est beau";

        var customPageChecker = new CustomPageChecker(fixture.Configuration);
        var act = () =>
            customPageChecker.VerifyPoemOfSkyCategoryStartingWithSpecificWordsIsListedOnCustomPage(poem, data);
        act.ShouldThrow<CustomPageConsistencyException>().Message
            .ShouldBe($"Poem {poem.Id} should be listed on 'Ciel' category index page!");
    }

    [Theory]
    [Trait("UnitTest", "ContentConsistencyCheck")]
    [AutoDomainData]
    public void ShouldThrowWhenCheckingGloballyForPoemsNotListedOnSaisonsCustomPage(Root data)
    {
        data.Seasons.First().Poems[0].Categories.Add(new Category
            { Name = "Saisons", SubCategories = ["Printemps", "Automne"] });

        data.Seasons.First().Poems[1].Categories.Add(new Category { Name = "Saisons", SubCategories = ["Printemps"] });

        var customPageChecker = new CustomPageChecker(fixture.Configuration);
        var act = () => customPageChecker.VerifyPoemOfMoreThanOneSeasonIsListedOnCustomPage(null, data);
        act.ShouldThrow<CustomPageConsistencyException>().Message.ShouldBe(
            $"Poem {data.Seasons.First().Poems.First().Id} should be listed on 'saisons' tag index page!");
    }

    [Theory]
    [Trait("UnitTest", "ContentConsistencyCheck")]
    [AutoDomainData]
    public void ShouldThrowWhenCheckingForPoemNotListedOnSaisonsCustomPage(Root data, Poem poem)
    {
        poem.Categories.Add(new Category { Name = "Saisons", SubCategories = ["Printemps", "Automne"] });

        var customPageChecker = new CustomPageChecker(fixture.Configuration);
        var act = () => customPageChecker.VerifyPoemOfMoreThanOneSeasonIsListedOnCustomPage(poem, data);
        act.ShouldThrow<CustomPageConsistencyException>().Message
            .ShouldBe($"Poem {poem.Id} should be listed on 'saisons' tag index page!");
    }
}