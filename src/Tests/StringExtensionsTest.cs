using System.Text;
using FluentAssertions;
using Toolbox.Domain;
using Xunit.Abstractions;

namespace Tests;

public class StringExtensionsTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    public StringExtensionsTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    [Theory]
    [Trait("UnitTest", "Computation")]
    [InlineData("Simple", "simple")]
    [InlineData("Au matin", "au_matin")]
    [InlineData("A la dérive", "a_la_derive")]
    [InlineData("Comme toi, aile", "comme_toi_aile")]
    [InlineData("Mais où vais-je ?", "mais_ou_vais_je")]
    [InlineData("L'air - créé", "l_air_cree")]
    [InlineData("Sais-tu l'amour...", "sais_tu_l_amour")]
    [InlineData("Marcher : neige", "marcher_neige")]
    [InlineData("Sur un air, souffles", "sur_un_air_souffles")]
    public void ShouldBeUnaccentedCleaned(string input, string expected)
    {
        input.UnaccentedCleaned().Should().Be(expected);
    }

    [Theory]
    [Trait("UnitTest", "Computation")]
    [InlineData("Simple", "Simple")]
    [InlineData("Vivere nell'arte", "Vivere nell'arte")]
    [InlineData("\"Vivere nell'arte\", en italien", "\\\"Vivere nell'arte\\\", en italien")]
    public void ShouldBeEscaped(string input, string expected)
    {
        _testOutputHelper.WriteLine("{0} => {1}", input, expected);
        input.Escaped().Should().Be(expected);
    }
    
    [Theory]
    [Trait("UnitTest", "Computation")]
    [InlineData(null, "")]
    [InlineData("Simple", "Simple")]
    [InlineData("Vivere nell'arte", "Vivere nell'arte")]
    [InlineData("\"Vivere nell'arte\", en italien", "\\\"Vivere nell'arte\\\", en italien")]
    public void ShouldBeUnescaped(string expected, string input)
    {
        _testOutputHelper.WriteLine("{0} => {1}", input, expected);
        input.Unescaped().Should().Be(expected);
    }
    
    [Theory]
    [Trait("UnitTest", "Computation")]
    [InlineData("\"Test1\"", "Test1")]
    [InlineData("\"Text2 with a \\\"quote\\\" into\"", "Text2 with a \"quote\" into")]
    [InlineData("\"Text3 with an end \\\"quote\\\"\"", "Text3 with an end \"quote\"")]
    [InlineData("\"\\\"Start quote\\\" for text4\"", "\"Start quote\" for text4")]
    public void ShouldBeCleanedContent(string input, string expected)
    {
        _testOutputHelper.WriteLine("{0} => {1}", input, expected);
        input.CleanedContent().Should().Be(expected);
    }
}