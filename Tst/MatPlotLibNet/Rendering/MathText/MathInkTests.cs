// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.MathText;

/// <summary>The one layout of math (<see cref="MathBox"/>): each construction placed by the rule matplotlib's mathtext (TeX's box model)
/// places it by, on the font set's constants. The runs are measured by a metrics whose glyphs are easy to sum: a character is half the
/// size wide, its ink rises 0.7 of the size, and a letter with a descender reaches 0.2 of the size below the baseline. The Skia suite
/// holds the same layout to matplotlib's own boxes (<c>MathLayoutParityTests</c>).</summary>
public sealed class MathInkTests
{
    private const double Size = 10;

    private static MathConstants C => MathConstants.Library;

    private static double XHeight => C.XHeight * Size;

    private static double Rule => C.RuleThickness * Size;

    internal static MathInk Laid(string label, MathFontSet? set = null, double size = Size) =>
        MathInk.Of(TextLabel.Of(label).Parsed(null).Tree!, new MathStyle(set ?? MathFontSet.Library, new Font { Size = size }, MathMeasure.ByInk(TestGlyphInk.Boxes)));

    private static MathGlyphInk Run(MathInk ink, string text) => ink.Glyphs.Single(g => g.Text == text);

    // ---- runs, spaces, rows ---------------------------------------------------------------------------------------------------

    [Fact]
    public void ARunOfCharacters_IsOneRunOnTheBaseline_AsWideAsItsCharacters()
    {
        var ink = Laid("$ab$");

        var run = Assert.Single(ink.Glyphs);
        Assert.Equal(("ab", 0.0, 0.0, Size), (run.Text, run.X, run.Y, run.Font.Size));
        Assert.Equal(new MathExtent(10, 7, 0), ink.Extent);
        Assert.NotNull(ink.Single);
    }

    [Fact]
    public void ASpaceCommand_IsAKernOfItsWidthInQuads()
    {
        var ink = Laid(@"$a\quad b$");

        Assert.Equal(5 + C.Quad * Size, Run(ink, "b").X, 9);
        Assert.Null(ink.Single);
    }

    [Fact]
    public void ANegativeSpace_TakesRoomAway()
    {
        Assert.Equal(5 - 0.16667 * C.Quad * Size, Run(Laid(@"$a\!b$"), "b").X, 9);
    }

    [Fact]
    public void AGroup_StandsInItsRow_AfterTheRunBeforeIt()
    {
        var ink = Laid("$a{bc}d$");

        Assert.Equal([0.0, 5.0, 15.0], ink.Glyphs.Select(g => g.X));
    }

    [Fact]
    public void AnEmptyLabel_TakesNoRoom()
    {
        Assert.Equal(default, Laid("$$").Extent);
    }

    // ---- scripts --------------------------------------------------------------------------------------------------------------

    /// <summary>A superscript of an upright character: kerned by delta x-heights shrunk with it, raised by at least sup1 x-heights or its
    /// depth and a quarter x-height, then the script space.</summary>
    [Fact]
    public void ASuperscript_IsKernedRaisedAndFollowedByTheScriptSpace()
    {
        var ink = Laid("$x^2$");

        double kern = C.Delta * XHeight * C.ShrinkFactor;
        double up = Math.Max(C.Sup1 * XHeight, 0 + XHeight / 4);
        Assert.Equal(5 + kern, Run(ink, "2").X, 9);
        Assert.Equal(-up, Run(ink, "2").Y, 9);
        Assert.Equal(new MathExtent(5 + kern + 3.5 + C.ScriptSpace * XHeight, 0.7 * 7 + up, 0), ink.Extent);
    }

    [Fact]
    public void ASubscript_IsDroppedByAtLeastSub1XHeights()
    {
        var ink = Laid("$x_g$");

        double down = Math.Max(C.Sub1 * XHeight, 4.9 - XHeight * 4 / 5);
        Assert.Equal(down, Run(ink, "g").Y, 9);
        Assert.Equal(1.4 + down, ink.Extent.Depth, 9);
    }

