using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit3;
using Toolbox.Domain;

namespace Tests.Customizations;

public class AutoDomainDataAttribute : AutoDataAttribute
{
    public AutoDomainDataAttribute() : base(() => new Fixture().Customize(new CompositeCustomization(
        new AutoMoqCustomization(),
        new PoemCustomization())
    ))
    {
    }
}

public class PoemCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        var seasonId = fixture.Create<int>();
        var randomString = fixture.Create<string>();
        fixture.Customize<Poem>(composer =>
            composer.With(p => p.Id, randomString + "_" + seasonId)
            .With(p => p.TextDate, DateTime.Now.ToString("dd.MM.yyyy")));
    }
}