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