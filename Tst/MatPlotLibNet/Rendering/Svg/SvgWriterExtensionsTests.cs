// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>
/// Pins every branch-family of the exact number writer (<see cref="SvgWriterExtensions.AppendExactSvgNumber"/>)
/// before the extension exists. Replaces the private <c>SvgRenderContext.F(double)</c> static
/// with a proper extension method on double. TDD per `feedback_real_tdd_solid.md`.
/// </summary>
public class SvgWriterExtensionsTests
{
    private static string Exact(double value) => new StringBuilder().AppendExactSvgNumber(value).ToString();

    [Theory]
    [InlineData("5", 5.0)] // AppendExactSvgNumber_Integer_FormatsAsInt
    [InlineData("1E+20", 1e20)] // AppendExactSvgNumber_LargeNumber_UsesGeneralFormat
    [InlineData("0", 0.0)] // AppendExactSvgNumber_Zero_FormatsAsZero
    public void AppendExactSvgNumber_FormatsTheNumberAsTheShortestExactForm(string expected, double number)
    {
        Assert.Equal(expected, Exact(number));
    }

    [Fact]
    public void AppendExactSvgNumber_Fractional_UsesInvariantDecimalPoint()
    {
        // Force a non-invariant culture during the call — the result MUST still use '.'.
        var prev = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("nl-NL");  // uses ',' as decimal
            Assert.Equal("1.5", Exact(1.5));
        }
        finally { CultureInfo.CurrentCulture = prev; }
    }

    [Fact]
    public void AppendExactSvgNumber_Negative_EmitsMinusSign()
    {
        Assert.Equal("-3.25", Exact(-3.25));
    }

    [Fact]
    public void AppendExactSvgNumber_NegativeZero_KeepsItsSign()
    {
        // "G" on double.NegativeZero yields "-0" in .NET 10+.
        Assert.Equal("-0", Exact(-0.0));
    }

    [Fact]
    public void AppendExactSvgNumber_WritesTheGeneralFormatInvariantly()
    {
        // Contract: extension must produce IDENTICAL bytes to the old F(double) private
        // static across a range of values. The old impl was:
        //     value.ToString("G", CultureInfo.InvariantCulture)
        // Pin that exactly.
        foreach (var v in new[] { 0.1, 1.0 / 3, Math.PI, 1e-7, 1e7, -42.125 })
        {
            Assert.Equal(v.ToString("G", CultureInfo.InvariantCulture), Exact(v));
        }
    }

    [Fact]
    public void AppendExactSvgNumber_WritesTheGeneralFormOfTenThousandValues()
    {
        // The exact writer (a view attribute, an opacity) appends a number straight into the document instead of making a
        // string of it first; what lands in the document may not differ by one byte, for the edges of the format and for
        // ten thousand doubles of every size.
        var values = new List<double>
        {
            0.0, -0.0, 1.0, -1.0, 0.5, 1.0 / 3, Math.PI, 1e-7, 1e7, 1e20, -1e-300, 123456789.123456789,
            double.Epsilon, double.MaxValue, double.MinValue, double.NaN, double.PositiveInfinity, double.NegativeInfinity,
        };
        var random = new Random(7);
        for (int i = 0; i < 10_000; i++)
        {
            values.Add((random.NextDouble() - 0.5) * Math.Pow(10, random.Next(-12, 18)));
        }

        foreach (var v in values)
        {
            var sb = new StringBuilder("x");
            sb.AppendExactSvgNumber(v);
            Assert.Equal("x" + v.ToString("G", CultureInfo.InvariantCulture), sb.ToString());
        }
    }

    [Theory]
    [InlineData(0.0, "0")]
    [InlineData(-0.004, "0")]                   // rounds to zero: no "-0" either
    [InlineData(1.0, "1")]
    [InlineData(-1.0, "-1")]
    [InlineData(1.5, "1.5")]                    // trailing zero stripped
    [InlineData(10.1, "10.1")]
    [InlineData(100.0, "100")]                  // a bare point stripped
    [InlineData(1.25, "1.25")]
    [InlineData(-3.25, "-3.25")]
    [InlineData(0.125, "0.13")]                 // a half rounds away from zero
    [InlineData(-0.125, "-0.13")]
    [InlineData(0.005, "0.01")]
    [InlineData(-0.005, "-0.01")]
    [InlineData(0.06, "0.06")]                  // a leading zero after the point stays
    [InlineData(1.0 / 3, "0.33")]
    [InlineData(2.0 / 3, "0.67")]
    [InlineData(0.999, "1")]                    // the carry reaches the whole part
    [InlineData(9.996, "10")]
    [InlineData(123456.789, "123456.79")]
    [InlineData(1e7, "10000000")]
    [InlineData(1e15, "1000000000000000")]
    [InlineData(1e20, "1E+20")]                 // past what a whole number of hundredths holds: the general form
    [InlineData(-1e20, "-1E+20")]
    [InlineData(double.NaN, "NaN")]             // the NaN guard: written as it always was
    [InlineData(double.PositiveInfinity, "Infinity")]
    [InlineData(double.NegativeInfinity, "-Infinity")]
    public void AppendSvgNumber_WritesAPixelToTwoDecimals(double value, string expected)
    {
        Assert.Equal("x" + expected, new StringBuilder("x").AppendSvgNumber(value).ToString());
    }

    [Fact]
    public void AppendSvgNumber_WritesNegativeZeroAsZero()
    {
        Assert.Equal("0", new StringBuilder().AppendSvgNumber(-0.0).ToString());
    }

    [Fact]
    public void AppendSvgNumber_AgreesWithRoundingTheHundredthsOfTenThousandValues()
    {
        // The writer works on the number of hundredths as a whole number; read back, it is that whole number over a hundred.
        var random = new Random(11);
        for (int i = 0; i < 10_000; i++)
        {
            double v = (random.NextDouble() - 0.5) * Math.Pow(10, random.Next(-4, 9));
            string written = new StringBuilder().AppendSvgNumber(v).ToString();
            double hundredths = Math.Round(v * 100, MidpointRounding.AwayFromZero);
            Assert.Equal(hundredths / 100, double.Parse(written, CultureInfo.InvariantCulture));
            Assert.DoesNotContain("E", written);
            Assert.False(written.Contains('.') && written.EndsWith('0'), written);
            Assert.NotEqual("-0", written);
        }
    }

    [Fact]
    public void AppendSvgNumber_UsesTheInvariantDecimalPoint()
    {
        var prev = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("nl-NL");
            Assert.Equal("1.5", new StringBuilder().AppendSvgNumber(1.5).ToString());
        }
        finally { CultureInfo.CurrentCulture = prev; }
    }

    [Fact]
    public void AppendSvgPoint_WritesTheTwoNumbersWithACommaBetween()
    {
        Assert.Equal("1.5,-2", new StringBuilder().AppendSvgPoint(new Point(1.5, -2)).ToString());
    }

    // ──────────────────────────────────────────────────────────────────────────
    // AppendFillStroke — one test per branch family (F.2.b)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AppendFillStroke_NullFill_EmitsFillNone()
    {
        var sb = new StringBuilder();
        sb.AppendFillStroke(new ShapeStyle(null, null, 1), DashLook.Library);
        Assert.Equal(" fill=\"none\"", sb.ToString());
    }

    [Fact]
    public void AppendFillStroke_OpaqueFill_EmitsFillHex_NoOpacity()
    {
        var sb = new StringBuilder();
        sb.AppendFillStroke(new ShapeStyle(Color.FromHex("#FF0000"), null, 0), DashLook.Library);
        Assert.Equal(" fill=\"#FF0000\"", sb.ToString());
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("AppendFillStroke_WithStroke_EmitsStrokeAndWidth", () =>
        {
            var sb = new StringBuilder();
            sb.AppendFillStroke(new ShapeStyle(null, Color.FromHex("#00FF00"), 2.5), DashLook.Library);
            return sb.ToString();
        }, " fill=\"none\"", " stroke=\"#00FF00\"", " stroke-width=\"2.5\""),
        new DrawnToken("AppendFillStroke_NullStroke_OmitsStrokeAttrs", () =>
        {
            var sb = new StringBuilder();
            sb.AppendFillStroke(new ShapeStyle(Color.FromHex("#123456"), null, 99), DashLook.Library);
            return sb.ToString();
        }) { Absent = ["stroke="] },
        new DrawnToken("AppendFillStroke_DashedLineStyleWithoutAStroke_EmitsNoDashArray", () =>
        {
            var sb = new StringBuilder();
            sb.AppendFillStroke(new ShapeStyle(Color.FromHex("#123456"), null, 2.5) { StrokeLineStyle = LineStyle.Dashed }, DashLook.Library);
            return sb.ToString();
        }) { Absent = ["stroke"] },
        new DrawnToken("AppendFillStroke_TransparentFill_AppendsFillOpacity", () =>
        {
            var sb = new StringBuilder();
            var half = new Color(255, 0, 0, 128);  // alpha 128 / 255
            sb.AppendFillStroke(new ShapeStyle(half, null, 0), DashLook.Library);
            return sb.ToString();
        }, " fill=\"", " fill-opacity=\"", (128 / 255.0).ToString("G", CultureInfo.InvariantCulture)),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    [Fact]
    public void AppendFillStroke_ReturnsSameStringBuilder_ForChaining()
    {
        var sb = new StringBuilder();
        var result = sb.AppendFillStroke(new ShapeStyle(null, null, 0), DashLook.Library);
        Assert.Same(sb, result);
    }

    [Fact]
    public void AppendFillStroke_DashedStroke_EmitsTheDashArrayAfterTheWidth()
    {
        var sb = new StringBuilder();
        sb.AppendFillStroke(new ShapeStyle(null, Color.FromHex("#00FF00"), 2.5) { StrokeLineStyle = LineStyle.Dashed }, DashLook.Library);
        Assert.Equal(" fill=\"none\" stroke=\"#00FF00\" stroke-width=\"2.5\" stroke-dasharray=\"5,2\"", sb.ToString());
    }

    [Fact]
    public void AppendFillStroke_SolidStroke_EmitsNoDashArray()
    {
        var sb = new StringBuilder();
        sb.AppendFillStroke(new ShapeStyle(null, Color.FromHex("#00FF00"), 2.5), DashLook.Library);
        Assert.Equal(" fill=\"none\" stroke=\"#00FF00\" stroke-width=\"2.5\"", sb.ToString());
    }

    [Fact]
    public void AppendFillStroke_NoneStroke_EmitsNoStrokeAtAll()
    {
        var sb = new StringBuilder();
        sb.AppendFillStroke(new ShapeStyle(Color.FromHex("#123456"), Color.FromHex("#00FF00"), 2.5) { StrokeLineStyle = LineStyle.None }, DashLook.Library);
        Assert.Equal(" fill=\"#123456\"", sb.ToString());
    }

    /// <summary>A stroke's colour keeps its opacity in the SVG, as its fill's does and as Skia and MAUI paint it: the hex colour
    /// carries none, so a translucent stroke writes <c>stroke-opacity</c> after its width, before its dashes. Before, the SVG drew
    /// a half-transparent edge opaque while the PNG of the same figure drew it half-transparent.</summary>
    [Fact]
    public void AppendFillStroke_TranslucentStroke_WritesItsOpacity()
    {
        var sb = new StringBuilder();
        sb.AppendFillStroke(new ShapeStyle(null, new Color(0, 255, 0, 128), 2.5) { StrokeLineStyle = LineStyle.Dashed }, DashLook.Library);
        Assert.Equal($" fill=\"none\" stroke=\"#00FF00\" stroke-width=\"2.5\" stroke-opacity=\"{(128 / 255.0).ToString("G", CultureInfo.InvariantCulture)}\" stroke-dasharray=\"5,2\"",
            sb.ToString());
    }

    [Fact]
    public void AppendFillStroke_TranslucentStrokeThatIsNotDrawn_WritesNoOpacity()
    {
        var sb = new StringBuilder();
        sb.AppendFillStroke(new ShapeStyle(Color.FromHex("#123456"), new Color(0, 255, 0, 128), 0), DashLook.Library);
        Assert.Equal(" fill=\"#123456\"", sb.ToString());
    }

    // ──────────────────────────────────────────────────────────────────────────
    // AppendAlpha — the one rule for the alpha of a colour: the hex colour carries none, so a translucent
    // colour writes its alpha in the attribute of the paint it colours, and an opaque one writes nothing.
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AppendAlpha_TranslucentColour_WritesItsAlphaAsTheAttribute()
    {
        var sb = new StringBuilder();
        sb.AppendAlpha("stroke-opacity", new Color(0, 0, 255, 64));
        Assert.Equal($" stroke-opacity=\"{(64 / 255.0).ToString("G", CultureInfo.InvariantCulture)}\"", sb.ToString());
    }

    [Fact]
    public void AppendAlpha_OpaqueColour_WritesNothing()
    {
        var sb = new StringBuilder();
        sb.AppendAlpha("fill-opacity", Color.FromHex("#123456"));
        Assert.Equal("", sb.ToString());
    }

    [Fact]
    public void AppendAlpha_ReturnsSameStringBuilder_ForChaining()
    {
        var sb = new StringBuilder();
        Assert.Same(sb, sb.AppendAlpha("opacity", new Color(1, 2, 3, 4)));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // AppendStroke — the one writer of a stroke drawn from a colour: a line, a polyline and a shape's outline.
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>An opaque solid stroke writes its colour and width and nothing else: the bytes a line wrote before its alpha
    /// was carried.</summary>
    [Fact]
    public void AppendStroke_OpaqueSolid_WritesColourAndWidth()
    {
        var sb = new StringBuilder();
        sb.AppendStroke(new StrokeStyle(Color.FromHex("#00FF00"), 2.5, LineStyle.Solid), DashLook.Library);
        Assert.Equal(" stroke=\"#00FF00\" stroke-width=\"2.5\"", sb.ToString());
    }

    /// <summary>A translucent dashed stroke writes its opacity after its width and before its dashes.</summary>
    [Fact]
    public void AppendStroke_TranslucentDashed_WritesItsOpacityBeforeItsDashes()
    {
        var sb = new StringBuilder();
        sb.AppendStroke(new StrokeStyle(new Color(0, 255, 0, 128), 2.5, LineStyle.Dashed), DashLook.Library);
        Assert.Equal($" stroke=\"#00FF00\" stroke-width=\"2.5\" stroke-opacity=\"{(128 / 255.0).ToString("G", CultureInfo.InvariantCulture)}\" stroke-dasharray=\"5,2\"",
            sb.ToString());
    }

    [Fact]
    public void AppendStroke_ReturnsSameStringBuilder_ForChaining()
    {
        var sb = new StringBuilder();
        Assert.Same(sb, sb.AppendStroke(new StrokeStyle(Colors.Black, 1, LineStyle.Solid), DashLook.Library));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // AppendDashArray — one test per LineStyle branch (F.2.c)
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(LineStyle.Solid, "")] // AppendDashArray_Solid_EmitsNothing
    [InlineData(LineStyle.None, "")] // AppendDashArray_None_EmitsNothing
    [InlineData(LineStyle.Dashed, " stroke-dasharray=\"5,2\"")] // AppendDashArray_Dashed_EmitsFiveCommaTwo
    [InlineData(LineStyle.Dotted, " stroke-dasharray=\"1,3\"")] // AppendDashArray_Dotted_EmitsOneCommaThree
    [InlineData(LineStyle.DashDot, " stroke-dasharray=\"5,2,1,2\"")] // AppendDashArray_DashDot_EmitsFourValuesCommaSeparated
    public void AppendDashArray_EmitsTheDashesOfTheStyle(LineStyle style, string expected)
    {
        var sb = new StringBuilder();
        sb.AppendDashArray(style, 2.5, DashLook.Library);
        Assert.Equal(expected, sb.ToString());
    }

    [Fact]
    public void AppendDashArray_ALookThatNamesNoLengthsForAStyle_EmitsNothing()
    {
        var sb = new StringBuilder();
        sb.AppendDashArray(LineStyle.Dashed, 2.5, new DashLook { Dashed = [] });
        Assert.Equal("", sb.ToString());
    }

    [Fact]
    public void AppendDashArray_ReturnsSameStringBuilder_ForChaining()
    {
        var sb = new StringBuilder();
        var result = sb.AppendDashArray(LineStyle.Dashed, 2.5, DashLook.Library);
        Assert.Same(sb, result);
    }

    // --- AppendHex: the colour of a fill, a stroke, a stop, a hatch and a text, without a string of its own ---

    [Theory]
    [InlineData(0, 0, 0, "#000000")]
    [InlineData(255, 255, 255, "#FFFFFF")]
    [InlineData(255, 0, 0, "#FF0000")]
    [InlineData(1, 10, 171, "#010AAB")]
    [InlineData(16, 160, 15, "#10A00F")]
    public void AppendHex_WritesTheSixUpperCaseDigitsToHexReturns(byte r, byte g, byte b, string expected)
    {
        var color = new Color(r, g, b);

        Assert.Equal(expected, new StringBuilder().AppendHex(color).ToString());
        Assert.Equal(color.ToHex(), new StringBuilder().AppendHex(color).ToString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(128)]
    [InlineData(255)]
    public void AppendHex_LeavesTheAlphaToAppendAlpha(byte alpha)
    {
        var color = new Color(18, 52, 86, alpha);

        Assert.Equal("#123456", new StringBuilder().AppendHex(color).ToString());
    }

    [Fact]
    public void AppendHex_EveryByteValue_EqualsToHex()
    {
        var sb = new StringBuilder();
        for (int v = 0; v < 256; v++)
        {
            var color = new Color((byte)v, (byte)(255 - v), (byte)(v ^ 0x5A));
            sb.Clear();
            Assert.Equal(color.ToHex(), sb.AppendHex(color).ToString());
        }
    }

    [Fact]
    public void AppendHex_ReturnsSameStringBuilder_ForChaining()
    {
        var sb = new StringBuilder();
        Assert.Same(sb, sb.AppendHex(Colors.Red));
    }

    // ---- the name a join of a stroke's corners has in SVG ----
    // What the SVG surface calls the join of a stroke's corners: <c>round</c> and <c>bevel</c> by name, and nothing for a mitre,
    // which is what a browser draws when it is told no join.

    [Theory]
    [InlineData(JoinStyle.Round, "round")]
    [InlineData(JoinStyle.Bevel, "bevel")]
    [InlineData(JoinStyle.Miter, null)]
    public void AJoin_HasTheNameTheBrowserKnowsIt_OrNone(JoinStyle join, string? name)
    {
        Assert.Equal(name, join.ToSvgName());
    }

    // ── The view of an axes the browser scripts read ──

    private static readonly Rect Plot = new(10, 20, 300, 200);

    private static SecondaryAxisTransform Stacked(AxisSide side, int slot, AxisScale scale, double linthresh, double lo, double hi) =>
        new(side, slot, new DataTransform(0, 10, lo, hi, Plot, null, null, 0, 10, lo, hi, AxisScale.Linear, scale, 1.0, linthresh));

    [Fact]
    public void ThePlotArea_IsWrittenAsFourAttributesWithEveryDigit()
    {
        string written = new StringBuilder().AppendPlotArea(new Rect(10.123456789, 20, 300, 200)).ToString();

        Assert.Equal(" data-plot-x=\"10.123456789\" data-plot-y=\"20\" data-plot-w=\"300\" data-plot-h=\"200\"", written);
    }

    [Fact]
    public void AViewWithoutARange_IsThePlotAreaAlone()
    {
        Assert.Equal(" data-plot-x=\"10\" data-plot-y=\"20\" data-plot-w=\"300\" data-plot-h=\"200\"",
            new StringBuilder().AppendView(Plot, null).ToString());
    }

    [Fact]
    public void ALinearView_WritesThePlotAreaThenTheRangeAndNoScale()
    {
        string written = new StringBuilder().AppendView(Plot, new DataTransform(0, 10, -5, 5, Plot)).ToString();

        Assert.Equal(" data-plot-x=\"10\" data-plot-y=\"20\" data-plot-w=\"300\" data-plot-h=\"200\""
                     + " data-xmin=\"0\" data-xmax=\"10\" data-ymin=\"-5\" data-ymax=\"5\"", written);
    }

    [Fact]
    public void ALogAndASymlogAxis_WriteTheirScaleAndOnlyTheSymlogItsLinearThreshold()
    {
        var range = new DataTransform(0, 3, 0, 4, Plot, null, null, 1, 1000, -100, 100, AxisScale.Log, AxisScale.SymLog, 1.0, 0.25);

        string written = new StringBuilder().AppendView(Plot, range).ToString();

        Assert.Equal(" data-plot-x=\"10\" data-plot-y=\"20\" data-plot-w=\"300\" data-plot-h=\"200\""
                     + " data-xmin=\"0\" data-xmax=\"3\" data-ymin=\"0\" data-ymax=\"4\""
                     + " data-xscale=\"log\" data-yscale=\"symlog\" data-ylinthresh=\"0.25\"", written);
    }

    [Fact]
    public void TheBreaksOfAnAxis_AreFourNumbersEach_AfterTheScale()
    {
        var range = new DataTransform(0, 10, 0, 5, Plot, [new AxisBreak(2, 4)], [new AxisBreak(1, 2)], 0, 12, 0, 6,
            AxisScale.Linear, AxisScale.Linear, 1.0, 1.0);

        string written = new StringBuilder().AppendView(Plot, range).ToString();

        Assert.Equal(" data-plot-x=\"10\" data-plot-y=\"20\" data-plot-w=\"300\" data-plot-h=\"200\""
                     + " data-xmin=\"0\" data-xmax=\"10\" data-ymin=\"0\" data-ymax=\"5\""
                     + " data-xbreaks=\"2 4 2 2\" data-ybreaks=\"1 2 1 1\"", written);
    }

    [Fact]
    public void TheStackedYAxes_AreWrittenInTheirOrderWithTheirSideAndSlot_AndACountPerSide()
    {
        IReadOnlyList<SecondaryAxisTransform> stacked =
        [
            Stacked(AxisSide.Right, 0, AxisScale.Linear, 1, 0, 500),
            Stacked(AxisSide.Right, 1, AxisScale.Log, 1, 0, 3),
            Stacked(AxisSide.Left, 0, AxisScale.SymLog, 0.5, -7, 7),
        ];

        string written = new StringBuilder().AppendSecondaryViews(stacked).ToString();

        Assert.Equal(" data-sec-right-0-ymin=\"0\" data-sec-right-0-ymax=\"500\""
                     + " data-sec-right-1-ymin=\"0\" data-sec-right-1-ymax=\"3\" data-sec-right-1-yscale=\"log\""
                     + " data-sec-left-0-ymin=\"-7\" data-sec-left-0-ymax=\"7\" data-sec-left-0-yscale=\"symlog\""
                     + " data-sec-left-0-ylinthresh=\"0.5\""
                     + " data-sec-right-count=\"2\" data-sec-left-count=\"1\"", written);
    }

    [Fact]
    public void NoStackedYAxis_WritesNothing()
    {
        Assert.Equal("", new StringBuilder().AppendSecondaryViews(null).ToString());
        Assert.Equal("", new StringBuilder().AppendSecondaryViews([]).ToString());
    }

    [Fact]
    public void TheMembers_ChainOnTheBuilderTheyAreGiven()
    {
        var sb = new StringBuilder();

        Assert.Same(sb, sb.AppendPlotArea(Plot));
        Assert.Same(sb, sb.AppendView(Plot, null));
        Assert.Same(sb, sb.AppendSecondaryViews(null));
    }
}
