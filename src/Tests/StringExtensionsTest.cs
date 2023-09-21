﻿using System.Text;
using FluentAssertions;
using Toolbox.Domain;

namespace Tests;

public class StringExtensionsTest
{
    public StringExtensionsTest()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    [Theory]
    [InlineData("Simple", "simple")]
    [InlineData("Au matin", "au_matin")]
    [InlineData("A la dérive", "a_la_derive")]
    [InlineData("Comme toi, aile", "comme_toi_aile")]
    [InlineData("Mais où vais-je ?", "mais_ou_vais_je")]
    [InlineData("L'air - créé", "l_air_cree")]
    [InlineData("Sais-tu l'amour...", "sais_tu_l_amour")]
    [InlineData("Marcher : neige", "marcher_neige")]
    public void ShouldBeUnaccentedCleaned(string input, string expected)
    {
        input.UnaccentedCleaned().Should().Be(expected);
    }
}