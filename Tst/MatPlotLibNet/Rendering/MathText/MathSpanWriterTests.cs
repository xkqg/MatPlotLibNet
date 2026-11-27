// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.MathText;

namespace MatPlotLibNet.Tests.Rendering.MathText;

/// <summary>The public flat view of a label (<see cref="RichText"/>) is written from its tree (<see cref="MathSpanWriter"/>): the one place a
/// kind of span is written. Each construction of the parse is marked by its kind.</summary>
public sealed class MathSpanWriterTests
{
    [Fact]
    public void AdjacentTextOfOneKindSizeAndSwitch_IsOneSpan()
    {
        var spans = MathTextParser.Parse(@"$a+b\,c$").Spans;

        Assert.Equal("a + b c", Assert.Single(spans).Text);
    }

    [Fact]
    public void TextOnEitherSideOfTheDollarSigns_IsTwoSpans()
    {
        var spans = MathTextParser.Parse("a $b$").Spans;

        Assert.Equal([true, false], spans.Select(s => s.OutsideMath));
    }

    [Fact]
    public void TheFlatViewOfEachConstruction_IsMarkedByItsKind()
    {
        static string Kinds(string label) => string.Join(",", MathTextParser.Parse(label).Spans.Select(s => s.Kind));

        Assert.Equal("FractionNumerator,FractionDenominator,Accent", Kinds(@"$\hat{\frac{b}{c}}$"));
        Assert.Equal("Radical", Kinds(@"$\sqrt{x}$"));
        Assert.Equal("FractionNumerator,FractionDenominator", Kinds(@"$\frac{a}{b}$"));
        Assert.Equal("Normal,OperatorSuperscript", Kinds(@"$\overset{a}{x}$"));
        Assert.Equal("Normal,OperatorSubscript", Kinds(@"$\underset{a}{x}$"));
        Assert.Equal("Normal", Kinds(@"$\left(x\right)\phantom{y}z$"));
        Assert.Equal("(xz", MathTextParser.Parse(@"$\left(x\right.\phantom{y}z$").PlainText);
    }

    [Fact]
    public void ASpaceOfNoWidth_WritesNothing_AndAStackWritesItsRows()
    {
        Assert.Equal("ab", MathTextParser.Parse(@"$a\!b$").PlainText);
        Assert.Equal("abc", MathTextParser.Parse(@"$\substack{a \\ b \\ c}$").PlainText);
        Assert.Equal("ab", MathTextParser.Parse(@"$\underline{a}\overline{b}$").PlainText);
        Assert.Equal("xy", MathTextParser.Parse(@"$\llap{x}\rlap{y}$").PlainText);
        Assert.Equal("x′", MathTextParser.Parse("$x'$").PlainText);
        Assert.Equal("∫", MathTextParser.Parse(@"$\int$").PlainText);
    }

    [Fact]
    public void TheTextOfABox_IsItsSpansOneAfterTheOther()
    {
        Assert.Equal("ab", MathSpanWriter.TextOf(TextLabel.Of("$a^b$").Parsed(null).Tree!));
    }
}
