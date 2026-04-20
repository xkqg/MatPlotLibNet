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
        sb.AppendFillStroke(fill: null, stroke: null, strokeThickness: 1);
        Assert.Equal(" fill=\"none\"", sb.ToString());
    }

    [Fact]
    public void AppendFillStroke_OpaqueFill_EmitsFillHex_NoOpacity()
    {
        var sb = new StringBuilder();
        sb.AppendFillStroke(fill: Color.FromHex("#FF0000"), stroke: null, strokeThickness: 0);
        Assert.Equal(" fill=\"#FF0000\"", sb.ToString());
    }

    [Fact]
    public void AppendFillStroke_TransparentFill_AppendsFillOpacity()
    {
        var sb = new StringBuilder();
        var half = new Color(255, 0, 0, 128);  // alpha 128 / 255
        sb.AppendFillStroke(fill: half, stroke: null, strokeThickness: 0);
        var s = sb.ToString();
        Assert.Contains(" fill=\"", s);
        Assert.Contains(" fill-opacity=\"", s);
        Assert.Contains((128 / 255.0).ToSvgNumber(), s);
    }

    [Fact]
    public void AppendFillStroke_WithStroke_EmitsStrokeAndWidth()
    {
        var sb = new StringBuilder();
        sb.AppendFillStroke(fill: null, stroke: Color.FromHex("#00FF00"), strokeThickness: 2.5);
        var s = sb.ToString();
        Assert.Contains(" fill=\"none\"", s);
        Assert.Contains(" stroke=\"#00FF00\"", s);
        Assert.Contains(" stroke-width=\"2.5\"", s);
    }

    [Fact]
    public void AppendFillStroke_NullStroke_OmitsStrokeAttrs()
    {
        var sb = new StringBuilder();
        sb.AppendFillStroke(fill: Color.FromHex("#123456"), stroke: null, strokeThickness: 99);
        Assert.DoesNotContain("stroke=", sb.ToString());
    }

    [Fact]
    public void AppendFillStroke_ReturnsSameStringBuilder_ForChaining()
    {
        var sb = new StringBuilder();
        var result = sb.AppendFillStroke(fill: null, stroke: null, strokeThickness: 0);
        Assert.Same(sb, result);
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
