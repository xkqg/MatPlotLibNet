// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A polar and a ternary axes draw their series, their patches and their grid in the z-order a Cartesian axes draws them
/// in under the same theme (<see cref="ZOrderExtensions"/>). Printed, matplotlib 3.11.2 with mpltern 1.0.5: on a polar axes
/// <c>plot</c> 2, <c>scatter</c> 1, <c>bar</c> 1, <c>pcolormesh</c> 1, <c>fill</c> 1; the theta and radial axes, their grid and
/// tick labels, 1.5 under the default style (<c>axes.axisbelow 'line'</c>) and 2.5 under classic; the polar spine 2.5. On a
/// ternary axes <c>plot</c> 2, <c>scatter</c> 1 and the three axes 1.5 / 2.5 the same way. Under the library's own themes the grid
/// stays under everything and a series stands at its <see cref="Models.Series.ISeries.ZOrder"/>, as before.</summary>
public class PolarTernaryZOrderTests
{
    private static readonly Rect Area = new(100, 100, 400, 400);

    private static int First(RecordingRenderContext ctx, Color color) => ctx.Calls.FindIndex(c => c.Paints(color));

    /// <summary>The first ring of the polar grid: a circle stroked in the grid's colour.</summary>
    private static int FirstRing(RecordingRenderContext ctx, Color grid) =>
        ctx.Calls.FindIndex(c => c is DrawCircleCall k && c.Paints(grid));

    private static Axes PolarLineThenBar()
    {
        var axes = new Axes();
        axes.PolarPlot([1.0, 2.0, 3.0], [0.0, 1.0, 2.0]).Color = Colors.Green;
        axes.PolarBar([2.0, 3.0], [0.5, 2.0]).Color = Colors.Red;
        axes.WithGridColor(Colors.Blue);
        return axes;
    }

    [Fact]
    public void UnderTheDefaultStyle_APolarBarAddedAfterALine_IsDrawnUnderIt_AndTheGridBetween()
    {
        var ctx = PolarLineThenBar().Drawn(Area, Theme.MatplotlibV2);

        int bar = First(ctx, Colors.Red), ring = FirstRing(ctx, Colors.Blue), line = First(ctx, Colors.Green);
        Assert.True(bar < ring && ring < line, $"bar {bar}, grid {ring}, line {line}");
    }

    [Fact]
    public void UnderTheClassicStyle_ThePolarGridIsDrawnOverTheLine()
    {
        var ctx = PolarLineThenBar().Drawn(Area, Theme.MatplotlibClassic);

        int bar = First(ctx, Colors.Red), ring = FirstRing(ctx, Colors.Blue), line = First(ctx, Colors.Green);
        Assert.True(bar < line && line < ring, $"bar {bar}, line {line}, grid {ring}");
    }

    [Fact]
    public void UnderTheClassicStyle_TheRingLabelsAreDrawnWithTheGrid_OverTheLine()
    {
        var ctx = PolarLineThenBar().Drawn(Area, Theme.MatplotlibClassic);

        int line = First(ctx, Colors.Green);
        int label = ctx.Calls.FindIndex(c => c is DrawTextCall);
        Assert.True(line < label, $"line {line}, first label {label}");
    }

    [Fact]
    public void UnderTheLibrarysTheme_ThePolarGridIsDrawnFirst_AndTheSeriesInTheOrderTheyWereAdded()
    {
        var ctx = PolarLineThenBar().Drawn(Area, Theme.Default);

        int ring = FirstRing(ctx, Colors.Blue), line = First(ctx, Colors.Green), bar = First(ctx, Colors.Red);
        Assert.True(ring < line && line < bar, $"grid {ring}, line {line}, bar {bar}");
    }

    [Fact]
    public void UnderTheDefaultStyle_ATernaryScatterAddedAfterALine_IsDrawnUnderIt_AndTheGridBetween()
    {
        var axes = new Axes();
        axes.TernaryPlot([0.2, 0.3], [0.3, 0.3], [0.5, 0.4]).Color = Colors.Green;
        axes.TernaryScatter([0.6], [0.2], [0.2]).Color = Colors.Red;
        axes.WithGridColor(Colors.Blue);

        var ctx = axes.Drawn(Area, Theme.MatplotlibV2);

        int scatter = First(ctx, Colors.Red), grid = First(ctx, Colors.Blue), line = First(ctx, Colors.Green);
        Assert.True(scatter < grid && grid < line, $"scatter {scatter}, grid {grid}, line {line}");
    }
}

/// <summary>The grid of the axes shown, in one colour, for these tests.</summary>
file static class GridColourExtensions
{
    extension(Axes axes)
    {
        internal void WithGridColor(Color color) => axes.Grid = axes.Grid with { Visible = true, Color = color };
    }
}