    /// <summary>Both scripts: the subscript dropped by at least sub2 x-heights, the two kept four rules apart (the subscript goes down,
    /// then both go up until the superscript's foot is at four fifths of the x-height).</summary>
    [Fact]
    public void BothScripts_AreKeptFourRulesApart()
    {
        var ink = Laid("$x_i^2$");

        double up = Math.Max(C.Sup1 * XHeight, XHeight / 4), down = C.Sub2 * XHeight;
        double clearance = 4 * Rule - ((up - 0) - (4.9 - down));
        if (clearance > 0)
        {
            down += clearance;
            clearance = XHeight * 4 / 5 - up;
            if (clearance > 0)
            {
                up += clearance;
                down -= clearance;
            }
        }

        Assert.Equal(-up, Run(ink, "2").Y, 9);
        Assert.Equal(down, Run(ink, "i").Y, 9);
        Assert.Equal(Run(ink, "2").X, Run(ink, "i").X, 9);
    }

    /// <summary>The clearance moves both scripts up when the superscript would stand lower than four fifths of the x-height.</summary>
    [Fact]
    public void BothScripts_RiseTogether_WhenTheSuperscriptStandsTooLow()
    {
        var ink = Laid("$x_{\\frac{a}{b}}^2$");

        Assert.True(Run(ink, "2").Y <= -XHeight * 4 / 5 + 1e-9);
    }

    /// <summary>A nucleus that is no character (a fraction) raises its superscript from its own top less supdrop x-heights of the script.</summary>
    [Fact]
    public void TheScriptsOfABoxThatIsNoCharacter_AreMeasuredFromItsTopAndBottom()
    {
        var plain = Laid("$x^2$");
        var fraction = Laid(@"${\frac{a}{b}}^2$");

        Assert.True(Run(fraction, "2").Y < Run(plain, "2").Y);
        Assert.True(Laid(@"${\frac{a}{b}}_2$").Glyphs[^1].Y > Run(Laid("$x_2$"), "2").Y);
    }

    /// <summary>A leaning nucleus (a letter of DejaVu Sans Oblique) holds its superscript further away and its subscript under it.</summary>
    [Fact]
    public void TheScriptsOfALeaningNucleus_StandFurtherAndCloser()
    {
        var upright = Laid("$x^2_i$", MathFontSet.Library);
        var leaning = Laid("$x^2_i$", MathFontSet.DejaVuSans);

        Assert.True(Run(leaning, "2").X - 5 > Run(upright, "2").X - 5);
        Assert.Equal(5, Run(leaning, "i").X, 9);
    }

    /// <summary>An integral's scripts stand by its top and its foot, apart by delta-integral of its height, with no script space.</summary>
    [Fact]
    public void AnIntegralsScripts_StandByItsTopAndItsFoot()
    {
        var ink = Laid(@"$\int_a^b$");
        double sign = 0.7 * Size * C.LargeOperatorScale;

        Assert.Equal(-(sign - C.SubDrop * XHeight), Run(ink, "b").Y, 9);
        Assert.Equal(0 + C.SubDrop * XHeight, Run(ink, "a").Y, 9);
        Assert.True(Run(ink, "b").X > Run(ink, "a").X);
        Assert.Equal(Math.Max(Run(ink, "b").X + 3.5, Run(ink, "a").X + 3.5), ink.Extent.Width, 9);
    }

    [Fact]
    public void ASecondSuperscript_StartsANucleusOfItsOwn()
    {
        var ink = Laid("$x^a^b$");

        Assert.True(Run(ink, "b").X > Run(ink, "a").X);
    }

    [Fact]
    public void Primes_AreASuperscript()
    {
        var ink = Laid("$f''$");

        Assert.True(Run(ink, "′′").Y < 0);
        Assert.True(Run(Laid("$f^a'$"), "′").X > Run(Laid("$f^a'$"), "a").X);
    }

    /// <summary>Scripts written alone stand on an empty nucleus, not on the text before the dollar sign.</summary>
    [Fact]
    public void ScriptsAfterTheTextOutsideMath_HaveAnEmptyNucleus()
    {
        var ink = Laid("R$^{2}$");

        Assert.Equal(5 + C.Delta * XHeight * C.ShrinkFactor, Run(ink, "2").X, 9);
    }

