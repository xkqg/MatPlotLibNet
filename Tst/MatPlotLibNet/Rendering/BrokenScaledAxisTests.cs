// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.TestFixtures;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>An axis break on a log or symlog axis: the break hides the data values between its bounds and is cut out of
/// the axis in the space the axis is drawn in, so the decades on either side keep their width, and the mark, the ticks and
/// the data all stand by the one rule (<see cref="AxisBreakMap"/>). The transform used to scale a value first and then
/// compare the scaled value with the break's bounds in data units, while the range was cut in data units and then
/// scaled.</summary>
public class BrokenScaledAxisTests
{
    private static readonly double[] Decades = [.. Enumerable.Range(0, 61).Select(i => Math.Pow(10, i / 10.0))];
    private static readonly double[] Squares = [.. Enumerable.Range(0, 41).Select(i => (double)(i * i))];
    private static readonly double[] Index61 = [.. Enumerable.Range(0, 61).Select(i => (double)i)];
    private static readonly double[] Index41 = [.. Enumerable.Range(0, 41).Select(i => (double)i)];

    /// <summary>Every data value strictly inside the break is hidden, and no other: 10^(i/10) in (100, 1000) is i = 21..29,
    /// in (2, 4) is i = 4..6; i² in (100, 400) is i = 11..19.</summary>
    [Theory]
    [InlineData(true,  AxisScale.Log,    100.0, 1000.0, 9)]
    [InlineData(true,  AxisScale.Log,    2.0,   4.0,    3)]
    [InlineData(false, AxisScale.Log,    100.0, 1000.0, 9)]
    [InlineData(false, AxisScale.Log,    2.0,   4.0,    3)]
    [InlineData(true,  AxisScale.SymLog, 100.0, 400.0,  9)]
    [InlineData(false, AxisScale.SymLog, 100.0, 400.0,  9)]
    public void ABreak_OnAScaledAxis_HidesTheDataValuesInsideIt(bool onX, AxisScale scale, double from, double to, int hidden)
    {
        var values = scale == AxisScale.Log ? Decades : Squares;
        var index = scale == AxisScale.Log ? Index61 : Index41;

        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                if (onX) ax.Plot(values, index).SetXScale(scale).WithXBreak(from, to);
                else ax.Plot(index, values).SetYScale(scale).WithYBreak(from, to);
            })
            .ToSvg();

        int drawn = svg.PolylinePoints().Count(p => double.IsFinite(p.X) && double.IsFinite(p.Y));
        Assert.Equal(values.Length - hidden, drawn);
    }

    /// <summary>On a log X axis from 1 to 10^6 with the decade (100, 1000) cut out, every decade left is as wide as every other,
    /// on either side of the break; 100 and 1000 stand at one place, where the data jumps and the mark stands; the ends of the
    /// range stand on the edges of the plot.</summary>
    [Fact]
    public void ALogXAxis_WithADecadeCutOut_KeepsEveryOtherDecadeTheSameWidth()
    {
        var (renderer, ctx, plot) = Render(ax =>
        {
            ax.Plot(Decades, Index61);
            ax.XAxis.Scale = AxisScale.Log;
            ax.XAxis.Min = 1;
            ax.XAxis.Max = 1e6;
            ax.AddXBreak(100, 1000);
        });
        var t = renderer.DrawnTransform!;
        double At(double x) => t.DataToPixel(x, 30).X;

        double decade = At(10) - At(1);
        Assert.Equal(plot.Width / 5, decade, 6);
        Assert.Equal(decade, At(100) - At(10), 6);
        Assert.Equal(At(100), At(1000), 6);
        Assert.Equal(decade, At(1e4) - At(1e3), 6);
        Assert.Equal(decade, At(1e6) - At(1e5), 6);
        Assert.Equal(plot.X, At(1), 6);
        Assert.Equal(plot.X + plot.Width, At(1e6), 6);
        Assert.True(double.IsNaN(At(300)));

        var mark = BreakGaps(ctx, plot, alongX: true).Single();
        Assert.Equal(At(100), mark.X + 4, 6);
        Assert.Contains(XTickPositions(ctx, plot), x => Math.Abs(x - At(1e4)) < 1e-6);
    }

    /// <summary>On a symlog Y axis (linear threshold 1) from 0 to 10^4 with (10, 100) cut out, the decades above the break keep
    /// the width of the decade below it, the break's ends stand at one height, the range's ends on the plot's edges, and the
    /// mark stands where the data jumps.</summary>
    [Fact]
    public void ASymLogYAxis_WithADecadeCutOut_KeepsEveryOtherDecadeTheSameHeight()
    {
        var (renderer, ctx, plot) = Render(ax =>
        {
            ax.Plot(Index41, Squares);
            ax.YAxis.Scale = AxisScale.SymLog;
            ax.YAxis.Min = 0;
            ax.YAxis.Max = 1e4;
            ax.AddYBreak(10, 100);
        });
        var t = renderer.DrawnTransform!;
        double At(double y) => t.DataToPixel(20, y).Y;

        double decade = At(1) - At(10);
        Assert.Equal(At(10), At(100), 6);
        Assert.Equal(decade, At(100) - At(1000), 6);
        Assert.Equal(decade, At(1000) - At(1e4), 6);
        Assert.Equal(plot.Y + plot.Height, At(0), 6);
        Assert.Equal(plot.Y, At(1e4), 6);
        Assert.True(double.IsNaN(At(50)));

        var mark = BreakGaps(ctx, plot, alongX: false).Single();
        Assert.Equal(At(10), mark.Y + 4, 6);
    }

    /// <summary>A log Y axis with a break and a symlog X axis with a break in one axes: each direction cuts its own break out of
    /// its own drawn space.</summary>
    [Fact]
    public void BothDirections_Broken_EachCutsItsOwnBreakInItsOwnScale()
    {
        var (renderer, _, plot) = Render(ax =>
        {
            ax.Plot(Squares, Decades[..41]);
            ax.XAxis.Scale = AxisScale.SymLog;
            ax.XAxis.Min = 0;
            ax.XAxis.Max = 1e4;
            ax.AddXBreak(10, 100);
            ax.YAxis.Scale = AxisScale.Log;
            ax.YAxis.Min = 1;
            ax.YAxis.Max = 1e4;
            ax.AddYBreak(10, 100);
        });
        var t = renderer.DrawnTransform!;

        Assert.Equal(t.DataToPixel(10, 1).X, t.DataToPixel(100, 1).X, 6);
        Assert.Equal(plot.X + plot.Width, t.DataToPixel(1e4, 1).X, 6);
        Assert.Equal(t.DataToPixel(1, 10).Y, t.DataToPixel(1, 100).Y, 6);
        Assert.Equal(plot.Height / 3, t.DataToPixel(1, 1).Y - t.DataToPixel(1, 10).Y, 6);
        Assert.Equal(plot.Y, t.DataToPixel(1, 1e4).Y, 6);
    }

    private static (CartesianAxesRenderer Renderer, RecordingRenderContext Ctx, Rect Plot) Render(Action<Axes> build)
    {
        var axes = new Axes();
        build(axes);
        var plot = new Rect(80, 60, 640, 480);
        var ctx = new RecordingRenderContext();
        var renderer = new CartesianAxesRenderer(axes, plot, ctx, Theme.Default);
        renderer.Render();
        return (renderer, ctx, plot);
    }

    /// <summary>The background gap every break mark erases: 8 pixels across, the whole plot long.</summary>
    private static Rect[] BreakGaps(RecordingRenderContext ctx, Rect plot, bool alongX) =>
        [.. ctx.Rects()
            .Where(r => alongX ? r.Width == 8 && r.Height == plot.Height : r.Height == 8 && r.Width == plot.Width)];

    /// <summary>Where the short vertical marks on the bottom edge stand: the X ticks.</summary>
    private static double[] XTickPositions(RecordingRenderContext ctx, Rect plot) =>
        [.. ctx.Of<DrawLineCall>().Select(c => (c.P1, c.P2))
            .Where(l => l.Item1.X == l.Item2.X && Math.Abs(l.Item1.Y - l.Item2.Y) < 12
                        && Math.Abs(Math.Max(l.Item1.Y, l.Item2.Y) - (plot.Y + plot.Height)) < 12)
            .Select(l => l.Item1.X)];
}
