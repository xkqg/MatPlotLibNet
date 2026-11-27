// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.MathText;

namespace MatPlotLibNet.Tests.Rendering.MathText;

public sealed class OperatorLimitsTests
{
    [Theory]
    [InlineData(@"$\sum$", "\u2211")] // Sum_ParsesAsLargeOperator
    [InlineData(@"$\int$", "\u222B")] // Int_ParsesAsLargeOperator
    [InlineData(@"$\prod$", "\u220F")] // Prod_ParsesAsLargeOperator
    [InlineData(@"$\lim$", "lim")] // Lim_ParsesAsLargeOperator
    public void AnOperator_ParsesAsALargeOperator(string source, string symbol)
    {
        var rt = MathTextParser.Parse(source);
        Assert.Contains(rt.Spans, s => s.Kind == TextSpanKind.LargeOperator && s.Text == symbol);
    }

    private static void Has(RichText rt, TextSpanKind kind, string? text = null) =>
        Assert.Contains(rt.Spans, s => s.Kind == kind && (text is null || s.Text == text));

    [Theory]
    [InlineData(@"$\sum_{i=0}$", null)]
    [InlineData(@"$\sum_{i=0}^{n}$", "n")]
    public void Sum_WithLimits_EmitsOperatorSubscript_AndTheSuperscriptItWasGiven(string math, string? superscript)
    {
        var rt = MathTextParser.Parse(math);
        Has(rt, TextSpanKind.LargeOperator);
        Has(rt, TextSpanKind.OperatorSubscript, "i = 0");
        if (superscript is not null) Has(rt, TextSpanKind.OperatorSuperscript, superscript);
    }

    /// <summary>An integral's scripts stand beside it, not above and below: matplotlib 3.11.2 sets the scripts of its
    /// <c>Parser._dropsub_symbols</c> (\int \oint \iint \oiint \iiint \oiiint \iiiint) as scripts, and limits only on its
    /// <c>Parser._overunder_symbols</c> (\sum \prod \coprod and the big operators).</summary>
    [Fact]
    public void Int_TakesItsScriptsBesideIt()
    {
        var rt = MathTextParser.Parse(@"$\int_a^b$");
        Has(rt, TextSpanKind.LargeOperator);
        Has(rt, TextSpanKind.Subscript, "a");
        Has(rt, TextSpanKind.Superscript, "b");
    }

    [Fact]
    public void Lim_WithSubscript_Works()
    {
        var rt = MathTextParser.Parse(@"$\lim_{x \to 0}$");
        Has(rt, TextSpanKind.LargeOperator, "lim");
        Has(rt, TextSpanKind.OperatorSubscript);
    }

    [Fact]
    public void RegularSubscript_NotAffected()
    {
        var rt = MathTextParser.Parse(@"$x_i$");
        Has(rt, TextSpanKind.Subscript, "i");
        Assert.DoesNotContain(rt.Spans, s => s.Kind == TextSpanKind.OperatorSubscript);
    }

    [Fact]
    public void LargeOperator_HasIncreasedScale()
    {
        var rt = MathTextParser.Parse(@"$\sum$");
        var op = rt.Spans.First(s => s.Kind == TextSpanKind.LargeOperator);
        Assert.True(op.FontSizeScale > 1.0);
    }

    [Fact]
    public void NewSymbols_Defined()
    {
        Assert.Contains(MathTextParser.Parse(@"$\iint$").Spans, s => s.Text == "\u222C");
        Assert.Contains(MathTextParser.Parse(@"$\iiint$").Spans, s => s.Text == "\u222D");
        Assert.Contains(MathTextParser.Parse(@"$\oint$").Spans, s => s.Text == "\u222E");
    }
}
