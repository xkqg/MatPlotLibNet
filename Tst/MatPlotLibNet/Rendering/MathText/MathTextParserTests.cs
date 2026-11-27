// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.MathText;

namespace MatPlotLibNet.Tests.Rendering.MathText;

/// <summary>Verifies <see cref="MathTextParser"/> tokenization: plain text, Greek substitution,
/// super/subscript, math symbols, and mixed mode.</summary>
public class MathTextParserTests
{
    // --- Plain text (no $ delimiters) ---

    /// <summary>The first span of what the parser reads from <paramref name="math"/> that <paramref name="which"/> picks, and its text.</summary>
    private static TextSpan FirstSpan(string math, Func<TextSpan, bool> which)
    {
        var span = MathTextParser.Parse(math).Spans.FirstOrDefault(which);
        Assert.NotNull(span);
        return span!;
    }

    private static void AssertFirstSpanTextIs(string math, Func<TextSpan, bool> which, string text) => Assert.Equal(text, FirstSpan(math, which).Text);

    private static void AssertFirstSpanTextHas(string math, Func<TextSpan, bool> which, string part) => Assert.Contains(part, FirstSpan(math, which).Text);

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
        Assert.Empty(MathTextParser.Parse(null!).Spans);
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

    [Theory]
    [InlineData("$\\alpha$", "\u03B1")] // Parse_GreekAlpha_ReturnsUnicode
    [InlineData("$\\omega$", "\u03C9")] // Parse_GreekOmega_ReturnsUnicode
    [InlineData("$\\Gamma$", "\u0393")] // Parse_GreekUppercaseGamma_ReturnsUnicode
    [InlineData("$\\Delta$", "\u0394")] // Parse_GreekUppercaseDelta_ReturnsUnicode
    [InlineData("$\\pm$", "\u00B1")] // Parse_PlusMinus_ReturnsUnicode
    [InlineData("$\\infty$", "\u221E")] // Parse_Infty_ReturnsUnicode
    [InlineData("$\\degree$", "\u00B0")] // Parse_Degree_ReturnsUnicode
    [InlineData("$\\times$", "\u00D7")] // Parse_Times_ReturnsUnicode
    [InlineData(@"$\Rightarrow$", "\u2009\u21D2\u2009")] // Parse_Rightarrow_ReturnsUnicode
    [InlineData(@"$\otimes$", "\u2297")] // Parse_Otimes_ReturnsUnicode
    [InlineData(@"$\emptyset$", "\u2205")] // Parse_Emptyset_ReturnsUnicode
    public void Parse_NamesTheUnicodeCharacterItStandsFor(string source, string character)
    {
        var result = MathTextParser.Parse(source);
        Assert.Contains(result.Spans, s => s.Text == character);
    }

    [Theory]
    [InlineData("$R^{2}$", TextSpanKind.Superscript, "2")]
    [InlineData("$x^2$", TextSpanKind.Superscript, "2")]
    [InlineData("$x_{i}$", TextSpanKind.Subscript, "i")]
    [InlineData("$x_i$", TextSpanKind.Subscript, "i")]
    [InlineData(@"$\sqrt{x}$", TextSpanKind.Radical, "x")]
    public void Parse_AScriptOrARadical_ProducesItsSpan(string math, TextSpanKind kind, string text) =>
        AssertFirstSpanTextIs(math, s => s.Kind == kind, text);

    [Theory]
    [InlineData(@"$\mathrm{sin}$", FontVariant.Roman, "sin")]
    [InlineData(@"$\mathbf{F}$", FontVariant.Bold, "F")]
    [InlineData(@"$x = 1 \text{ if } y > 0$", FontVariant.Roman, " if ")]
    [InlineData(@"$\mathcal{L}$", FontVariant.Calligraphic, "L")]
    public void Parse_AFontCommand_ProducesItsVariantSpan(string math, FontVariant variant, string text) =>
        AssertFirstSpanTextIs(math, s => s.Variant == variant, text);

    // --- Superscript ---

