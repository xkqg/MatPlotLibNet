// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Rendering.Svg;

namespace MatPlotLibNet.Tests.Rendering.Svg;

/// <summary>
/// Phase F.2.a — RED: pins every branch-family of <see cref="SvgWriterExtensions.ToSvgNumber"/>
/// before the extension exists. Replaces the private <c>SvgRenderContext.F(double)</c> static
/// with a proper extension method on double. TDD per `feedback_real_tdd_solid.md`.
/// </summary>
public class SvgWriterExtensionsTests
{
    [Fact]
    public void ToSvgNumber_Integer_FormatsAsInt()
    {
        Assert.Equal("5", 5.0.ToSvgNumber());
    }

    [Fact]
    public void ToSvgNumber_Fractional_UsesInvariantDecimalPoint()
    {
        // Force a non-invariant culture during the call — the result MUST still use '.'.
        var prev = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("nl-NL");  // uses ',' as decimal
            Assert.Equal("1.5", 1.5.ToSvgNumber());
        }
        finally { CultureInfo.CurrentCulture = prev; }
    }

    [Fact]
    public void ToSvgNumber_Negative_EmitsMinusSign()
    {
        Assert.Equal("-3.25", (-3.25).ToSvgNumber());
    }

    [Fact]
    public void ToSvgNumber_LargeNumber_UsesGeneralFormat()
    {
        // 1e20 is emitted by "G" as "1E+20" — pin the current behaviour.
        Assert.Equal("1E+20", 1e20.ToSvgNumber());
    }

    [Fact]
    public void ToSvgNumber_Zero_FormatsAsZero()
    {
        Assert.Equal("0", 0.0.ToSvgNumber());
    }

    [Fact]
    public void ToSvgNumber_NegativeZero_FormatsAsZero()
    {
        // "G" on double.NegativeZero yields "-0" in .NET 10+.
        Assert.Equal("-0", (-0.0).ToSvgNumber());
    }

    [Fact]
    public void ToSvgNumber_MatchesPrivateSvgRenderContextFFormatter()
    {
        // Contract: extension must produce IDENTICAL bytes to the old F(double) private
        // static across a range of values. The old impl was:
        //     value.ToString("G", CultureInfo.InvariantCulture)
        // Pin that exactly.
        foreach (var v in new[] { 0.1, 1.0 / 3, Math.PI, 1e-7, 1e7, -42.125 })
        {
            Assert.Equal(v.ToString("G", CultureInfo.InvariantCulture), v.ToSvgNumber());
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // AppendFillStroke — one test per branch family (F.2.b)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AppendFillStroke_NullFill_EmitsFillNone()
    {
        var sb = new StringBuilder();
        sb.AppendFillStroke(new ShapeStyle(null, null, 1));
        Assert.Equal(" fill=\"none\"", sb.ToString());
    }

    [Fact]
    public void AppendFillStroke_OpaqueFill_EmitsFillHex_NoOpacity()
    {
        var sb = new StringBuilder();
        sb.AppendFillStroke(new ShapeStyle(Color.FromHex("#FF0000"), null, 0));
        Assert.Equal(" fill=\"#FF0000\"", sb.ToString());
    }

    [Fact]
    public void AppendFillStroke_TransparentFill_AppendsFillOpacity()
    {
        var sb = new StringBuilder();
        var half = new Color(255, 0, 0, 128);  // alpha 128 / 255
        sb.AppendFillStroke(new ShapeStyle(half, null, 0));
        var s = sb.ToString();
        Assert.Contains(" fill=\"", s);
        Assert.Contains(" fill-opacity=\"", s);
        Assert.Contains((128 / 255.0).ToSvgNumber(), s);
    }

    [Fact]
    public void AppendFillStroke_WithStroke_EmitsStrokeAndWidth()
    {
        var sb = new StringBuilder();
        sb.AppendFillStroke(new ShapeStyle(null, Color.FromHex("#00FF00"), 2.5));
        var s = sb.ToString();
        Assert.Contains(" fill=\"none\"", s);
        Assert.Contains(" stroke=\"#00FF00\"", s);
        Assert.Contains(" stroke-width=\"2.5\"", s);
    }

    [Fact]
    public void AppendFillStroke_NullStroke_OmitsStrokeAttrs()
    {
        var sb = new StringBuilder();
        sb.AppendFillStroke(new ShapeStyle(Color.FromHex("#123456"), null, 99));
        Assert.DoesNotContain("stroke=", sb.ToString());
    }

    [Fact]
    public void AppendFillStroke_ReturnsSameStringBuilder_ForChaining()
    {
        var sb = new StringBuilder();
        var result = sb.AppendFillStroke(new ShapeStyle(null, null, 0));
        Assert.Same(sb, result);
    }

    [Fact]
    public void AppendFillStroke_DashedStroke_EmitsTheDashArrayAfterTheWidth()
    {
        var sb = new StringBuilder();
        sb.AppendFillStroke(new ShapeStyle(null, Color.FromHex("#00FF00"), 2.5) { StrokeLineStyle = LineStyle.Dashed });
        Assert.Equal(" fill=\"none\" stroke=\"#00FF00\" stroke-width=\"2.5\" stroke-dasharray=\"5,2\"", sb.ToString());
    }

    [Fact]
    public void AppendFillStroke_SolidStroke_EmitsNoDashArray()
    {
        var sb = new StringBuilder();
        sb.AppendFillStroke(new ShapeStyle(null, Color.FromHex("#00FF00"), 2.5));
        Assert.Equal(" fill=\"none\" stroke=\"#00FF00\" stroke-width=\"2.5\"", sb.ToString());
    }

    [Fact]
    public void AppendFillStroke_NoneStroke_EmitsNoStrokeAtAll()
    {
        var sb = new StringBuilder();
        sb.AppendFillStroke(new ShapeStyle(Color.FromHex("#123456"), Color.FromHex("#00FF00"), 2.5) { StrokeLineStyle = LineStyle.None });
        Assert.Equal(" fill=\"#123456\"", sb.ToString());
    }

    [Fact]
    public void AppendFillStroke_DashedLineStyleWithoutAStroke_EmitsNoDashArray()
    {
        var sb = new StringBuilder();
        sb.AppendFillStroke(new ShapeStyle(Color.FromHex("#123456"), null, 2.5) { StrokeLineStyle = LineStyle.Dashed });
        Assert.DoesNotContain("stroke", sb.ToString());
    }

    /// <summary>A stroke's colour keeps its opacity in the SVG, as its fill's does and as Skia and MAUI paint it: the hex colour
    /// carries none, so a translucent stroke writes <c>stroke-opacity</c> after its width, before its dashes. Before, the SVG drew
    /// a half-transparent edge opaque while the PNG of the same figure drew it half-transparent.</summary>
    [Fact]
    public void AppendFillStroke_TranslucentStroke_WritesItsOpacity()
    {
        var sb = new StringBuilder();
        sb.AppendFillStroke(new ShapeStyle(null, new Color(0, 255, 0, 128), 2.5) { StrokeLineStyle = LineStyle.Dashed });
        Assert.Equal($" fill=\"none\" stroke=\"#00FF00\" stroke-width=\"2.5\" stroke-opacity=\"{(128 / 255.0).ToSvgNumber()}\" stroke-dasharray=\"5,2\"",
            sb.ToString());
    }

    [Fact]
    public void AppendFillStroke_TranslucentStrokeThatIsNotDrawn_WritesNoOpacity()
    {
        var sb = new StringBuilder();
        sb.AppendFillStroke(new ShapeStyle(Color.FromHex("#123456"), new Color(0, 255, 0, 128), 0));
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
        Assert.Equal($" stroke-opacity=\"{(64 / 255.0).ToSvgNumber()}\"", sb.ToString());
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
        sb.AppendStroke(new StrokeStyle(Color.FromHex("#00FF00"), 2.5, LineStyle.Solid));
        Assert.Equal(" stroke=\"#00FF00\" stroke-width=\"2.5\"", sb.ToString());
    }

    /// <summary>A translucent dashed stroke writes its opacity after its width and before its dashes.</summary>
    [Fact]
    public void AppendStroke_TranslucentDashed_WritesItsOpacityBeforeItsDashes()
    {
        var sb = new StringBuilder();
        sb.AppendStroke(new StrokeStyle(new Color(0, 255, 0, 128), 2.5, LineStyle.Dashed));
        Assert.Equal($" stroke=\"#00FF00\" stroke-width=\"2.5\" stroke-opacity=\"{(128 / 255.0).ToSvgNumber()}\" stroke-dasharray=\"5,2\"",
            sb.ToString());
    }

    [Fact]
    public void AppendStroke_ReturnsSameStringBuilder_ForChaining()
    {
        var sb = new StringBuilder();
        Assert.Same(sb, sb.AppendStroke(new StrokeStyle(Colors.Black, 1, LineStyle.Solid)));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // AppendDashArray — one test per LineStyle branch (F.2.c)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AppendDashArray_Solid_EmitsNothing()
    {
        var sb = new StringBuilder();
        sb.AppendDashArray(LineStyle.Solid);
        Assert.Equal("", sb.ToString());
    }

    [Fact]
    public void AppendDashArray_None_EmitsNothing()
    {
        var sb = new StringBuilder();
        sb.AppendDashArray(LineStyle.None);
        Assert.Equal("", sb.ToString());
    }

    [Fact]
    public void AppendDashArray_Dashed_EmitsFiveCommaTwo()
    {
        var sb = new StringBuilder();
        sb.AppendDashArray(LineStyle.Dashed);
        Assert.Equal(" stroke-dasharray=\"5,2\"", sb.ToString());
    }

    [Fact]
    public void AppendDashArray_Dotted_EmitsOneCommaThree()
    {
        var sb = new StringBuilder();
        sb.AppendDashArray(LineStyle.Dotted);
        Assert.Equal(" stroke-dasharray=\"1,3\"", sb.ToString());
    }

    [Fact]
    public void AppendDashArray_DashDot_EmitsFourValuesCommaSeparated()
    {
        var sb = new StringBuilder();
        sb.AppendDashArray(LineStyle.DashDot);
        Assert.Equal(" stroke-dasharray=\"5,2,1,2\"", sb.ToString());
    }

    [Fact]
    public void AppendDashArray_ReturnsSameStringBuilder_ForChaining()
    {
        var sb = new StringBuilder();
        var result = sb.AppendDashArray(LineStyle.Dashed);
        Assert.Same(sb, result);
    }
}
