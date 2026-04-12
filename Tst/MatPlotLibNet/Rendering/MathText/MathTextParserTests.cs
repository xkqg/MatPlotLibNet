// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.MathText;

namespace MatPlotLibNet.Tests.Rendering.MathText;

/// <summary>Verifies <see cref="MathTextParser"/> tokenization: plain text, Greek substitution,
/// super/subscript, math symbols, and mixed mode.</summary>
public class MathTextParserTests
{
    // --- Plain text (no $ delimiters) ---

    [Fact]
    public void Parse_PlainText_SingleNormalSpan()
    {
        var result = MathTextParser.Parse("hello world");
        Assert.Single(result.Spans);
        Assert.Equal("hello world", result.Spans[0].Text);
        Assert.Equal(TextSpanKind.Normal, result.Spans[0].Kind);
    }

    [Fact]
    public void Parse_EmptyString_EmptySpans()
    {
        Assert.Empty(MathTextParser.Parse("").Spans);
    }

    // --- Dollar delimiter switches math mode ---

    [Fact]
    public void Parse_OnlyMath_NoPrefixText()
    {
        var result = MathTextParser.Parse("$\\alpha$");
        Assert.Single(result.Spans);
        Assert.Equal("\u03B1", result.Spans[0].Text); // α
    }

    [Fact]
    public void Parse_MixedText_NormalBeforeAndAfterMath()
    {
        var result = MathTextParser.Parse("Temperature ($\\degree$C)");
        // Should produce: "Temperature (" | "°" | "C)"
        Assert.True(result.Spans.Count >= 2);
        var allText = string.Concat(result.Spans.Select(s => s.Text));
        Assert.Contains("\u00B0", allText); // °
        Assert.Contains("Temperature", allText);
    }

    // --- Greek letters ---

    [Fact]
    public void Parse_GreekAlpha_ReturnsUnicode()
    {
        var result = MathTextParser.Parse("$\\alpha$");
        Assert.Contains(result.Spans, s => s.Text == "\u03B1");
    }

    [Fact]
    public void Parse_GreekOmega_ReturnsUnicode()
    {
        var result = MathTextParser.Parse("$\\omega$");
        Assert.Contains(result.Spans, s => s.Text == "\u03C9");
    }

    [Fact]
    public void Parse_GreekUppercaseGamma_ReturnsUnicode()
    {
        var result = MathTextParser.Parse("$\\Gamma$");
        Assert.Contains(result.Spans, s => s.Text == "\u0393");
    }

    [Fact]
    public void Parse_GreekUppercaseDelta_ReturnsUnicode()
    {
        var result = MathTextParser.Parse("$\\Delta$");
        Assert.Contains(result.Spans, s => s.Text == "\u0394");
    }

    // --- Superscript ---

    [Fact]
    public void Parse_Superscript_BracedContent_ProducesSuperSpan()
    {
        var result = MathTextParser.Parse("$R^{2}$");
        var sup = result.Spans.FirstOrDefault(s => s.Kind == TextSpanKind.Superscript);
        Assert.NotNull(sup);
        Assert.Equal("2", sup!.Text);
    }

    [Fact]
    public void Parse_Superscript_SingleChar_ProducesSuperSpan()
    {
        var result = MathTextParser.Parse("$x^2$");
        var sup = result.Spans.FirstOrDefault(s => s.Kind == TextSpanKind.Superscript);
        Assert.NotNull(sup);
        Assert.Equal("2", sup!.Text);
    }

    [Fact]
    public void Parse_Superscript_FontSizeScale_IsLessThanOne()
    {
        var result = MathTextParser.Parse("$x^{2}$");
        var sup = result.Spans.First(s => s.Kind == TextSpanKind.Superscript);
        Assert.True(sup.FontSizeScale < 1.0);
    }

    // --- Subscript ---

    [Fact]
    public void Parse_Subscript_BracedContent_ProducesSubSpan()
    {
        var result = MathTextParser.Parse("$x_{i}$");
        var sub = result.Spans.FirstOrDefault(s => s.Kind == TextSpanKind.Subscript);
        Assert.NotNull(sub);
        Assert.Equal("i", sub!.Text);
    }

    [Fact]
    public void Parse_Subscript_SingleChar_ProducesSubSpan()
    {
        var result = MathTextParser.Parse("$x_i$");
        var sub = result.Spans.FirstOrDefault(s => s.Kind == TextSpanKind.Subscript);
        Assert.NotNull(sub);
        Assert.Equal("i", sub!.Text);
    }

    // --- Math symbols ---

    [Fact]
    public void Parse_PlusMinus_ReturnsUnicode()
    {
        var result = MathTextParser.Parse("$\\pm$");
        Assert.Contains(result.Spans, s => s.Text == "\u00B1");
    }

    [Fact]
    public void Parse_Infty_ReturnsUnicode()
    {
        var result = MathTextParser.Parse("$\\infty$");
        Assert.Contains(result.Spans, s => s.Text == "\u221E");
    }

    [Fact]
    public void Parse_Degree_ReturnsUnicode()
    {
        var result = MathTextParser.Parse("$\\degree$");
        Assert.Contains(result.Spans, s => s.Text == "\u00B0");
    }

    [Fact]
    public void Parse_Times_ReturnsUnicode()
    {
        var result = MathTextParser.Parse("$\\times$");
        Assert.Contains(result.Spans, s => s.Text == "\u00D7");
    }

    // --- Combined: Greek + superscript ---

    [Fact]
    public void Parse_AlphaSquared_ProducesTwoSpans()
    {
        var result = MathTextParser.Parse("$\\alpha^{2}$");
        Assert.Contains(result.Spans, s => s.Text == "\u03B1");
        Assert.Contains(result.Spans, s => s.Kind == TextSpanKind.Superscript && s.Text == "2");
    }

    // --- Caret/underscore outside math mode are literal ---

    [Fact]
    public void Parse_CaretOutsideMath_IsLiteral()
    {
        var result = MathTextParser.Parse("R^2 = 0.99");
        var allText = string.Concat(result.Spans.Select(s => s.Text));
        Assert.Contains("^", allText);
        Assert.DoesNotContain(result.Spans, s => s.Kind == TextSpanKind.Superscript);
    }
}
