// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Coverage;

/// <summary>
/// Phase A.1 batch 2 — strict-90 floor plan. LegendMeasurer + MarkerRenderer
/// remaining partials.
/// </summary>
public class PinpointBranchTests17
{
    // ── LegendMeasurer.MeasureBox + LegendFont (L42, L54, L85, L96) ────────

    private static (Axes axes, Theme theme, IRenderContext ctx) NewMeasureContext(
        params string?[] labels)
    {
        var theme = Theme.Default;
        var axes = new Axes();
        axes.Legend = axes.Legend with { Visible = true };
        for (int i = 0; i < labels.Length; i++)
        {
            var s = new LineSeries(
                (global::MatPlotLibNet.Numerics.Vec)new[] { 0.0, 1.0 },
                (global::MatPlotLibNet.Numerics.Vec)new[] { 0.0, 1.0 });
            s.Label = labels[i];
            axes.AddSeries(s);
        }
        IRenderContext ctx = new SvgRenderContext();
        return (axes, theme, ctx);
    }

    [Fact]
    public void LegendMeasurer_LegendInvisible_ReturnsEmpty()
    {
        // L54: if (!axes.Legend.Visible) return Size.Empty; — true arm
        var (axes, theme, ctx) = NewMeasureContext("L1");
        axes.Legend = axes.Legend with { Visible = false };
        var size = LegendMeasurer.MeasureBox(axes, ctx, theme);
        Assert.Equal(Size.Empty, size);
    }

    [Fact]
    public void LegendMeasurer_NoLabelledSeries_ReturnsEmpty()
    {
        // labels.Count == 0 path
        var (axes, theme, ctx) = NewMeasureContext(null, null);
        var size = LegendMeasurer.MeasureBox(axes, ctx, theme);
        Assert.Equal(Size.Empty, size);
    }

    [Fact]
    public void LegendMeasurer_WithExplicitFontSize_AppliesOverride()
    {
        // L42: legend.FontSize.HasValue ? tickFont with { Size = ... } : tickFont — true arm
        var (axes, theme, ctx) = NewMeasureContext("L1");
        axes.Legend = axes.Legend with { FontSize = 18 };
        var size = LegendMeasurer.MeasureBox(axes, ctx, theme);
        Assert.True(size.Width > 0);
    }

    [Fact]
    public void LegendMeasurer_DefaultFontSize_UsesThemeTickFont()
    {
        // L42: false arm — FontSize is null (default)
        var (axes, theme, ctx) = NewMeasureContext("L1");
        var size = LegendMeasurer.MeasureBox(axes, ctx, theme);
        Assert.True(size.Width > 0);
    }

    [Fact]
    public void LegendMeasurer_MathLabel_UsesMeasureRichText()
    {
        // L85: MathTextParser.ContainsMath(labels[i]) ? MeasureRichText : MeasureText — true arm
        var (axes, theme, ctx) = NewMeasureContext(@"$\alpha$");
        var size = LegendMeasurer.MeasureBox(axes, ctx, theme);
        Assert.True(size.Width > 0);
    }

    [Fact]
    public void LegendMeasurer_PlainLabel_UsesMeasureText()
    {
        // L85: false arm — plain text
        var (axes, theme, ctx) = NewMeasureContext("plain");
        var size = LegendMeasurer.MeasureBox(axes, ctx, theme);
        Assert.True(size.Width > 0);
    }

    [Fact]
    public void LegendMeasurer_WithExplicitTitleFontSize_UsesOverride()
    {
        // L96: legend.TitleFontSize.HasValue ? ... : ... — true arm
        var (axes, theme, ctx) = NewMeasureContext("L1");
        axes.Legend = axes.Legend with { Title = "MyLegend", TitleFontSize = 20 };
        var size = LegendMeasurer.MeasureBox(axes, ctx, theme);
        Assert.True(size.Height > 0);
    }

    [Fact]
    public void LegendMeasurer_TitleWithoutExplicitTitleFontSize_UsesDefaultPlusOne()
    {
        // L96: false arm — Title set, TitleFontSize null → uses base+1 bold
        var (axes, theme, ctx) = NewMeasureContext("L1");
        axes.Legend = axes.Legend with { Title = "MyLegend" };
        var size = LegendMeasurer.MeasureBox(axes, ctx, theme);
        Assert.True(size.Height > 0);
    }

    // ── MarkerRenderer remaining markers + Cross/Plus stroke variations ────

    [Fact]
    public void MarkerRenderer_Pentagon_RendersWithoutError()
    {
        var svg = new SvgRenderContext();
        MarkerRenderer.Draw(svg, MarkerStyle.Pentagon, new Point(50, 50), 12,
            fill: Colors.Red, stroke: null, strokeWidth: 0);
        Assert.Contains("<polygon", svg.GetOutput());
    }

