// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A polar axes whose radii fall below zero, or whose centre was given a radius of its own (matplotlib's <c>rmin</c> and
/// <c>rorigin</c>): the radii below zero are drawn on a scale that reaches them, and an origin below the low end leaves a hole at the
/// centre, bounded by an inner spine, as matplotlib draws <c>set_rorigin</c>.</summary>
public class PolarOriginRenderTests
{
    private static readonly Rect Cell = new(100, 60, 400, 400);

    private static Axes PolarAxes(double[] r, double? origin = null)
    {
        var axes = new Axes { CoordinateSystem = CoordinateSystem.Polar, ROrigin = origin };
        axes.AddSeries(new PolarLineSeries(r, [.. r.Select((_, i) => i * 0.7)]));
        return axes;
    }

    private static double DistanceFromCentre(Point point) => Math.Sqrt(Math.Pow(point.X - 300, 2) + Math.Pow(point.Y - 260, 2));

    [Fact]
    public void ALineWhollyBelowZero_IsDrawn_ItsPointAtMinus10WhereTheScalePutsIt()
    {
        // matplotlib v2: r -20, -10, -3 give rlim (-20.85, -2.15); -10 stands 10.85 / 18.7 of the way out.
        var ctx = PolarAxes([-20, -10, -3]).Drawn(Cell, Theme.MatplotlibV2);

        var line = ctx.Of<DrawLinesCall>().Last().Points;
        Assert.Equal(3, line.Length);
        Assert.All(line, point => point.AssertFinite());
        Assert.Equal(200 * 10.85 / 18.7, DistanceFromCentre(line[1]), 6);
    }

    [Fact]
    public void AnOriginBelowTheLowEnd_FillsARing_NotTheDisc()
    {
        var theme = Theme.MatplotlibV2;
        var ctx = PolarAxes([1, 5, 9], origin: -3).Drawn(Cell, theme);

        Assert.DoesNotContain(ctx.Of<DrawCircleCall>(), call => call.Fill == theme.AxesBackground);
        Assert.Single(ctx.Of<DrawPathCall>(), call => call.Fill == theme.AxesBackground);
    }

    [Fact]
    public void AnOriginBelowTheLowEnd_IsBoundedByAnInnerSpine_DrawnAsTheBottomSpine()
    {
        // matplotlib: spines['inner'] = Spine.arc_spine(self, 'bottom', ...), visible when the hole has a width.
        var theme = Theme.MatplotlibV2;
        var axes = PolarAxes([1, 5, 9], origin: -3);
        var ctx = axes.Drawn(Cell, theme);

        double hole = 200 * 3 / 12.4;     // rlim (0, 9.4), rorigin -3
        // The ring at r = 0 stands on the hole too, in the grid's colour; the spine is the circle there in the bottom spine's.
        var inner = ctx.Of<DrawCircleCall>()
            .Single(args => Math.Abs(args.Radius - hole) < 1e-6 && args.Stroke == axes.Spines.Bottom.ColorIn(theme));
        Assert.Equal(axes.Spines.Bottom.LineWidthIn(theme), inner.StrokeThickness, 9);
    }

    [Fact]
    public void AHiddenBottomSpine_LeavesTheHoleUnbounded()
    {
        var theme = Theme.MatplotlibV2;
        var axes = PolarAxes([1, 5, 9], origin: -3);
        axes.Spines = axes.Spines with { Bottom = axes.Spines.Bottom with { Visible = false } };

        var circles = axes.Drawn(Cell, theme).Of<DrawCircleCall>();

        Assert.DoesNotContain(circles, args => Math.Abs(args.Radius - 200 * 3 / 12.4) < 1e-6
            && args.Stroke == axes.Spines.Bottom.ColorIn(theme));
    }

    [Fact]
    public void ADashedBottomSpine_BoundsTheHoleWithABrokenCircle()
    {
        var axes = PolarAxes([1, 5, 9], origin: -3);
        axes.Spines = axes.Spines with { Bottom = axes.Spines.Bottom with { LineStyle = LineStyle.Dashed } };

        var ctx = axes.Drawn(Cell, Theme.MatplotlibV2);

        double hole = 200 * 3 / 12.4;
        Assert.Contains(ctx.Of<DrawLinesCall>(), call =>
            call.Points.All(point => Math.Abs(DistanceFromCentre(point) - hole) < 1e-6));
    }

    [Fact]
    public void WithAHole_TheSpokesStartAtItsEdge()
    {
        var ctx = PolarAxes([1, 5, 9], origin: -3).Drawn(Cell, Theme.MatplotlibV2);

        double hole = 200 * 3 / 12.4;
        var spokes = ctx.Of<DrawLineCall>().Select(call => call.P1).ToList();
        Assert.Equal(8, spokes.Count);
        Assert.All(spokes, start => Assert.Equal(hole, DistanceFromCentre(start), 6));
    }

    [Fact]
    public void WithNoHole_TheBackgroundIsTheDisc_AndThereIsNoInnerSpine()
    {
        var theme = Theme.MatplotlibV2;
        var ctx = PolarAxes([-20, -10, -3]).Drawn(Cell, theme);

        Assert.Single(ctx.Of<DrawCircleCall>(), call => call.Fill == theme.AxesBackground);
        Assert.Empty(ctx.Of<DrawPathCall>());
    }

    [Fact]
    public void ABarBelowZero_IsDrawnFromTheCentreLineDown_AsMatplotlibDrawsIt()
    {
        // matplotlib v2: ax.bar(theta, [-2, 3, 5]) gives rlim (-2.35, 5.35) and draws all three bars, the first from 0 down to -2.
        var axes = new Axes { CoordinateSystem = CoordinateSystem.Polar };
        axes.AddSeries(new PolarBarSeries([-2, 3, 5], [0, 2, 4]));

        var bars = axes.Drawn(Cell, Theme.MatplotlibV2).Polygons().ToList();

        Assert.Equal(3, bars.Count);
        double zero = 200 * 2.35 / 7.7, minusTwo = 200 * 0.35 / 7.7;
        Assert.Equal(minusTwo, bars[0].Min(DistanceFromCentre), 6);
        Assert.Equal(zero, bars[0].Max(DistanceFromCentre), 6);
    }

    [Fact]
    public void AStackedBarBelowZero_AddsNothing_AndIsNotDrawn()
    {
        var axes = new Axes { CoordinateSystem = CoordinateSystem.Polar, BarMode = BarMode.Stacked };
        axes.AddSeries(new PolarBarSeries([-3, 2], [0, 2]));
        axes.AddSeries(new PolarBarSeries([1, 1], [0, 2]));

        var bars = axes.Drawn(Cell, Theme.MatplotlibV2).Of<DrawPolygonCall>().ToList();

        Assert.Equal(3, bars.Count);
    }

    [Fact]
    public void SetROrigin_GivesTheAxesItsOrigin()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.PolarPlot([1.0, 5, 9], [0.0, 1, 2]).SetROrigin(-3)).Build();

        Assert.Equal(-3, figure.SubPlots[0].ROrigin);
    }

    [Fact]
    public void AnOrigin_ComesBackFromJson()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.PolarPlot([1.0, 5, 9], [0.0, 1, 2]).SetROrigin(-3)).Build();

        Assert.Equal(-3, figure.RoundTripped().SubPlots[0].ROrigin);
    }
}
