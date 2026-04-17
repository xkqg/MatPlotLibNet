// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.MathText;

namespace MatPlotLibNet.Tests.Rendering.MathText;

public sealed class OperatorLimitsTests
{
    [Fact]
    public void Sum_ParsesAsLargeOperator()
    {
        var rt = MathTextParser.Parse(@"$\sum$");
        Assert.Contains(rt.Spans, s => s.Kind == TextSpanKind.LargeOperator && s.Text == "\u2211");
    }

    [Fact]
    public void Int_ParsesAsLargeOperator()
    {
        var rt = MathTextParser.Parse(@"$\int$");
        Assert.Contains(rt.Spans, s => s.Kind == TextSpanKind.LargeOperator && s.Text == "\u222B");
    }

    [Fact]
    public void Prod_ParsesAsLargeOperator()
    {
        var rt = MathTextParser.Parse(@"$\prod$");
        Assert.Contains(rt.Spans, s => s.Kind == TextSpanKind.LargeOperator && s.Text == "\u220F");
    }

    [Fact]
    public void Lim_ParsesAsLargeOperator()
    {
        var rt = MathTextParser.Parse(@"$\lim$");
        Assert.Contains(rt.Spans, s => s.Kind == TextSpanKind.LargeOperator && s.Text == "lim");
    }

    [Fact]
    public void Sum_WithSubscript_EmitsOperatorSubscript()
    {
        var rt = MathTextParser.Parse(@"$\sum_{i=0}$");
        Assert.Contains(rt.Spans, s => s.Kind == TextSpanKind.LargeOperator);
        Assert.Contains(rt.Spans, s => s.Kind == TextSpanKind.OperatorSubscript && s.Text == "i=0");
    }

    [Fact]
    public void Sum_WithBothLimits_EmitsOperatorSubAndSuper()
    {
        var rt = MathTextParser.Parse(@"$\sum_{i=0}^{n}$");
        Assert.Contains(rt.Spans, s => s.Kind == TextSpanKind.LargeOperator);
        Assert.Contains(rt.Spans, s => s.Kind == TextSpanKind.OperatorSubscript && s.Text == "i=0");
        Assert.Contains(rt.Spans, s => s.Kind == TextSpanKind.OperatorSuperscript && s.Text == "n");
    }

    [Fact]
    public void Int_WithLimits_Works()
    {
        var rt = MathTextParser.Parse(@"$\int_a^b$");
        Assert.Contains(rt.Spans, s => s.Kind == TextSpanKind.LargeOperator);
        Assert.Contains(rt.Spans, s => s.Kind == TextSpanKind.OperatorSubscript && s.Text == "a");
        Assert.Contains(rt.Spans, s => s.Kind == TextSpanKind.OperatorSuperscript && s.Text == "b");
    }

    [Fact]
    public void Lim_WithSubscript_Works()
    {
        var rt = MathTextParser.Parse(@"$\lim_{x \to 0}$");
        Assert.Contains(rt.Spans, s => s.Kind == TextSpanKind.LargeOperator && s.Text == "lim");
        Assert.Contains(rt.Spans, s => s.Kind == TextSpanKind.OperatorSubscript);
    }

    [Fact]
    public void RegularSubscript_NotAffected()
    {
        var rt = MathTextParser.Parse(@"$x_i$");
        Assert.Contains(rt.Spans, s => s.Kind == TextSpanKind.Subscript && s.Text == "i");
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
