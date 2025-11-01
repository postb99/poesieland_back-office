using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit3;
using Toolbox.Domain;

namespace Tests.Customizations;

public class MyCustomization : CompositeCustomization
{
    public MyCustomization() : base(new AutoMoqCustomization(), new PoemCustomization()) {}
}

public class AutoDomainDataAttribute() : AutoDataAttribute(() => new Fixture().Customize(new MyCustomization()));

public class InlineAutoDomainDataAttribute(params object[] values) : InlineAutoDataAttribute(() =>
    new Fixture().Customize(new MyCustomization()), values);

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