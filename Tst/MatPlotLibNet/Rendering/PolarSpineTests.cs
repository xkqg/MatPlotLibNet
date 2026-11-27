// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A polar axes is closed by its spine, a circle on the edge of its square drawn as the spines of a Cartesian axes are,
/// over the series. matplotlib 3.11, <c>PolarAxes._gen_axes_spines</c>: <c>'polar': Spine.arc_spine(self, 'top', (0.5, 0.5),
/// 0.5, 0, 360)</c>, a spine of the type 'top' that a Spine draws in <c>axes.edgecolor</c> at <c>axes.linewidth</c> (black,
/// 0.8 pt in the default style, 1 pt in the classic one) at the spines' z-order 2.5, above the lines' 2; the grid has no ring
/// of its own there. Ours closed the circle with a ring of the grid's grey and width, drawn under the series, and drew none
/// when the grid was switched off.</summary>
public class PolarSpineTests
{
    private static readonly Rect Plot = new(0, 0, 400, 400);

    private static Axes PolarAxes()
    {
        var axes = new Axes { CoordinateSystem = CoordinateSystem.Polar };
        axes.PolarPlot([2.0, 10, 6], [0.0, 1, 2]);
        return axes;
    }

    /// <summary>The outline circles: drawn with a stroke and no fill, the rings of the grid and the spine.</summary>
    private static List<DrawCircleCall> Outlines(RecordingRenderContext ctx) =>
        [.. ctx.Of<DrawCircleCall>().Where(a => a.Stroke is not null && a.Fill is null)];

    /// <summary>The outline circles at the edge of the circle, 200 px from its centre: the spine (the grid draws no ring there).</summary>
    private static List<DrawCircleCall> AtTheEdge(RecordingRenderContext ctx) => [.. Outlines(ctx).Where(a => Math.Abs(a.Radius - 200) < 1e-9)];

    [Theory]
    [InlineData("MatplotlibV2")]
    [InlineData("MatplotlibClassic")]
    [InlineData("Default")]
    public void TheCircle_IsClosedByTheSpine_InTheSpinesColourAndWidth(string name)
    {
        var theme = name.AsTheme();
        var axes = PolarAxes();

        var spine = Assert.Single(AtTheEdge(axes.Drawn(Plot, theme)));

        Assert.Equal(new Point(200, 200), spine.Center);
        Assert.Equal(200, spine.Radius, 9);
        Assert.Equal(axes.Spines.Top.ColorIn(theme), spine.Stroke);
        Assert.Equal(axes.Spines.Top.LineWidthIn(theme), spine.StrokeThickness, 9);
    }

    [Fact]
    public void TheSpine_IsDrawnOverTheSeries()
    {
        var ctx = PolarAxes().Drawn(Plot, Theme.MatplotlibV2);

        int curve = ctx.Calls.FindIndex(c => c is DrawLinesCall);
        int spine = ctx.Calls.FindIndex(c => c is DrawCircleCall k && k.Fill is null && Math.Abs(k.Radius - 200) < 1e-9);
        Assert.True(curve >= 0 && spine > curve, $"the curve is call {curve}, the spine call {spine}");
    }

    [Fact]
    public void TheGrid_DrawsNoRingOfItsOwnAtTheEdge()
    {
        // The rings stand at 2.5, 5.0, 7.5 and 10.0 of an extent of 10.5: none at the edge.
        var ctx = PolarAxes().Drawn(Plot, Theme.MatplotlibV2);

        Assert.Single(AtTheEdge(ctx));
    }

    [Fact]
    public void WithTheGridSwitchedOff_TheSpineStays()
    {
        var axes = PolarAxes();
        new AxesBuilder(axes).WithGrid(g => g with { Visible = false });

        Assert.Single(Outlines(axes.Drawn(Plot, Theme.MatplotlibV2)));
    }

    [Fact]
    public void AHiddenSpine_IsNotDrawn()
    {
        var axes = PolarAxes();
        axes.Spines = axes.Spines with { Top = axes.Spines.Top with { Visible = false } };

        Assert.Empty(AtTheEdge(axes.Drawn(Plot, Theme.MatplotlibV2)));
    }

    [Fact]
    public void ASpineThatIsNotSolid_IsAClosedPolylineInItsStyle()
    {
        var axes = PolarAxes();
        axes.Spines = axes.Spines with { Top = axes.Spines.Top with { LineStyle = LineStyle.Dashed, Color = Colors.Red } };

        var ctx = axes.Drawn(Plot, Theme.MatplotlibV2);

        Assert.Empty(AtTheEdge(ctx));
        var ring = Assert.Single(ctx.Of<DrawLinesCall>(), a => a.Color == Colors.Red);
        Assert.Equal(LineStyle.Dashed, ring.Style);
        Point[] points = ring.Points;
        Assert.Equal(points[0], points[^1]);
        Assert.All(points, p => Assert.Equal(200, Math.Sqrt((p.X - 200) * (p.X - 200) + (p.Y - 200) * (p.Y - 200)), 9));
    }
}