    /// <summary>A script inside a script is one size smaller again: the shrink factor twice.</summary>
    [Fact]
    public void ANestedScript_ShrinksTwice()
    {
        var ink = Laid("$x^{a^b}$");

        Assert.Equal(Size * C.ShrinkFactor * C.ShrinkFactor, Run(ink, "b").Font.Size, 9);
    }

    /// <summary>Past the last size level a script stays at the last size.</summary>
    [Fact]
    public void AScriptPastTheLastLevel_StaysAtTheLastSize()
    {
        var ink = Laid("$x^{x^{x^{x^{x^{x^{x^{x}}}}}}}$");

        double last = Size * Math.Pow(C.ShrinkFactor, C.SizeLevels - 1);
        Assert.Equal(last, ink.Glyphs.Min(g => g.Font.Size), 9);
    }

    // ---- limits ---------------------------------------------------------------------------------------------------------------

    /// <summary>A sum's limits: one size smaller, centred on the widest of the three on whole pixels, three rules from the sign.</summary>
    [Fact]
    public void ASumsLimits_StandCentredAboveAndBelowIt()
    {
        var ink = Laid(@"$\sum_{ij}^{n}$");
        double sign = 0.7 * Size * C.LargeOperatorScale, gap = 3 * Rule;
        var sum = Run(ink, "∑");

        Assert.Equal(-(sign + gap + 0), Run(ink, "n").Y, 9);
        Assert.Equal(0 + gap + 4.9, Run(ink, "ij").Y, 9);
        Assert.Equal(Math.Round((ink.Extent.Width - 3.5) / 2), Run(ink, "n").X, 9);
        Assert.Equal(Math.Round((ink.Extent.Width - sum.Font.Size / 2) / 2), sum.X, 9);
    }

    [Fact]
    public void AFunctionWhoseScriptsAreLimits_TakesThemAboveAndBelow_ThenAThinSpace()
    {
        var ink = Laid(@"$\lim_{x \to 0} f$");

        Assert.True(Run(ink, "x→0").Y > 0);
        Assert.Equal(Math.Max(15, 3 * 0.7 * 5) + 0.16667 * C.Quad * Size, Run(ink, "f").X, 9);
    }

    [Fact]
    public void Overset_AndUnderset_SetTheAnnotationOverOrUnderTheBody()
    {
        Assert.True(Run(Laid(@"$\overset{a}{x}$"), "a").Y < 0);
        Assert.True(Run(Laid(@"$\underset{a}{x}$"), "a").Y > 0);
    }

    // ---- fractions ------------------------------------------------------------------------------------------------------------

    /// <summary>A fraction in text style (matplotlib's <c>_genfrac</c>): numerator and denominator one size smaller, centred, the rule on
    /// the axis, a rule's width of padding on each side.</summary>
    [Fact]
    public void AFraction_IsStackedOverARuleOnTheAxis()
    {
        var ink = Laid(@"$\frac{a}{b}$");
        double xh = XHeight * C.ShrinkFactor, axis = C.AxisHeight * Size, half = Rule / 2;
        double numClear = Math.Max(C.Num2 * xh - 0 - (axis + half), Rule);
        double denClear = Math.Max(axis - half - (4.9 - C.Denom2 * xh), Rule);
        double den = 4.9 + denClear + half - axis;
        double ruleTop = den - 4.9 - denClear - Rule;

        Assert.Equal(new Rect(Rule, ruleTop, 3.5, Rule), Assert.Single(ink.Rules));
        Assert.Equal(den, Run(ink, "b").Y, 9);
        Assert.Equal(ruleTop - numClear, Run(ink, "a").Y, 9);
        Assert.Equal(Rule, Run(ink, "a").X, 9);
        Assert.Equal(3.5 + 2 * Rule, ink.Extent.Width, 9);
    }

    [Fact]
    public void ANarrowerPart_IsCentredOnTheWider()
    {
        var ink = Laid(@"$\frac{abc}{b}$");

        Assert.Equal(Rule + Math.Round((10.5 - 3.5) / 2), Run(ink, "b").X, 9);
    }

