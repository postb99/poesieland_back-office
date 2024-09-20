using FluentAssertions;
using Toolbox.Domain;

namespace Tests;

public class PoemTest
{
    [Fact]
    public void ShouldHaveIntegerDetailedVerseLength()
    {
        var poem = new Poem
        {
            VerseLength = "8"
        };

        poem.DetailedVerseLength.Should().Be("8");
    }
    
    [Theory]
    [InlineData("Vers variable : 6, 3")]
    [InlineData("Vers variable : 6, 3.")]
    [InlineData("Vers variable : 6, 3. Autre info.")]
    public void ShouldHaveMultipleIntegersDetailedVerseLength(string info)
    {
        var poem = new Poem
        {
            VerseLength = "-1",
            Info = info
        };

        poem.DetailedVerseLength.Should().Be("6, 3");
    }

    [Fact]
    public void ShouldThrowNullInfo()
    {
        var poem = new Poem
        {
            VerseLength = "-1",
            Info = null
        };

        var func = () => poem.DetailedVerseLength;
        func.Should().Throw<InvalidOperationException>();
    }
    
    [Fact]
    public void ShouldThrowInfoWithoutVersVariable()
    {
        var poem = new Poem
        {
            VerseLength = "-1",
            Info = "Some info"
        };

        var func = () => poem.DetailedVerseLength;
        func.Should().Throw<InvalidOperationException>();
    }
}