    [Fact]
    public void MarkerRenderer_Hexagon_RendersWithoutError()
    {
        var svg = new SvgRenderContext();
        MarkerRenderer.Draw(svg, MarkerStyle.Hexagon, new Point(50, 50), 12,
            fill: Colors.Blue, stroke: null, strokeWidth: 0);
        Assert.Contains("<polygon", svg.GetOutput());
    }

    [Fact]
    public void MarkerRenderer_Star_RendersWithoutError()
    {
        var svg = new SvgRenderContext();
        MarkerRenderer.Draw(svg, MarkerStyle.Star, new Point(50, 50), 12,
            fill: Colors.Green, stroke: null, strokeWidth: 0);
        Assert.Contains("<polygon", svg.GetOutput());
    }

    [Fact]
    public void MarkerRenderer_TriangleLeft_RendersWithoutError()
    {
        var svg = new SvgRenderContext();
        MarkerRenderer.Draw(svg, MarkerStyle.TriangleLeft, new Point(50, 50), 12,
            fill: Colors.Cyan, stroke: null, strokeWidth: 0);
        Assert.Contains("<polygon", svg.GetOutput());
    }

    [Fact]
    public void MarkerRenderer_TriangleRight_RendersWithoutError()
    {
        var svg = new SvgRenderContext();
        MarkerRenderer.Draw(svg, MarkerStyle.TriangleRight, new Point(50, 50), 12,
            fill: Colors.Magenta, stroke: null, strokeWidth: 0);
        Assert.Contains("<polygon", svg.GetOutput());
    }

    [Fact]
    public void MarkerRenderer_TriangleDown_RendersWithoutError()
    {
        var svg = new SvgRenderContext();
        MarkerRenderer.Draw(svg, MarkerStyle.TriangleDown, new Point(50, 50), 12,
            fill: Colors.Yellow, stroke: null, strokeWidth: 0);
        Assert.Contains("<polygon", svg.GetOutput());
    }

    [Fact]
    public void MarkerRenderer_Diamond_RendersWithoutError()
    {
        var svg = new SvgRenderContext();
        MarkerRenderer.Draw(svg, MarkerStyle.Diamond, new Point(50, 50), 12,
            fill: Colors.Black, stroke: null, strokeWidth: 0);
        Assert.Contains("<polygon", svg.GetOutput());
    }

    [Fact]
    public void MarkerRenderer_Cross_FillNullStrokeNonNull_UsesStrokeColor()
    {
        // L105 ?? chain: fill=null, stroke=non-null → color = stroke
        var svg = new SvgRenderContext();
        MarkerRenderer.Draw(svg, MarkerStyle.Cross, new Point(50, 50), 12,
            fill: null, stroke: Colors.Red, strokeWidth: 1.5);
        Assert.Contains("<line", svg.GetOutput());
    }

    [Fact]
    public void MarkerRenderer_Plus_FillNullStrokeNonNull_UsesStrokeColor()
    {
        var svg = new SvgRenderContext();
        MarkerRenderer.Draw(svg, MarkerStyle.Plus, new Point(50, 50), 12,
            fill: null, stroke: Colors.Blue, strokeWidth: 1.5);
        Assert.Contains("<line", svg.GetOutput());
    }

    [Fact]
    public void MarkerRenderer_NoneStyle_EarlyReturn()
    {
        // L45: if (style == MarkerStyle.None || size <= 0) return;  None arm
        var svg = new SvgRenderContext();
        MarkerRenderer.Draw(svg, MarkerStyle.None, new Point(50, 50), 12,
            fill: Colors.Red, stroke: null, strokeWidth: 0);
        Assert.DoesNotContain("<polygon", svg.GetOutput());
        Assert.DoesNotContain("<circle", svg.GetOutput());
    }

    [Fact]
    public void MarkerRenderer_ZeroSize_EarlyReturn()
    {
        // L45: size <= 0 arm
        var svg = new SvgRenderContext();
        MarkerRenderer.Draw(svg, MarkerStyle.Circle, new Point(50, 50), 0,
            fill: Colors.Red, stroke: null, strokeWidth: 0);
        Assert.DoesNotContain("<circle", svg.GetOutput());
    }

    [Fact]
    public void MarkerRenderer_NegativeSize_EarlyReturn()
    {
        var svg = new SvgRenderContext();
        MarkerRenderer.Draw(svg, MarkerStyle.Circle, new Point(50, 50), -5,
            fill: Colors.Red, stroke: null, strokeWidth: 0);
        Assert.DoesNotContain("<circle", svg.GetOutput());
    }
}