    /// <summary>In display style the parts keep the text's size, raised by num1 and dropped by denom1, three rules clear of the rule.</summary>
    [Fact]
    public void ADisplayFraction_KeepsTheTextsSize()
    {
        var ink = Laid(@"$\dfrac{a}{b}$");

        Assert.Equal(Size, Run(ink, "a").Font.Size, 9);
        Assert.True(Run(ink, "a").Y < Run(Laid(@"$\frac{a}{b}$"), "a").Y);
    }

    /// <summary>A binomial has no rule and stands in parentheses grown to it; its parts are kept three rules apart.</summary>
    [Fact]
    public void ABinomial_HasNoRule_AndStandsInParentheses()
    {
        var ink = Laid(@"$\binom{n}{k}$");

        Assert.Empty(ink.Rules);
        Assert.Contains(ink.Glyphs, g => g.Text == "(");
        Assert.Contains(ink.Glyphs, g => g.Text == ")");
        Assert.True(Run(ink, "k").Y - Run(ink, "n").Y >= 3 * Rule + 4.9 - 1e-9);
    }

    [Fact]
    public void ADisplayBinomial_KeepsItsPartsSevenRulesApart()
    {
        var ink = Laid(@"$\genfrac{(}{)}{0}{0}{n}{k}$");

        Assert.Equal(Size, Run(ink, "n").Font.Size, 9);
        Assert.True(Run(ink, "k").Y - Run(ink, "n").Y >= 7 * Rule + 0.7 * Size);
    }

    [Fact]
    public void AGeneralFraction_TakesItsRuleItsStyleAndItsDelimiters()
    {
        var ink = Laid(@"$\genfrac{[}{]}{2}{2}{a}{b}$");

        Assert.Equal(2, Assert.Single(ink.Rules).Height);
        Assert.Equal(Size * C.ShrinkFactor * C.ShrinkFactor, Run(ink, "a").Font.Size, 9);
        Assert.Contains(ink.Glyphs, g => g.Text == "[");
        Assert.Single(Laid(@"$\genfrac{}{}{}{}{a}{b}$").Rules);
    }

    // ---- radicals, rules ------------------------------------------------------------------------------------------------------

    /// <summary>A radical: the sign, then a bar from its top over the body padded by two rules on each side; the body stays on the
    /// baseline (to the glue's whole pixel).</summary>
    [Fact]
    public void ARadical_DrawsItsSignAndABarOverTheBody()
    {
        var ink = Laid(@"$\sqrt{x}$");
        var sign = Run(ink, "√");
        var bar = Assert.Single(ink.Rules);

        Assert.Equal(sign.X + sign.Font.Size / 2, bar.X, 9);
        Assert.Equal(Rule, bar.Height, 9);
        Assert.Equal(5 + 4 * Rule, bar.Width, 9);
        Assert.Equal(bar.X + 2 * Rule, Run(ink, "x").X, 9);
        Assert.InRange(Run(ink, "x").Y, -0.5, 0.5);
        Assert.True(bar.Y <= -(7 + 5 * Rule));
    }

    [Fact]
    public void ARadicalsIndex_IsTwoSizesSmaller_RaisedOverTheSign()
    {
        var ink = Laid(@"$\sqrt[3]{x}$");

        Assert.Equal(Size * C.ShrinkFactor * C.ShrinkFactor, Run(ink, "3").Font.Size, 9);
        Assert.True(Run(ink, "3").Y < 0);
        Assert.Equal(0, Run(ink, "3").X, 9);
    }

    [Fact]
    public void Overline_DrawsARuleOverTheBody()
    {
        var ink = Laid(@"$\overline{x}$");

        var bar = Assert.Single(ink.Rules);
        Assert.Equal(5, bar.Width, 9);
        Assert.True(bar.Y < -7);
    }

    [Fact]
    public void Underline_DrawsARuleThreeRulesBelowTheBody()
    {
        var ink = Laid(@"$\underline{g}$");

        Assert.Equal(new Rect(0, 2 + 3 * Rule, 5, Rule), Assert.Single(ink.Rules));
        Assert.Equal(2 + 5 * Rule, ink.Extent.Depth, 9);
    }

