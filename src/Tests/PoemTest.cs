using System.Text;
using Shouldly;
using Toolbox.Domain;
using Xunit;

namespace Tests;

public class PoemTest
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
    public void ShouldGenerateExpectedTags()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var poem = new Poem
        {
            Id = "poem_25",
            TextDate = "01.01.2025",
            Acrostiche = "Something",
            ExtraTags = ["wonderful"]
        };
        poem.FileContent(-1).ShouldContain("tags = [\"wonderful\", \"2025\", \"acrostiche\"]");
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
            Locations = ["Ici", "Là", "ailleurs"]
        };
        poem.FileContent(-1).ShouldContain("locations = [\"Ici\", \"Là\", \"ailleurs\"]");
    }
}