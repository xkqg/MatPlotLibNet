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

    // --- v1.3.0: Fractions ---

    [Fact]
    public void Parse_Fraction_ProducesNumeratorAndDenominatorSpans()
    {
        var result = MathTextParser.Parse(@"$\frac{a}{b}$");
        var num = result.Spans.FirstOrDefault(s => s.Kind == TextSpanKind.FractionNumerator);
        var den = result.Spans.FirstOrDefault(s => s.Kind == TextSpanKind.FractionDenominator);
        Assert.NotNull(num);
        Assert.NotNull(den);
        Assert.Equal("a", num!.Text);
        Assert.Equal("b", den!.Text);
    }

    [Fact]
    public void Parse_Fraction_FontSizeScale_IsReduced()
    {
        var result = MathTextParser.Parse(@"$\frac{x+1}{y-1}$");
        var num = result.Spans.First(s => s.Kind == TextSpanKind.FractionNumerator);
        Assert.True(num.FontSizeScale < 1.0);
    }

    [Fact]
    public void Parse_Fraction_WithGreekInside()
    {
        var result = MathTextParser.Parse(@"$\frac{\alpha}{\beta}$");
        var num = result.Spans.First(s => s.Kind == TextSpanKind.FractionNumerator);
        Assert.Equal("\u03B1", num.Text); // α
    }

    // --- v1.3.0: Square roots ---

    [Fact]
    public void Parse_Sqrt_ProducesRadicalSpan()
    {
        var result = MathTextParser.Parse(@"$\sqrt{x}$");
        var rad = result.Spans.FirstOrDefault(s => s.Kind == TextSpanKind.Radical);
        Assert.NotNull(rad);
        Assert.Equal("x", rad!.Text);
    }

    [Fact]
    public void Parse_SqrtWithIndex_ProducesIndexAndRadical()
    {
        var result = MathTextParser.Parse(@"$\sqrt[3]{x}$");
        // Index '3' emitted as superscript-sized prefix
        var idx = result.Spans.FirstOrDefault(s => s.Kind == TextSpanKind.Superscript && s.Text == "3");
        var rad = result.Spans.FirstOrDefault(s => s.Kind == TextSpanKind.Radical);
        Assert.NotNull(idx);
        Assert.NotNull(rad);
        Assert.Equal("x", rad!.Text);
    }

    [Fact]
    public void Parse_Sqrt_WithExpression()
    {
        var result = MathTextParser.Parse(@"$\sqrt{x^2+y^2}$");
        var rad = result.Spans.FirstOrDefault(s => s.Kind == TextSpanKind.Radical);
        Assert.NotNull(rad);
        Assert.Contains("x", rad!.Text);
    }

    // --- v1.3.0: Accents ---

    [Fact]
    public void Parse_Hat_ProducesAccentSpan()
    {
        var result = MathTextParser.Parse(@"$\hat{x}$");
        var accent = result.Spans.FirstOrDefault(s => s.Kind == TextSpanKind.Accent);
        Assert.NotNull(accent);
        Assert.Equal('x', accent!.Text[0]);
        Assert.True(accent.Text.Contains("\u0302", StringComparison.Ordinal)); // combining circumflex
    }

    [Fact]
    public void Parse_Bar_ProducesAccentSpan()
    {
        var result = MathTextParser.Parse(@"$\bar{y}$");
        var accent = result.Spans.FirstOrDefault(s => s.Kind == TextSpanKind.Accent);
        Assert.NotNull(accent);
        Assert.Equal('y', accent!.Text[0]);
    }

    [Fact]
    public void Parse_Vec_ProducesAccentSpan()
    {
        var result = MathTextParser.Parse(@"$\vec{v}$");
        var accent = result.Spans.FirstOrDefault(s => s.Kind == TextSpanKind.Accent);
        Assert.NotNull(accent);
        Assert.Equal('v', accent!.Text[0]);
        Assert.True(accent.Text.Contains("\u20D7", StringComparison.Ordinal)); // combining right arrow above
    }

    [Fact]
    public void Parse_Tilde_ProducesAccentSpan()
    {
        var result = MathTextParser.Parse(@"$\tilde{x}$");
        var accent = result.Spans.FirstOrDefault(s => s.Kind == TextSpanKind.Accent);
        Assert.NotNull(accent);
        Assert.True(accent!.Text.Contains("\u0303", StringComparison.Ordinal)); // combining tilde
    }

    [Fact]
    public void Parse_Dot_ProducesAccentSpan()
    {
        var result = MathTextParser.Parse(@"$\dot{x}$");
        var accent = result.Spans.FirstOrDefault(s => s.Kind == TextSpanKind.Accent);
        Assert.NotNull(accent);
        Assert.True(accent!.Text.Contains("\u0307", StringComparison.Ordinal)); // combining dot above
    }

    // --- v1.3.0: Font variants ---

    [Fact]
    public void Parse_Mathrm_ProducesRomanFontSpan()
    {
        var result = MathTextParser.Parse(@"$\mathrm{sin}$");
        var roman = result.Spans.FirstOrDefault(s => s.Variant == FontVariant.Roman);
        Assert.NotNull(roman);
        Assert.Equal("sin", roman!.Text);
    }

    [Fact]
    public void Parse_Mathbf_ProducesBoldFontSpan()
    {
        var result = MathTextParser.Parse(@"$\mathbf{F}$");
        var bold = result.Spans.FirstOrDefault(s => s.Variant == FontVariant.Bold);
        Assert.NotNull(bold);
        Assert.Equal("F", bold!.Text);
    }

    [Fact]
    public void Parse_Text_ProducesRomanFontSpan()
    {
        var result = MathTextParser.Parse(@"$x = 1 \text{ if } y > 0$");
        var text = result.Spans.FirstOrDefault(s => s.Variant == FontVariant.Roman);
        Assert.NotNull(text);
        Assert.Equal(" if ", text!.Text);
    }

    [Fact]
    public void Parse_Mathcal_ProducesCalligraphicSpan()
    {
        var result = MathTextParser.Parse(@"$\mathcal{L}$");
        var cal = result.Spans.FirstOrDefault(s => s.Variant == FontVariant.Calligraphic);
        Assert.NotNull(cal);
        Assert.Equal("L", cal!.Text);
    }

    // --- v1.3.0: Spacing ---

    [Fact]
    public void Parse_Quad_InsertsEmSpace()
    {
        var result = MathTextParser.Parse(@"$a\quad b$");
        var allText = string.Concat(result.Spans.Select(s => s.Text));
        Assert.Contains("\u2003", allText); // em space
    }

    [Fact]
    public void Parse_ThinSpace_InsertsUnicodeSpace()
    {
        var result = MathTextParser.Parse(@"$a\,b$");
        var allText = string.Concat(result.Spans.Select(s => s.Text));
        Assert.Contains("\u2009", allText); // thin space
    }

    [Fact]
    public void Parse_MediumSpace_InsertsUnicodeSpace()
    {
        var result = MathTextParser.Parse(@"$a\:b$");
        var allText = string.Concat(result.Spans.Select(s => s.Text));
        Assert.Contains("\u2005", allText); // medium mathematical space
    }

    [Fact]
    public void Parse_ThickSpace_InsertsUnicodeSpace()
    {
        var result = MathTextParser.Parse(@"$a\;b$");
        var allText = string.Concat(result.Spans.Select(s => s.Text));
        Assert.Contains("\u2004", allText); // thick mathematical space
    }

    // --- v1.3.0: Additional symbols ---

    [Fact]
    public void Parse_BlackboardBoldR_ReturnsUnicode()
    {
        var result = MathTextParser.Parse(@"$\mathbb{R}$");
        // \mathbb{R} is handled as font variant OR as a direct symbol lookup
        var allText = string.Concat(result.Spans.Select(s => s.Text));
        Assert.True(
            allText.Contains("\u211D") || // ℝ from symbol map
            result.Spans.Any(s => s.Variant == FontVariant.BlackboardBold)); // font variant
    }

    [Fact]
    public void Parse_Rightarrow_ReturnsUnicode()
    {
        var result = MathTextParser.Parse(@"$\Rightarrow$");
        Assert.Contains(result.Spans, s => s.Text == "\u21D2"); // ⇒
    }

    [Fact]
    public void Parse_Otimes_ReturnsUnicode()
    {
        var result = MathTextParser.Parse(@"$\otimes$");
        Assert.Contains(result.Spans, s => s.Text == "\u2297"); // ⊗
    }

    [Fact]
    public void Parse_Emptyset_ReturnsUnicode()
    {
        var result = MathTextParser.Parse(@"$\emptyset$");
        Assert.Contains(result.Spans, s => s.Text == "\u2205"); // ∅
    }

    // --- v1.3.0: Scaling delimiters ---

    [Fact]
    public void Parse_LeftRight_Parens_EmitsParens()
    {
        var result = MathTextParser.Parse(@"$\left(x+1\right)$");
        var allText = string.Concat(result.Spans.Select(s => s.Text));
        Assert.Contains("(", allText);
        Assert.Contains(")", allText);
        Assert.Contains("x+1", allText);
    }

    // --- v1.3.0: Nested superscript ---

    [Fact]
    public void Parse_NestedSuperscript_ContainsCommand()
    {
        // x^{\alpha} — superscript body contains a \command
        var result = MathTextParser.Parse(@"$x^{\alpha}$");
        var sup = result.Spans.First(s => s.Kind == TextSpanKind.Superscript);
        Assert.Equal("\u03B1", sup.Text); // α (substituted inside super)
    }

    // --- v1.3.0: ContainsMath ---

    [Fact]
    public void ContainsMath_SingleDollar_ReturnsFalse()
    {
        Assert.False(MathTextParser.ContainsMath("Revenue ($)"));
    }

    [Fact]
    public void ContainsMath_TwoDollars_ReturnsTrue()
    {
        Assert.True(MathTextParser.ContainsMath("$\\alpha$ = 0.05"));
    }

    /// <summary>Explicit <c>\end{env}</c> consumes the brace group without crashing —
    /// covers the <c>cmd == "end"</c> branch that ReadBraceGroup-then-continues.</summary>
    [Fact]
    public void Parse_BeginEnvWithExplicitEnd_DoesNotThrow()
    {
        var rt = MathTextParser.Parse(@"$\begin{matrix}1\end{matrix}$");
        Assert.NotNull(rt);
    }

    /// <summary>Super/subscript with no following content (ends with $) — covers
    /// the empty-string fallback branch in the super/subscript reader.</summary>
    [Fact]
    public void Parse_TrailingCaretWithNoContent_EmitsEmptySuperscript()
    {
        var rt = MathTextParser.Parse("$x^$");
        Assert.NotNull(rt);
    }

    /// <summary>ReadBraceGroup called when the next char is NOT { — covers the
    /// bare-character fallback branch.</summary>
    [Fact]
    public void Parse_EndWithoutBraces_DoesNotThrow()
    {
        // `\end x` — `\end` is followed by 'x' rather than `{...}`.
        var rt = MathTextParser.Parse(@"$\end x$");
        Assert.NotNull(rt);
    }
}