    // ---- delimiters, matrices, stacks -------------------------------------------------------------------------------------------

    /// <summary>A delimiter shorter than what it encloses (by more than a fifth of an x-height) is grown until it is as tall; one tall
    /// enough stays at the text's size.</summary>
    [Fact]
    public void ADelimiter_IsGrownToWhatItEncloses()
    {
        var tall = Laid(@"$\left(\frac{a}{b}\right)$");
        var level = Laid(@"$\left(x\right)$");

        Assert.True(Run(tall, "(").Font.Size > Size);
        Assert.Equal(Size, Run(level, "(").Font.Size);
        Assert.Equal(Run(tall, "(").Font.Size, Run(tall, ")").Font.Size);
    }

    [Fact]
    public void AMiddleDelimiter_StandsBetweenItsSegments_AndAnEmptyOneDrawsNothing()
    {
        var ink = Laid(@"$\left. a \middle| b \right)$");

        Assert.Equal(["a", "|", "b", ")"], ink.Glyphs.OrderBy(g => g.X).Select(g => g.Text));
        Assert.True(Run(ink, "b").X > Run(ink, "|").X);
    }

    [Fact]
    public void AStrayRightOrMiddle_IsItsDelimitersCharacter()
    {
        Assert.Equal("x)|", MathTextParser.Parse(@"$x\right)\middle|$").PlainText);
    }

    /// <summary>A matrix: cells at the text's size centred in their columns a quad apart, rows at least LaTeX's strut tall, the grid
    /// centred on the axis, its delimiters grown to it.</summary>
    [Fact]
    public void AMatrix_LaysItsCellsInRowsAndColumns_InsideItsDelimiters()
    {
        var ink = Laid(@"$\begin{pmatrix} a & bb \\ c & d \end{pmatrix}$");

        var a = Run(ink, "a");
        var c = Run(ink, "c");
        Assert.Equal(Size, a.Font.Size);
        Assert.Equal(a.X, c.X, 9);
        Assert.Equal((C.MatrixStrutHeight + C.MatrixStrutDepth) * Size, c.Y - a.Y, 9);
        Assert.Equal(a.X + 5 + C.MatrixColumnGap * Size, Run(ink, "bb").X, 9);
        Assert.Equal(Run(ink, "bb").X + Math.Round((10 - 5) / 2.0), Run(ink, "d").X, 9);
        Assert.Equal(2, ink.Glyphs.Count(g => g.Text is "(" or ")"));
        Assert.Equal(C.AxisHeight * Size + (C.MatrixStrutHeight + C.MatrixStrutDepth) * Size, ink.Extent.Height, 1e-9);
    }

    [Theory]
    [InlineData("matrix", "", "")]
    [InlineData("pmatrix", "(", ")")]
    [InlineData("bmatrix", "[", "]")]
    [InlineData("Bmatrix", "{", "}")]
    [InlineData("vmatrix", "|", "|")]
    [InlineData("Vmatrix", "‖", "‖")]
    [InlineData("cases", "", "")]
    public void AMatrixEnvironment_NamesItsDelimiters(string environment, string left, string right)
    {
        Assert.Equal(new MathDelimiters(left, right), MathEnvironment.DelimitersOf(environment));
    }

    [Fact]
    public void AnEmptyMatrix_HasNoRows()
    {
        var ink = Laid(@"$\begin{matrix}\end{matrix}$");

        Assert.Empty(ink.Glyphs);
        Assert.Equal(0, ink.Extent.Width);
    }

    [Fact]
    public void AMatrixEndingInARowBreak_HasNoEmptyLastRow()
    {
        var spans = MathTextParser.Parse(@"$\begin{matrix} a \\ \end{matrix}$").Spans;

        Assert.Equal(1, spans.Count(s => s.Kind == TextSpanKind.MatrixRowSeparator));
    }

    [Fact]
    public void ASubstack_SetsItsRowsCentredOneOverTheOther_TheLastOnTheBaseline()
    {
        var ink = Laid(@"$\substack{ab \\ c}$");

        Assert.Equal(0, Run(ink, "c").Y, 9);
        Assert.Equal(-(7 + 2 * Rule + 0), Run(ink, "ab").Y, 9);
        Assert.Equal(Math.Round((10 - 5) / 2.0), Run(ink, "c").X, 9);
    }

