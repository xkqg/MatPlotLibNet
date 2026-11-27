// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Rendering.MathText;

namespace MatPlotLibNet.Tests.Rendering.MathText;

/// <summary>Holds every span <see cref="MathTextParser.Parse"/> makes for a corpus of labels that reaches each command
/// family and each edge of the reader (a group never closed, a command at the end of the text, a backslash before a
/// character that names nothing). The expected strings were read off the parser as it stood before its commands moved
/// into a table, so the table is held to the same spans; the reading of an argument without braces, and of a backslash before
/// a character TeX escapes, follows TeX: one token, spaces before it skipped, a closing <c>$</c> never taken.</summary>
public class MathTextParserCorpusTests
{
    [Theory]
    [InlineData("plain", "plain|Normal|1|Default")]
    [InlineData("Revenue ($)", "Revenue (|Normal|1|Default ; )|Normal|1|Default")]
    // matplotlib's Parser._space_widths: \, 0.16667 em (U+2006, a sixth), \: 0.22222 (U+205F, four eighteenths), \; 0.27778
    // (U+2005, a quarter, the nearest), \! -0.16667 (nothing)
    [InlineData(@"$\,x\!y\:z\;w$", "U+2006xyU+205FzU+2005w|Normal|1|Default")]
    // a space typed in math is no space (matplotlib drops it)
    [InlineData(@"$a\quad b\qqquad c$", @"aU+2003b\qqquadc|Normal|1|Default")]
    [InlineData(@"$a\qquad b$", "aU+2003U+2003b|Normal|1|Default")]
    [InlineData(@"$\sqrt[3]{x}$", "3|Superscript|0.48999999999999994|Default ; x|Radical|1|Default")]
    [InlineData(@"$\sqrt[3$", "3|Superscript|0.48999999999999994|Default")]
    [InlineData(@"$\sqrt$", "")]
    [InlineData(@"$\sqrt", "")]
    [InlineData(@"$\left($", "(|Normal|1|Default")]
    [InlineData(@"$\left$", "")]
    [InlineData(@"$\right$", "")]
    [InlineData(@"$\left[ x \right]$", "[x]|Normal|1|Default")]
    [InlineData(@"$\begin{pmatrix} a & \alpha \\ c & d \end{pmatrix} = M$", "pmatrix|MatrixStart|1|Default ; a|MatrixCell|1|Default ; |MatrixCellSeparator|1|Default ; U+03B1|MatrixCell|1|Default ; |MatrixCellSeparator|1|Default ; |MatrixRowSeparator|1|Default ; c|MatrixCell|1|Default ; |MatrixCellSeparator|1|Default ; d|MatrixCell|1|Default ; |MatrixCellSeparator|1|Default ; |MatrixRowSeparator|1|Default ; pmatrix|MatrixEnd|1|Default ; U+2009=U+2009M|Normal|1|Default")]
    [InlineData(@"$\begin{x}a$", "x|MatrixStart|1|Default ; a|MatrixCell|1|Default ; |MatrixCellSeparator|1|Default ; |MatrixRowSeparator|1|Default ; x|MatrixEnd|1|Default")]
    [InlineData(@"$\end$", "")]
    [InlineData(@"$\end$ a_b", " a_b|Normal|1|Default")]
    [InlineData(@"$\end{x} y$", "y|Normal|1|Default")]
    // an integral's scripts stand beside it (matplotlib's Parser._dropsub_symbols), a sum's are its limits (_overunder_symbols)
    [InlineData(@"$\int_0^1 x$", "U+222B|LargeOperator|1.4|Default ; 0|Subscript|0.7|Default ; 1|Superscript|0.7|Default ; x|Normal|1|Default")]
    // a relation in a limit stands between matplotlib's spaces (U+2009, 0.2 em)
    [InlineData(@"$\sum_{i=1}^{n} a_i$", "U+2211|LargeOperator|1.4|Default ; iU+2009=U+20091|OperatorSubscript|0.7|Default ; n|OperatorSuperscript|0.7|Default ; a|Normal|1|Default ; i|Subscript|0.7|Default")]
    // the scripts of a function name take no space around their symbols, and a thin space (U+2006) after them
    [InlineData(@"$\lim_{x \to 0} f$", "lim|LargeOperator|1|Roman ; xU+21920|OperatorSubscript|0.7|Default ; U+2006f|Normal|1|Default")]
    [InlineData(@"$\max x$", "max|LargeOperator|1|Roman ; U+2006x|Normal|1|Default")]
    [InlineData(@"$\hat{\alpha}$", "U+03B1U+0302|Accent|1|Default")]
    [InlineData(@"$\dot x$", "xU+0307|Accent|1|Default")]
    [InlineData(@"$\mathbf{\beta x}$", "U+03B2x|Normal|1|Bold")]
    [InlineData(@"$\mathbb R$", "R|Normal|1|BlackboardBold")]
    [InlineData(@"$\frac{a$", "a|FractionNumerator|0.7|Default")]
    [InlineData(@"$\frac{\pi}{2}$", "U+03C0|FractionNumerator|0.7|Default ; 2|FractionDenominator|0.7|Default")]
    [InlineData(@"$\{x$", "{x|Normal|1|Default")]
    [InlineData(@"$x^{\{\}}$", "x|Normal|1|Default ; {}|Superscript|0.7|Default")]
    [InlineData(@"$\frac\alpha\beta$", "U+03B1|FractionNumerator|0.7|Default ; U+03B2|FractionDenominator|0.7|Default")]
    [InlineData("$a_ b$", "a|Normal|1|Default ; b|Subscript|0.7|Default")]
    [InlineData(@"$\unknown + 1$", @"\unknownU+2009+U+20091|Normal|1|Default")]
    [InlineData("$a^$", "a|Normal|1|Default")]
    [InlineData("$a^", "a|Normal|1|Default")]
    [InlineData(@"$a_\alpha$", "a|Normal|1|Default ; U+03B1|Subscript|0.7|Default")]
    [InlineData(@"$x^{\infty}_{\nope}$", @"x|Normal|1|Default ; \nope|Subscript|0.7|Default ; U+221E|Superscript|0.7|Default")]
    [InlineData(@"price $5 and $\alpha$", @"price |Normal|1|Default ; 5and|Normal|1|Default ; \alpha|Normal|1|Default")]
    [InlineData(@"$\$", "$|Normal|1|Default")]
    [InlineData(@"$\$5$ net", "$5|Normal|1|Default ;  net|Normal|1|Default")]
    [InlineData(@"$\", @"\|Normal|1|Default")]
    // \times after \pm: matplotlib reads the character written before it ('m'), so it is spaced
    [InlineData(@"$\pm\times$ tail", "U+00B1U+2009U+00D7U+2009|Normal|1|Default ;  tail|Normal|1|Default")]
    public void Parse_TheCorpus_KeepsItsSpans(string label, string expected)
    {
        Assert.Equal(expected, Describe(MathTextParser.Parse(label)));
    }

    /// <summary>Every span as <c>text|kind|scale|variant</c>, a character outside ASCII written as <c>U+XXXX</c> so the
    /// expected strings read the same in any editor.</summary>
    private static string Describe(RichText text) => string.Join(" ; ", text.Spans.Select(span =>
        string.Create(CultureInfo.InvariantCulture, $"{Escaped(span.Text)}|{span.Kind}|{span.FontSizeScale}|{span.Variant}")));

    private static string Escaped(string text) => string.Concat(text.Select(c =>
        c < 128 ? c.ToString() : string.Create(CultureInfo.InvariantCulture, $"U+{(int)c:X4}")));
}
