// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Rendering.MathText;

namespace MatPlotLibNet.Tests.Rendering.MathText;

/// <summary>A brace in math opens or closes a group and is not drawn (only <c>\{</c> and <c>\}</c> are braces); a delimiter after
/// <c>\left</c> or <c>\right</c> is its character (<c>\langle</c> is ⟨, <c>\|</c> is ‖), and <c>.</c> is the delimiter that is not
/// drawn; <c>\|</c> is the symbol ‖ anywhere. Each row's comment is matplotlib 3.11.2's glyph list (character, x in px at 12 pt,
/// 72 dpi), printed with python.</summary>
public class MathGroupTests
{
    [Theory]
    [InlineData("${x}$", "x")]                       // [x 0.0]
    [InlineData("$a{b}c$", "abc")]                   // [a 0.0, b 6.13, c 12.48]
    [InlineData("${}^{14}C$", "14 ; C")]             // [1 0.1, 4 4.55, C 9.28]
    [InlineData("$x^{a{b}}$", "x ; ab")]             // [x 0.0, a 6.36, b 10.65]
    [InlineData(@"$\left\{x\right\}$", "{x}")] // [{ 0.0, x 6.36, } 12.28]
    [InlineData(@"$\left\langle x\right\rangle$", "U+27E8xU+27E9")]
    [InlineData(@"$\left\lfloor x\right\rfloor$", "U+230AxU+230B")]
    [InlineData(@"$\left. x\right|$", "x|")]      // [x 0.0, | 5.92]: '.' draws nothing
    [InlineData(@"$\left\| x\right\|$", "U+2016xU+2016")]
    [InlineData(@"$\left< x\right>$", "<x>")]  // a delimiter takes no space of a relation
    [InlineData(@"$\|x\|$", "U+2016xU+2016")]
    public void Parse_AGroupAndADelimiter_AreReadAsMatplotlibReadsThem(string label, string expected)
    {
        Assert.Equal(expected, Describe(MathTextParser.Parse(label)));
    }

    /// <summary>Every span's text, joined by " ; ", a character outside ASCII written as <c>U+XXXX</c>.</summary>
    private static string Describe(RichText text) => string.Join(" ; ", text.Spans.Select(span => string.Concat(span.Text.Select(c =>
        c < 128 ? c.ToString() : string.Create(CultureInfo.InvariantCulture, $"U+{(int)c:X4}")))));
}