    // ---- phantom, laps, accents ----------------------------------------------------------------------------------------------

    [Fact]
    public void APhantom_TakesItsRoom_AndDrawsNothing()
    {
        var ink = Laid(@"$\phantom{ab}c$");

        Assert.Equal(10, Run(ink, "c").X, 9);
        Assert.Single(ink.Glyphs);
    }

    [Fact]
    public void Laps_DrawTheirContent_AndTakeNoRoom()
    {
        var left = Laid(@"$a\llap{bc}d$");
        var right = Laid(@"$a\rlap{bc}d$");

        Assert.Equal(-5, Run(left, "bc").X, 9);
        Assert.Equal(5, Run(left, "d").X, 9);
        Assert.Equal(5, Run(right, "bc").X, 9);
        Assert.Equal(5, Run(right, "d").X, 9);
    }

    [Fact]
    public void ACustomSpace_IsItsWidthInQuads()
    {
        Assert.Equal(5 + 2 * C.Quad * Size, Run(Laid(@"$a\hspace{2}b$"), "b").X, 9);
        Assert.Equal(5, Run(Laid(@"$a\hspace{x}b$"), "b").X, 9);
    }

    [Fact]
    public void AnAccent_IsTheMarkOnItsLetter_AndOnABoxItFollows()
    {
        Assert.Equal("x̂", Assert.Single(Laid(@"$\hat{x}$").Glyphs).Text);
        Assert.Contains(Laid(@"$\hat{\frac{a}{b}}$").Glyphs, g => g.Text == "̂");
    }

    [Fact]
    public void AnAccentedLetter_HoldsItsScriptsByItsTop()
    {
        var accented = Laid(@"$\hat{x}^2$");

        Assert.True(Run(accented, "2").Y <= Run(Laid("$x^2$"), "2").Y);
    }

    // ---- operators and functions ----------------------------------------------------------------------------------------------

    [Fact]
    public void ALargeOperator_IsTheSetsLargeOperatorSize()
    {
        Assert.Equal(Size * MathConstants.Library.LargeOperatorScale, Run(Laid(@"$\sum$"), "∑").Font.Size, 9);
        Assert.Equal(Size * MathConstants.DejaVuSans.LargeOperatorScale, Laid(@"$\sum$", MathFontSet.DejaVuSans).Glyphs[0].Font.Size, 9);
    }

    [Fact]
    public void AFunctionName_IsFollowedByAThinSpace_AndItsScriptsBesideIt()
    {
        var ink = Laid(@"$\sin^2 x$");

        Assert.True(Run(ink, "2").Y < 0);
        Assert.Equal(Run(ink, "2").X + 3.5 + C.ScriptSpace * XHeight + 0.16667 * C.Quad * Size, Run(ink, "x").X, 9);
    }

    [Fact]
    public void AnOperatorName_IsAWordInRoman_FollowedByAThinSpace()
    {
        var ink = Laid(@"$\operatorname{foo} x$");

        Assert.Equal(15 + 0.16667 * C.Quad * Size, Run(ink, "x").X, 9);
    }

    // ---- painting order ---------------------------------------------------------------------------------------------------------

    [Fact]
    public void AHebrewWordWithASuperscript_IsPaintedRightToLeft_TheSuperscriptFirst()
    {
        var ink = Laid("שבת$^{2}$");

        Assert.True(Run(ink, "2").X < Run(ink, "שבת").X);
    }

    [Fact]
    public void ALatinLabelWithAHebrewWordAndASuperscript_IsCutWhereTheLevelChanges()
    {
        var ink = Laid("x שבת$^{2}$");

        Assert.Equal(["x ", "2", "שבת"], ink.Glyphs.OrderBy(g => g.X).Select(g => g.Text));
    }

    [Fact]
    public void ALabelWithNothingRightToLeft_IsPaintedAsWritten()
    {
        Assert.Equal(["x", "2", " y"], Laid("x$^{2}$ y").Glyphs.OrderBy(g => g.X).Select(g => g.Text));
    }