    [Fact]
    public void Parse_Superscript_FontSizeScale_IsLessThanOne()
    {
        var result = MathTextParser.Parse("$x^{2}$");
        var sup = result.Spans.First(s => s.Kind == TextSpanKind.Superscript);
        Assert.True(sup.FontSizeScale < 1.0);
    }

    // --- Subscript ---

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

    /// <summary>An argument is read as math, recursively, as every row is (a named fix: a body was read for its symbols only, so
    /// <c>\mathrm{sin}</c> inside a numerator was written as <c>\mathrmsin</c>).</summary>
    [Fact]
    public void Parse_ACommandInsideAnArgument_IsReadAsMath()
    {
        var spans = MathTextParser.Parse(@"$\frac{\mathrm{sin}(x)}{\sqrt{2}}$").Spans;

        Assert.Contains(spans, s => s.Kind == TextSpanKind.FractionNumerator && s.Text == "sin" && s.Variant == FontVariant.Roman);
        Assert.Contains(spans, s => s.Kind == TextSpanKind.Radical && s.Text == "2");
        Assert.DoesNotContain(spans, s => s.Text.Contains('\\'));
    }

    // --- v1.3.0: Square roots ---

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
        AssertFirstSpanTextHas(@"$\sqrt{x^2+y^2}$", s => s.Kind == TextSpanKind.Radical, "x");
    }

    // --- v1.3.0: Accents ---

    [Theory]
    [InlineData(@"$\hat{x}$", 'x', "\u0302")] // combining circumflex
    [InlineData(@"$\vec{v}$", 'v', "\u20D7")] // combining right arrow above
    public void Parse_AnAccentOverALetter_ProducesAccentSpan(string source, char letter, string combining)
    {
        var result = MathTextParser.Parse(source);
        var accent = result.Spans.FirstOrDefault(s => s.Kind == TextSpanKind.Accent);
        Assert.NotNull(accent);
        Assert.Equal(letter, accent!.Text[0]);
        Assert.True(accent.Text.Contains(combining, StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_Bar_ProducesAccentSpan()
    {
        var result = MathTextParser.Parse(@"$\bar{y}$");
        var accent = result.Spans.FirstOrDefault(s => s.Kind == TextSpanKind.Accent);
        Assert.NotNull(accent);
        Assert.Equal('y', accent!.Text[0]);
    }

    [Theory]
    [InlineData(@"$\tilde{x}$", "\u0303")] // combining tilde
    [InlineData(@"$\dot{x}$", "\u0307")]   // combining dot above
    public void Parse_AnAccentOverAMark_ProducesAccentSpan(string source, string combining)
    {
        var result = MathTextParser.Parse(source);
        var accent = result.Spans.FirstOrDefault(s => s.Kind == TextSpanKind.Accent);
        Assert.NotNull(accent);
        Assert.True(accent!.Text.Contains(combining, StringComparison.Ordinal));
    }

    // --- v1.3.0: Font variants ---

    // --- v1.3.0: Spacing ---

    [Theory]
    [InlineData(@"$a\quad b$", "\u2003")] // Parse_Quad_InsertsEmSpace
    [InlineData(@"$a\,b$", "\u2009")] // Parse_ThinSpace_InsertsUnicodeSpace
    [InlineData(@"$a\:b$", "\u2005")] // Parse_MediumSpace_InsertsUnicodeSpace
    [InlineData(@"$a\;b$", "\u2004")] // Parse_ThickSpace_InsertsUnicodeSpace
    public void Parse_ASpaceCommand_InsertsItsUnicodeSpace(string source, string space)
    {
        var result = MathTextParser.Parse(source);
        var allText = string.Concat(result.Spans.Select(s => s.Text));
        Assert.Contains(space, allText); // em space
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

    // --- v1.3.0: Scaling delimiters ---

    [Fact]
    public void Parse_LeftRight_Parens_EmitsParens()
    {
        var result = MathTextParser.Parse(@"$\left(x+1\right)$");
        var allText = string.Concat(result.Spans.Select(s => s.Text));
        Assert.Contains("(", allText);
        Assert.Contains(")", allText);
        Assert.Contains("x\u2009+\u20091", allText); // matplotlib's 0.2 em around a binary operator
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

    [Theory]
    [InlineData(@"$\begin{matrix}1\end{matrix}$")] // Parse_BeginEnvWithExplicitEnd_DoesNotThrow
    [InlineData("$x^$")] // Parse_TrailingCaretWithNoContent_EmitsEmptySuperscript
    [InlineData(@"$\end x$")] // Parse_EndWithoutBraces_DoesNotThrow
    [InlineData(@"$\sqrt[3 x$")] // Parse_SqrtWithUnclosedIndex_DoesNotThrow
    public void Parse_AMalformedCommand_DoesNotThrow(string source)
    {
        var rt = MathTextParser.Parse(source);
        Assert.NotNull(rt);
    }

    // ── Wave J.2 — branch close-out ──────────────────────────────────────

    /// <summary>A function name is set upright as the word; it is a large operator (its scripts are limits) only where matplotlib
    /// sets limits: <c>matplotlib._mathtext.Parser._overunder_functions</c> = {lim, liminf, limsup, max, min, sup} (3.11.2, printed
    /// with python). <c>\inf</c>, <c>\log</c> and the trigonometric names take scripts beside them.</summary>
    [Theory]
    [InlineData("lim", TextSpanKind.LargeOperator)]
    [InlineData("liminf", TextSpanKind.LargeOperator)]
    [InlineData("limsup", TextSpanKind.LargeOperator)]
    [InlineData("max", TextSpanKind.LargeOperator)]
    [InlineData("min", TextSpanKind.LargeOperator)]
    [InlineData("sup", TextSpanKind.LargeOperator)]
    [InlineData("inf", TextSpanKind.Normal)]
    [InlineData("log", TextSpanKind.Normal)]
    [InlineData("ln", TextSpanKind.Normal)]
    [InlineData("sin", TextSpanKind.Normal)]
    [InlineData("cos", TextSpanKind.Normal)]
    [InlineData("tan", TextSpanKind.Normal)]
    [InlineData("exp", TextSpanKind.Normal)]
    [InlineData("det", TextSpanKind.Normal)]
    public void Parse_AFunctionName_IsTheWordUpright_ALargeOperatorWhereItsScriptsAreLimits(string name, TextSpanKind kind)
    {
        var spans = MathTextParser.Parse($@"$\{name}$").Spans;

        Assert.Equal((name, kind, FontVariant.Roman), (spans[0].Text, spans[0].Kind, spans[0].Variant));
        Assert.Equal("\u2006", spans[1].Text); // the thin space after a function name (Parser.operatorname)
    }

    /// <summary>Large-symbol operators (\int, \sum, \prod) — hit the <c>cmd is "int" or "sum" or "prod" …</c>
    /// pattern match (each alternative is a separate branch).</summary>
    [Theory]
    [InlineData("int")]
    [InlineData("sum")]
    [InlineData("prod")]
    [InlineData("iint")]
    [InlineData("iiint")]
    [InlineData("oint")]
    public void Parse_LargeSymbolOperator_ProducesLargeOperatorSpan(string op)
    {
        var result = MathTextParser.Parse($@"$\{op}$");
        Assert.Contains(result.Spans, s => s.Kind == TextSpanKind.LargeOperator);
    }

    /// <summary>ContainsMath(null) — hits the <c>if (text is null) return false</c>
    /// true arm.</summary>
    [Fact]
    public void ContainsMath_Null_ReturnsFalse()
    {
        Assert.False(MathTextParser.ContainsMath(null!));
    }

    /// <summary>A space typed in math and a brace that closes no group are not drawn: <c>$a b$</c> and <c>$a}b$</c> read as <c>$ab$</c>, as
    /// TeX's math mode ignores the one and matplotlib's parser drops the other.</summary>
    [Theory]
    [InlineData("$a b$")]
    [InlineData("$a}b$")]
    public void Parse_ASpaceOrAStrayBraceInMath_IsNotDrawn(string source)
    {
        static string Text(string math) => string.Concat(MathTextParser.Parse(math).Spans.Select(span => span.Text));

        Assert.Equal(Text("$ab$"), Text(source));
    }
}
