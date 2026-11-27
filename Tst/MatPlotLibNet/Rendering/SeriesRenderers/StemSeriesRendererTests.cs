// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>What a stem plot draws under the automatic range: every head is a whole circle inside the plot area. A verb that
/// draws stems (the lollipop chart) sets no axis limits, because a limit clips every other series on the axes and overwrites
/// the caller's own, so the heads at the two ends of the data have to keep their room from the automatic range alone.</summary>
public class StemSeriesRendererTests
{
    /// <summary>Where a stem's head is drawn: the centre of its circle and how far it reaches, in pixels.</summary>
    /// <param name="Cx">Horizontal centre.</param>
    /// <param name="Cy">Vertical centre.</param>
    /// <param name="Reach">The radius plus half the outline: how far the drawn circle reaches from its centre.</param>
    /// <param name="Fill">The colour it is filled with.</param>
    internal readonly record struct Head(double Cx, double Cy, double Reach, string Fill);

    /// <summary>The heads of a stem plot: the circles of the stem renderer, in the order they were drawn.</summary>
    internal static Head[] Heads(string svg) =>
        [.. svg.Elements("circle").Where(circle => circle["fill"] is { } fill && fill.StartsWith('#'))
            .Select(circle => new Head(circle.Number("cx"), circle.Number("cy"),
                circle.Number("r") + (circle["stroke-width"] is not null ? circle.Number("stroke-width") / 2 : 0), circle["fill"]!))];

    /// <summary>Under the automatic range the end heads keep the axis margin: each head is a whole circle inside the plot
    /// area on both axes. Measured before the fix in this 800 x 450 figure of six stems: the first head stood at the plot's
    /// left edge (cx 100, the circle reaching 96) and the last at its right edge (cx 720, reaching 724).</summary>
    [Fact]
    public void EveryHead_IsAWholeCircleInsideThePlotArea_UnderTheAutomaticRange()
    {
        string svg = Plt.Create().WithSize(800, 450)
            .AddSubPlot(1, 1, 1, ax => ax.Stem([0.5, 1.5, 2.5, 3.5, 4.5, 5.5], [82.5, 67.3, 91.1, 54.8, 76.4, 88.2]))
            .ToSvg();

        var plot = svg.PlotArea();
        var heads = Heads(svg);

        Assert.Equal(6, heads.Length);
        Assert.All(heads, h =>
        {
            Assert.True(h.Cx - h.Reach >= plot.X, $"head at {h.Cx} reaches past the left edge {plot.X}");
            Assert.True(h.Cx + h.Reach <= plot.X + plot.Width, $"head at {h.Cx} reaches past the right edge {plot.X + plot.Width}");
            Assert.True(h.Cy - h.Reach >= plot.Y, $"head at {h.Cy} reaches past the top edge {plot.Y}");
            Assert.True(h.Cy + h.Reach <= plot.Y + plot.Height, $"head at {h.Cy} reaches past the bottom edge {plot.Y + plot.Height}");
        });
    }

    // ---- drawn on a recording context ---------------------------------------------------------------------------------

    private static readonly Rect Bounds = new(0, 0, 400, 300);

    private static RecordingRenderContext Render(StemSeries series, Theme theme, Color? seriesColor = null)
    {
        var ctx = new RecordingRenderContext();
        var transform = new DataTransform(0, 10, -5, 5, Bounds);
        var context = new SeriesRenderContext(transform, ctx, seriesColor ?? theme.CycleColors[0], new RenderArea(Bounds, ctx)) { Theme = theme };
        new StemSeriesRenderer(context).Render(series);
        return ctx;
    }

    /// <summary>A stem's head is drawn in the marker style its series names, as matplotlib draws the head in <c>markerfmt</c>'s
    /// marker (<c>stem(..., markerfmt='s')</c> draws squares). The renderer drew a round dot whatever was set.</summary>
    [Fact]
    public void TheHeads_AreDrawnInTheMarkerStyleTheSeriesNames()
    {
        var ctx = Render(new StemSeries([1.0, 2], [1, 2]) { Marker = MarkerStyle.Square }, Theme.Default);

        Assert.Equal(2, ctx.Count<DrawRectangleCall>());
        Assert.Equal(0, ctx.Count<DrawCircleCall>());
    }