    // ---- the edges of each construction ------------------------------------------------------------------------------------------

    /// <summary>Runs measured by the surface's boxes: every glyph rises the font size and reaches a fifth of it below the baseline.</summary>
    private static MathInk LaidByBoxes(string label) => MathInk.Of(TextLabel.Of(label).Parsed(null).Tree!,
        new MathStyle(MathFontSet.Library, new Font { Size = Size }, MathMeasure.ByBox(new MatPlotLibNet.Rendering.Svg.SvgRenderContext())));

    /// <summary>A radical whose sign is tall enough at the text's size keeps it, and a body that reaches deeper than the sign takes the
    /// list's depth no further than the sign's.</summary>
    [Fact]
    public void ARadicalOverADeepBody_KeepsItsSign_AndReachesNoDeeperThanIt()
    {
        var ink = LaidByBoxes(@"$\sqrt{_x}$");

        Assert.Equal(Size, Run(ink, "√").Font.Size);
        Assert.Equal(0.2 * Size, ink.Extent.Depth, 9);
    }

    [Fact]
    public void AnIntegralWithOneScript_SetsItByItsTopOrItsFoot()
    {
        double sign = 0.7 * Size * C.LargeOperatorScale;

        Assert.Equal(C.SubDrop * XHeight, Run(Laid(@"$\int_a$"), "a").Y, 9);
        Assert.Equal(-(sign - C.SubDrop * XHeight), Run(Laid(@"$\int^b$"), "b").Y, 9);
    }

    /// <summary>A group is no operator: a sum in braces takes its scripts beside it, read from its sign.</summary>
    [Fact]
    public void ASumInBraces_TakesItsScriptsBesideIt()
    {
        var ink = Laid(@"${\sum}^2$");

        var sum = Run(ink, "∑");
        Assert.True(Run(ink, "2").X > sum.X + sum.Font.Size / 2 - 1e-9);
        Assert.True(Run(ink, "2").Y < 0);
    }

    [Fact]
    public void AnEmptyNucleusOfAHandMadeRichText_HasNoLastCharacter()
    {
        var ink = MathInk.Of(MathBoxes.Of(new RichText([new TextSpan(""), new TextSpan("2", TextSpanKind.Superscript, 0.7)])),
            new MathStyle(MathFontSet.Library, new Font { Size = Size }, MathMeasure.ByInk(TestGlyphInk.Boxes)));

        Assert.Equal(C.Delta * XHeight * C.ShrinkFactor, Run(ink, "2").X, 9);
    }

    [Fact]
    public void AnIntegralWithAnEmptySign_HasNoLastCharacter()
    {
        var style = new MathStyle(MathFontSet.Library, new Font { Size = Size }, MathMeasure.ByInk(TestGlyphInk.Boxes));

        Assert.Null(new IntegralBox(new LargeSymbolBox("")).LastChar(style));
    }

    /// <summary>Whether a box counts as a character node follows its content: a phantom, a lap and a delimited group of characters do; a
    /// rule over or under a body, a stack and a matrix do not; scripts do while one of them stands alone.</summary>
    [Theory]
    [InlineData(@"\phantom{x}", true)]
    [InlineData(@"\llap{x}", true)]
    [InlineData(@"\left(x\right)", true)]
    [InlineData("x_i", true)]
    [InlineData("x_i^a", false)]
    [InlineData(@"\left(\frac{a}{b}\right)", false)]
    [InlineData(@"\underline{x}", false)]
    [InlineData(@"\overline{x}", false)]
    [InlineData(@"\substack{a}", false)]
    [InlineData(@"\sum_i", false)]
    public void ACharacterNode_IsABoxWithNoVerticalListInIt(string nucleus, bool charNode)
    {
        // A superscript of a character node is raised by its own rules alone; one of any other box from that box's top.
        double raised = -Run(Laid("${" + nucleus + "}^2$"), "2").Y;
        double ownRules = Math.Max(C.Sup1 * XHeight, XHeight / 4);

        Assert.Equal(charNode, Math.Abs(raised - ownRules) < 1e-9);
    }

