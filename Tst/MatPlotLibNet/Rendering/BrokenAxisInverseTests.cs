// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A pixel on an axis with breaks reads back the value drawn there: <see cref="DataTransform.PixelToData"/> undoes
/// the breaks by the same rule that cut them out (<see cref="AxisBreakMap"/>), on a linear, a log and a symlog axis. It used
/// to spread the pixel over the compressed range and undo only the scale, so a pointer past a break read a value smaller by
/// the gap below it. Where a break is cut, the one pixel both its ends stand on reads the value where the break begins.</summary>
public class BrokenAxisInverseTests
{
    private static readonly Rect Plot = new(80, 60, 640, 480);

    /// <summary>One axis' range and breaks, and the values that are drawn (none inside a break, none on a break's end).</summary>
    private sealed record Case(AxisScale Scale, double Min, double Max, (double From, double To)[] Breaks, double[] Values);

    private static Case CaseOf(AxisScale scale, int breaks) => (scale, breaks) switch
    {
        (AxisScale.Linear, 1) => new(scale, 0, 150, [(30, 80)], [0, 10, 29, 81, 95, 100, 130, 150]),
        (AxisScale.Linear, _) => new(scale, 0, 150, [(100, 120), (30, 80)], [0, 10, 29, 81, 95, 121, 130, 150]),
        (AxisScale.Log, 1) => new(scale, 1, 1e6, [(100, 1000)], [1, 5, 50, 99, 2000, 5000, 2e5, 1e6]),
        (AxisScale.Log, _) => new(scale, 1, 1e6, [(100, 1000), (1e4, 1e5)], [1, 5, 50, 99, 2000, 5000, 2e5, 1e6]),
        (_, 1) => new(scale, 0, 1e4, [(10, 100)], [0, 0.5, 5, 150, 500, 3000, 1e4]),
        _ => new(scale, 0, 1e4, [(10, 100), (1000, 2000)], [0, 0.5, 5, 150, 500, 3000, 1e4]),
    };

    private static DataTransform Drawn(bool onX, Case c)
    {
        var axes = new Axes();
        var axis = onX ? axes.XAxis : axes.YAxis;
        axis.Scale = c.Scale;
        axis.Min = c.Min;
        axis.Max = c.Max;
        var other = onX ? axes.YAxis : axes.XAxis;
        other.Min = 0;
        other.Max = 10;
        axes.Plot(c.Values, c.Values);
        foreach (var (from, to) in c.Breaks)
        {
            if (onX) axes.AddXBreak(from, to);
            else axes.AddYBreak(from, to);
        }

        var renderer = new CartesianAxesRenderer(axes, Plot, new RecordingRenderContext(), Theme.Default);
        renderer.Render();
        return renderer.DrawnTransform!;
    }

    [Theory]
    [InlineData(true, AxisScale.Linear, 1)]
    [InlineData(true, AxisScale.Linear, 2)]
    [InlineData(false, AxisScale.Linear, 1)]
    [InlineData(false, AxisScale.Linear, 2)]
    [InlineData(true, AxisScale.Log, 1)]
    [InlineData(true, AxisScale.Log, 2)]
    [InlineData(false, AxisScale.Log, 1)]
    [InlineData(false, AxisScale.Log, 2)]
    [InlineData(true, AxisScale.SymLog, 1)]
    [InlineData(true, AxisScale.SymLog, 2)]
    [InlineData(false, AxisScale.SymLog, 1)]
    [InlineData(false, AxisScale.SymLog, 2)]
    public void EveryDrawnValue_ReadsBackFromItsPixel(bool onX, AxisScale scale, int breaks)
    {
        var c = CaseOf(scale, breaks);
        var t = Drawn(onX, c);

        foreach (double v in c.Values)
        {
            var pixel = onX ? t.DataToPixel(v, 5) : t.DataToPixel(5, v);
            var read = t.PixelToData(pixel);
            v.AssertCloseTo(onX ? read.X : read.Y, relative: 1e-9, floor: 1, what: $"{scale} {(onX ? "X" : "Y")} with {breaks} break(s), value {v}");
        }
    }

    /// <summary>Both ends of a break are drawn on one pixel; that pixel reads where the break begins, and the next pixel to the
    /// right reads just past where it ends.</summary>
    [Fact]
    public void ThePixelWhereABreakIsCut_ReadsWhereTheBreakBegins_AndThePixelPastIt_ReadsPastItsEnd()
    {
        var t = Drawn(onX: true, CaseOf(AxisScale.Linear, 1));
        var cut = t.DataToPixel(30, 5);
        Assert.Equal(cut.X, t.DataToPixel(80, 5).X, 9);

        30.0.AssertCloseTo(t.PixelToData(cut).X, relative: 1e-9, floor: 1, what: "at the cut");
        double perPixel = 100.0 / Plot.Width; // 150 wide less the gap of 50, over the plot
        (80 + perPixel).AssertCloseTo(t.PixelToData(cut with { X = cut.X + 1 }).X, relative: 1e-9, floor: 1, what: "one pixel past the cut");
        (30 - perPixel).AssertCloseTo(t.PixelToData(cut with { X = cut.X - 1 }).X, relative: 1e-9, floor: 1, what: "one pixel before the cut");
    }

    /// <summary>The found repro, through the layout a native control converts the pointer with.</summary>
    [Fact]
    public void ThePointerOnAPointPastABreak_ReadsThatPointsValue_ThroughTheControlsLayout()
    {
        var figure = Plt.Create().WithSize(600, 500)
            .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 20, 100, 150], [0.0, 1, 2, 3]).WithXBreak(30, 80))
            .Build();
        var layout = ChartLayout.Create(figure, ChartRendererDefault.Current.ComputeLayout(figure, new SvgRenderContext()));
        var renderer = AxesRenderer.Create(figure.SubPlots[0], layout.GetPlotArea(0), new SvgRenderContext(), figure.Theme);
        renderer.Render();
        var pixel = renderer.DrawnTransform!.DataToPixel(100, 2);

        var read = layout.PixelToData(pixel.X, pixel.Y, 0);

        Assert.NotNull(read);
        100.0.AssertCloseTo(read!.Value.DataX, relative: 1e-9, floor: 1, what: "x");
        2.0.AssertCloseTo(read.Value.DataY, relative: 1e-9, floor: 1, what: "y");
    }

    /// <summary>An extended trendline runs from the left edge of the plot to the right edge on an X axis with a break too; it
    /// used to read the right edge as a value smaller by the gap and stop short of it.</summary>
    [Fact]
    public void AnExtendedTrendline_OnABrokenXAxis_ReachesTheRightEdge()
    {
        var axes = new Axes();
        axes.XAxis.Min = 0;
        axes.XAxis.Max = 150;
        axes.YAxis.Min = 0;
        axes.YAxis.Max = 3;
        axes.Plot([0.0, 150], [0.0, 3]);
        axes.AddXBreak(30, 80);
        var line = axes.AddTrendline(0, 0, 150, 1.5);
        line.IsExtended = true;
        line.Color = Colors.Red;
        var ctx = axes.Drawn(Plot);

        var drawn = Assert.Single(ctx.Of<DrawLineCall>(), c => c.Color == Colors.Red);
        var p1 = drawn.P1;
        var p2 = drawn.P2;

        Assert.Equal(Plot.X, p1.X, 6);
        Assert.Equal(Plot.X + Plot.Width, p2.X, 6);
        Assert.Equal(Plot.Y + Plot.Height / 2, p2.Y, 6); // y = 1.5 at x = 150, half way up 0..3
    }
}
