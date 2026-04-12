// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.MathText;

namespace MatPlotLibNet.Tests.Rendering.MathText;

/// <summary>Verifies <see cref="GreekLetters"/> dictionary completeness.</summary>
public class GreekLetterTests
{
    [Theory]
    [InlineData("alpha",   "\u03B1")]
    [InlineData("beta",    "\u03B2")]
    [InlineData("gamma",   "\u03B3")]
    [InlineData("delta",   "\u03B4")]
    [InlineData("epsilon", "\u03B5")]
    [InlineData("zeta",    "\u03B6")]
    [InlineData("eta",     "\u03B7")]
    [InlineData("theta",   "\u03B8")]
    [InlineData("iota",    "\u03B9")]
    [InlineData("kappa",   "\u03BA")]
    [InlineData("lambda",  "\u03BB")]
    [InlineData("mu",      "\u03BC")]
    [InlineData("nu",      "\u03BD")]
    [InlineData("xi",      "\u03BE")]
    [InlineData("pi",      "\u03C0")]
    [InlineData("rho",     "\u03C1")]
    [InlineData("sigma",   "\u03C3")]
    [InlineData("tau",     "\u03C4")]
    [InlineData("upsilon", "\u03C5")]
    [InlineData("phi",     "\u03C6")]
    [InlineData("chi",     "\u03C7")]
    [InlineData("psi",     "\u03C8")]
    [InlineData("omega",   "\u03C9")]
    public void LookupLowercase_ReturnsCorrectUnicode(string name, string expected)
    {
        Assert.Equal(expected, GreekLetters.TryGet(name));
    }

    [Theory]
    [InlineData("Alpha",   "\u0391")]
    [InlineData("Beta",    "\u0392")]
    [InlineData("Gamma",   "\u0393")]
    [InlineData("Delta",   "\u0394")]
    [InlineData("Epsilon", "\u0395")]
    [InlineData("Zeta",    "\u0396")]
    [InlineData("Eta",     "\u0397")]
    [InlineData("Theta",   "\u0398")]
    [InlineData("Iota",    "\u0399")]
    [InlineData("Kappa",   "\u039A")]
    [InlineData("Lambda",  "\u039B")]
    [InlineData("Mu",      "\u039C")]
    [InlineData("Nu",      "\u039D")]
    [InlineData("Xi",      "\u039E")]
    [InlineData("Pi",      "\u03A0")]
    [InlineData("Rho",     "\u03A1")]
    [InlineData("Sigma",   "\u03A3")]
    [InlineData("Tau",     "\u03A4")]
    [InlineData("Upsilon", "\u03A5")]
    [InlineData("Phi",     "\u03A6")]
    [InlineData("Chi",     "\u03A7")]
    [InlineData("Psi",     "\u03A8")]
    [InlineData("Omega",   "\u03A9")]
    public void LookupUppercase_ReturnsCorrectUnicode(string name, string expected)
    {
        Assert.Equal(expected, GreekLetters.TryGet(name));
    }

    [Fact]
    public void Unknown_ReturnsNull()
    {
        Assert.Null(GreekLetters.TryGet("notgreek"));
    }
}