    /// <summary>The baseline is drawn in <c>basefmt</c>'s colour: matplotlib 3.11.2's <c>Axes.stem</c> reads
    /// <c>basefmt = ("C2-" if mpl.rcParams["_internal.classic_mode"] else "C3-")</c>, the cycle's fourth colour under the default
    /// style (#d62728, printed <c>(0.839, 0.153, 0.157)</c>) and its third under the classic one (<c>'r'</c>, <c>(1.0, 0.0, 0.0)</c>).
    /// The library drew it in the theme's neutral grey.</summary>
    [Theory]
    [InlineData("v2", "#D62728")]
    [InlineData("classic", "#FF0000")]
    public void TheBaseline_UnderAMatplotlibTheme_IsDrawnInTheColourOfBasefmt(string theme, string hex)
    {
        var baseline = Render(new StemSeries([1.0, 2, 3], [1, -2, 3]), theme.AsTheme()).Of<DrawLineCall>()[^1];

        Assert.Equal(Color.FromHex(hex), baseline.Color);
    }

    [Fact]
    public void ABaselineColourTheSeriesNames_WinsUnderAMatplotlibTheme()
    {
        var baseline = Render(new StemSeries([1.0, 2], [1, 2]) { BaselineColor = Colors.Green }, Theme.MatplotlibV2).Of<DrawLineCall>()[^1];

        Assert.Equal(Colors.Green, baseline.Color);
    }

    /// <summary>The stems and the baseline are as wide as the theme's lines: <c>stem</c> draws the stems with <c>vlines</c> and the
    /// baseline with <c>plot</c>, both at <c>lines.linewidth</c> (printed: 1.5 pt under the default style, 1.0 pt under the classic
    /// one). The library drew both 1 pixel wide under every theme, which it still does under its own.</summary>
    [Theory]
    [InlineData("v2", 1.5)]
    [InlineData("classic", 1.0)]
    public void TheStemsAndTheBaseline_UnderAMatplotlibTheme_AreAsWideAsTheThemesLines(string theme, double points)
    {
        var lines = Render(new StemSeries([1.0, 2, 3], [1, -2, 3]), theme.AsTheme()).Of<DrawLineCall>();

        Assert.Equal(4, lines.Count);
        Assert.All(lines, line => Assert.Equal(points.PointsInPixels, line.Thickness, 9));
    }

    [Fact]
    public void TheStemsAndTheBaseline_UnderTheLibrarysTheme_AreOnePixelWide()
    {
        var lines = Render(new StemSeries([1.0, 2, 3], [1, -2, 3]), Theme.Default).Of<DrawLineCall>();

        Assert.All(lines, line => Assert.Equal(1.0, line.Thickness));
    }

    /// <summary>A head is a <c>Line2D</c> marker: <c>lines.markersize</c> across (6 pt in both styles), outlined at
    /// <c>lines.markeredgewidth</c> in <c>markeredgecolor = 'auto'</c> (printed: 1.0 pt in the head's colour under the default style;
    /// 0.5 pt in black under the classic one, whose <c>'auto'</c> outlines a filled marker in <c>'k'</c>).</summary>
    [Theory]
    [InlineData("v2", 1.0, null)]
    [InlineData("classic", 0.5, "#000000")]
    public void AHead_UnderAMatplotlibTheme_IsALineMarkerOfSixPointsOutlinedAsTheThemeOutlinesOne(string theme, double edgePoints, string? edgeHex)
    {
        var look = theme.AsTheme();
        var head = Assert.Single(Render(new StemSeries([1.0], [1]), look).Of<DrawCircleCall>());

        Assert.Equal(6.0.PointsInPixels / 2, head.Radius, 9);
        Assert.Equal(look.CycleColors[0], head.Fill);
        Assert.Equal(edgeHex is null ? look.CycleColors[0] : Color.FromHex(edgeHex), head.Stroke);
        Assert.Equal(edgePoints.PointsInPixels, head.StrokeThickness, 9);
    }

    /// <summary>matplotlib adds the stems (a <c>LineCollection</c>), the heads (a <c>Line2D</c>) and the baseline (a <c>Line2D</c>) in
    /// that order at the same z-order (printed: <c>ax.get_children()</c> starts LineCollection, Line2D, Line2D), so every stem lies
    /// under every head and the baseline over both: the heads at a value of 0 carry the baseline across them.</summary>
    [Fact]
    public void TheStemsAreDrawnFirst_ThenTheHeads_ThenTheBaseline()
    {
        var kinds = Render(new StemSeries([1.0, 2, 3], [1, -2, 3]), Theme.MatplotlibV2).Calls.Select(c => c.Kind);

        Assert.Equal(["DrawLine", "DrawLine", "DrawLine", "DrawCircle", "DrawCircle", "DrawCircle", "DrawLine"], kinds);
    }

    /// <summary>The resting circle is still the 8-pixel dot it always was under the library's own themes.</summary>
    [Fact]
    public void TheRestingHead_UnderTheLibrarysTheme_IsAnEightPixelDotWithNoOutline()
    {
        var head = Assert.Single(Render(new StemSeries([1.0], [1]), Theme.Default).Of<DrawCircleCall>());

        Assert.Equal(4, head.Radius);
        Assert.Null(head.Stroke);
    }
}
