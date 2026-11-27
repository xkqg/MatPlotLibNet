// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Rendering.MathText;

namespace MatPlotLibNet.Tests.Rendering.MathText;

/// <summary>The spaces of math as matplotlib 3.11.2 sets them (<c>matplotlib._mathtext.Parser</c>): a space typed in math is no
/// space; a binary operator, a relation and an arrow (<c>_spaced_symbols</c>) stand between two spaces of 0.2 em
/// (<c>Parser.symbol</c>: <c>_make_space(0.2)</c>), a binary operator none at the start or after an opening brace, an opening
/// delimiter or a relation; punctuation (<c>_punctuation_symbols</c>) has 0.2 em after it, but a decimal point between digits and a
/// comma alone in braces none; a function name has a thin space (<c>\,</c>, 0.16667 em) after it unless a delimiter or a script
/// follows (<c>Parser.operatorname</c>), and its scripts take no space around their symbols and a thin space after them.
/// Each row's comment is matplotlib's glyph list (character, x in px at 12 pt, 72 dpi, DejaVu Sans) printed with python; a 0.2 em
/// space is U+2009, a thin space U+2006.</summary>
public class MathSpacingTests
{
    [Theory]
    // '$ab$' 13.0 [a 0.0, b 6.13] and '$a b$' 13.0 [a 0.0, b 6.13]: the typed space is dropped
    [InlineData("$a b$", "ab")]
    [InlineData(@"$\alpha x$", "U+03B1x")]
    // '$a + b$' 25.0 [a 0.0, + 8.08, b 18.4]: 'a' ends at 6.13, 1.95 px (0.2 em) on each side of '+'
    [InlineData("$a + b$", "aU+2009+U+2009b")]
    [InlineData("$a+b$", "aU+2009+U+2009b")]
    // '$a-b$' [a 0.0, U+2212 8.08, b 18.4]: the hyphen is a binary operator
    [InlineData("$a-b$", "aU+2009-U+2009b")]
    // '$-1$' [U+2212 0.0, 1 8.38]: no space at the start; '$a=-1$' [a, = 8.08, U+2212 18.4, 1 26.78]: none after a relation
    [InlineData("$-1$", "-1")]
    [InlineData("$a=-1$", "aU+2009=U+2009-1")]
    // '$\pm 1$' [U+00B1 0.0, 1 8.38]; '$a \pm b$' [a, U+00B1 8.08, b 18.4]
    [InlineData(@"$\pm 1$", "U+00B11")]
    [InlineData(@"$a \pm b$", "aU+2009U+00B1U+2009b")]
    // '$x \in \mathbb{R}$' [x, U+2208 7.87, U+211D 18.53]: a relation
    [InlineData(@"$x \in y$", "xU+2009U+2208U+2009y")]
    // '$x^{a+b}$' [x, a 6.36, + 12.02, b 19.25]: spaced inside a script too
    [InlineData("$x^{a+b}$", "x ; aU+2009+U+2009b")]
    // '$10^{-3}$': no space after an opening brace
    [InlineData("$10^{-3}$", "10 ; -3")]
    // '$a,b$' [a, ',' 6.13, b 11.25]; '$1.5$' [1, . 6.36, 5 9.54]; '$a.b$' [a, . 6.13, b 11.25]; '$x^{,}$' [x, ',' 6.36]
    [InlineData("$a,b$", "a,U+2009b")]
    [InlineData("$1.5$", "1.5")]
    [InlineData("$a.b$", "a.U+2009b")]
    [InlineData("$x^{,}$", "x ; ,")]
    // '$\sin x$' [s, i, n 7.99, x 15.95] and '$\sin\alpha$' [... U+03B1 15.95]: a thin space after the name
    [InlineData(@"$\sin x$", "sin ; U+2006x")]
    [InlineData(@"$\sin\alpha$", "sin ; U+2006U+03B1")]
    // '$\sin(x)$' [s, i, n 7.99, ( 14.33 ...]: no space before a delimiter
    [InlineData(@"$\sin(x)$", "sin ; (x)")]
    // '$\log_{a+b} x$' [l, o, g, a 15.34, + 19.63, b 25.49, x 31.83]: no space in a function's script, a thin space after it
    [InlineData(@"$\log_{a+b} x$", "log ; a+b ; U+2006x")]
    // '$\lim_{x \to 0} f$' [l, i, m, x 0.0, U+2192 4.14, 0 10.01, f 16.92]
    [InlineData(@"$\lim_{x \to 0} f$", "lim ; xU+21920 ; U+2006f")]
    // '$\text{a b}$' [a 0.0, ' ' 6.13, b 9.31]: text keeps its spaces; '$\text{a \alpha b}$' [a, ' ', a l p h a, ' ', b]
    [InlineData(@"$\text{a b}$", "a b")]
    [InlineData(@"$\text{a \alpha b}$", "a alpha b")]
    // '$a~b$' and '$a\ b$' [a 0.0, b 9.37]: a third of an em
    [InlineData("$a~b$", "aU+2004b")]
    // the text outside math keeps its spaces
    [InlineData("$a$ and $b$", "a ;  and  ; b")]
    public void Parse_TheSpacesOfMath_AreMatplotlibs(string label, string expected)
    {
        Assert.Equal(expected, Describe(MathTextParser.Parse(label)));
    }

    /// <summary>Every span's text, joined by " ; ", a character outside ASCII written as <c>U+XXXX</c>.</summary>
    private static string Describe(RichText text) => string.Join(" ; ", text.Spans.Select(span => string.Concat(span.Text.Select(c =>
        c < 128 ? c.ToString() : string.Create(CultureInfo.InvariantCulture, $"U+{(int)c:X4}")))));
}
