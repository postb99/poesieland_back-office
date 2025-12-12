using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit3;
using Toolbox.Domain;

namespace Tests.Customizations;

public class MyCustomization : CompositeCustomization
{
    internal static int SeasonId() => new Random().Next(100, 999);

    public MyCustomization() : base(
        new AutoMoqCustomization(), 
        new SeasonCustomization(), 
        new PoemCustomization())
    {
    }
}

public class AutoDomainDataAttribute() : AutoDataAttribute(() => new Fixture().Customize(new MyCustomization()));

public class InlineAutoDomainDataAttribute(params object[] values) : InlineAutoDataAttribute(() =>
    new Fixture().Customize(new MyCustomization()), values);

public class SeasonCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customize<Season>(composer => composer.With(s => s.Id, MyCustomization.SeasonId()));
    }
}

public class PoemCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        var randomString = fixture.Create<string>();

        fixture.Customize<Poem>(composer =>
            composer.With(p => p.Id, randomString + "_" + MyCustomization.SeasonId())
                .With(p => p.TextDate, DateTime.Now.ToString("dd.MM.yyyy")));
    }
}