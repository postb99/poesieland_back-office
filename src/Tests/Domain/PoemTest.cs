using System.Text;
using Microsoft.Extensions.Configuration;
using Shouldly;
using Toolbox.Domain;
using Toolbox.Settings;
using Xunit;

namespace Tests.Domain;

public class PoemTest(BasicFixture basicFixture) : IClassFixture<BasicFixture>
{
    [Fact]
    [Trait("UnitTest", "Computation")]
    public void ShouldHaveIntegerDetailedVerseLength()
    {
        var poem = new Poem
        {
            VerseLength = "8"
        };

        poem.DetailedMetric.ShouldBe("8");
    }

    [Theory]
    [Trait("UnitTest", "Computation")]
    [InlineData("Métrique variable : 6, 3")]
    [InlineData("Métrique variable : 6, 3.")]
    [InlineData("Métrique variable : 6, 3. Autre info.")]
    public void ShouldHaveMultipleIntegersDetailedVerseLength(string info)
    {
        var poem = new Poem
        {
            VerseLength = "-1",
            Info = info
        };

        poem.DetailedMetric.ShouldBe("6, 3");
    }

    [Theory]
    [Trait("UnitTest", "Computation")]
    [InlineData("6, 3", 6, true)]
    [InlineData("6, 3", 3, true)]
    [InlineData("6", 6, true)]
    [InlineData("Poème en prose", 6, false)]
    public void ShouldHaveMetric(string verseLength, int metric, bool expected)
    {
        var poem = new Poem
        {
            VerseLength = verseLength
        };

        poem.HasMetric(metric).ShouldBe(expected);
    }

    [Fact]
    [Trait("UnitTest", "Computation")]
    public void ShouldThrowNullInfo()
    {
        var poem = new Poem
        {
            VerseLength = "-1",
            Info = null
        };

        var func = () => poem.DetailedMetric;
        func.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    [Trait("UnitTest", "Computation")]
    public void ShouldThrowInfoWithoutVariableMetric()
    {
        var poem = new Poem
        {
            VerseLength = "-1",
            Info = "Some info"
        };

        var func = () => poem.DetailedMetric;
        func.ShouldThrow<InvalidOperationException>();
    }

    [Fact]
    [Trait("UnitTest", "ContentGeneration")]
    public void ShouldGenerateExpectedExtraAndMetricTags()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var poem = new Poem
        {
            Id = "poem_25",
            TextDate = "01.01.2025",
            Acrostiche = "Something",
            ExtraTags = ["wonderful"],
            VerseLength = "12"
        };
        poem.FileContent(-1, basicFixture.Configuration.GetSection(Constants.METRIC_SETTINGS).Get<MetricSettings>()!)
            .ShouldContain("tags = [\"wonderful\", \"2025\", \"acrostiche\", \"alexandrin\"]");
    }

    [Fact]
    [Trait("UnitTest", "ContentGeneration")]
    public void ShouldGenerateExpectedMultipleMetricTags()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var poem = new Poem
        {
            Id = "poem_25",
            TextDate = "01.01.2025",
            VerseLength = "6, 3"
        };
        poem.FileContent(-1, basicFixture.Configuration.GetSection(Constants.METRIC_SETTINGS).Get<MetricSettings>()!)
            .ShouldContain("tags = [\"2025\", \"métrique variable\", \"hexasyllabe\", \"trisyllabe\"]");
    }

    [Fact]
    [Trait("UnitTest", "ContentGeneration")]
    public void ShouldGenerateExpectedLocations()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var poem = new Poem
        {
            Id = "poem_25",
            TextDate = "01.01.2025",
            VerseLength = "8",
            Locations = ["Ici", "Là", "ailleurs"]
        };
        poem.FileContent(-1, basicFixture.Configuration.GetSection(Constants.METRIC_SETTINGS).Get<MetricSettings>()!)
            .ShouldContain("locations = [\"Ici\", \"Là\", \"ailleurs\"]");
    }
}