    [Fact]
    public void ADelimitedGroupWithoutARightDelimiter_DrawsOnlyItsLeft()
    {
        Assert.Equal(["(", "x"], Laid(@"$\left(x\right.$").Glyphs.OrderBy(g => g.X).Select(g => g.Text));
        Assert.Equal(["(", "x"], Laid(@"$\left(x$").Glyphs.OrderBy(g => g.X).Select(g => g.Text));
    }

    [Fact]
    public void ASubstackOfOneRow_IsThatRow()
    {
        Assert.Equal(0, Run(Laid(@"$\substack{ab}$"), "ab").Y, 9);
        Assert.Equal("ab", MathTextParser.Parse(@"$\substack{ab").PlainText); // a substack the text ends in
    }

    [Fact]
    public void AnEmptyBoxInARightToLeftLabel_StandsWhereItIsWritten()
    {
        var ink = Laid("שבת$\\phantom{a}x$");

        Assert.True(Run(ink, "x").X < Run(ink, "שבת").X);
    }

    // ---- the ink ------------------------------------------------------------------------------------------------------------------

    [Fact]
    public void TheInk_MovesARangeOfItsRunsAndRules()
    {
        var ink = MathInk.Of(new RowBox([]), InkStyle);
        var from = ink.Mark;
        ink.AddGlyphs("a", new SpanFont(new Font(), 10), 1, 2);
        ink.AddRule(1, 2, 3, 4);
        ink.Shift(from, 10, 20);
        ink.Shift(ink.Mark, 5, 5);
        ink.Shift(from, 0, 0);

        Assert.Equal(11, ink.Glyphs[0].X);
        Assert.Equal(new Rect(11, 22, 3, 4), ink.Rules[0]);
        ink.Erase(from);
        Assert.Empty(ink.Glyphs);
        Assert.Empty(ink.Rules);
    }

    [Fact]
    public void TheInkOfOneRunAwayFromTheOrigin_IsNoSingleRun()
    {
        Assert.Null(Laid("$x_i$").Single);
        Assert.Null(Laid(@"$\overline{x}$").Single);
    }

    private static MathStyle InkStyle => new(MathFontSet.Library, new Font { Size = Size }, MathMeasure.ByInk(TestGlyphInk.Boxes));

    /// <summary>A measure lays a label out for its box alone: the box a draw's layout takes, from a layout that places no ink.</summary>
    [Theory]
    [InlineData(@"$\sqrt{x^2 + y^2}$")]
    [InlineData(@"$\frac{a}{b}$ and $\left(\begin{matrix}1&2\\3&4\end{matrix}\right)$")]
    [InlineData(@"$\phantom{x}\overline{ab}\underline{c}$")]
    public void TheBoxOfALabel_IsTheBoxItsDrawnLayoutTakes(string label)
    {
        var tree = TextLabel.Of(label).Parsed(null).Tree!;

        Assert.Equal(MathInk.Of(tree, InkStyle).Extent, MathInk.ExtentOf(tree, InkStyle));
    }

    [Fact]
    public void ALabelMeasuredThenDrawn_IsMeasuredOnceAndLaidOutWithItsInkOnce()
    {
        var tree = TextLabel.Of(@"$\frac{a}{b}$").Parsed(null).Tree!;
        var style = InkStyle;

        var box = tree.ExtentIn(style);
        Assert.Equal(box, tree.ExtentIn(style));
        var ink = tree.LaidOut(style);
        Assert.NotEmpty(ink.Glyphs);
        Assert.Equal(box, ink.Extent);
        Assert.Same(ink, tree.LaidOut(style));
        Assert.Equal(box, tree.ExtentIn(style));
    }

    [Fact]
    public void ALabelMeasuredInAnotherStyle_IsLaidOutAgain()
    {
        var tree = TextLabel.Of(@"$x$").Parsed(null).Tree!;
        var small = tree.ExtentIn(InkStyle);
        var large = tree.ExtentIn(new MathStyle(MathFontSet.Library, new Font { Size = 2 * Size }, MathMeasure.ByInk(TestGlyphInk.Boxes)));

        Assert.True(large.Width > small.Width);
    }
}
