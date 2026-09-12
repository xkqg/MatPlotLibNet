// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>
/// A series that asks for downsampling on a log or symlog axis used to lose most of its points. The renderers hand
/// <c>DataTransform.DataXMin</c>/<c>DataXMax</c> to <c>ViewportCuller.Cull</c>, and on those scales the transform
/// holds the range in SCALED space (0..5 for data 1..100000) while the culler compares it against the RAW values —
/// so everything above 10^DataXMax was cut as "outside the viewport" while the axis went on drawing ticks for it.
/// Measured before the fix: a six-point line with a budget of four drew 2 points, a step series 3, a scatter 2.
/// These facts pin the whole budget arriving on the canvas.
/// </summary>
public class LogAxisDownsamplingTests
{
    private static readonly double[] Decades = [1, 10, 100, 1_000, 10_000, 100_000];
    private static readonly double[] Ys = [1, 2, 3, 4, 5, 6];

    /// <summary>The number of coordinate pairs in the first polyline of the document.</summary>
    private static int PolylinePoints(string svg)
    {
        var m = Regex.Match(svg, @"<polyline points=""([^""]*)""");
        return m.Success ? m.Groups[1].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length : 0;
    }

    private static int MarkerCount(string svg) => Regex.Matches(svg, "<circle").Count;

    /// <summary>The marker areas the document actually carries, in draw order. The renderer writes a radius of
    /// <c>sqrt(size / PI) * 100 / 72</c>, so the size is recoverable from it.</summary>
    private static double[] MarkerSizes(string svg)
    {
        const double dpiScale = 100.0 / 72.0;
        return [.. Regex.Matches(svg, @"<circle[^>]*\br=""([0-9.eE+-]+)""")
            .Select(m => double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture))
            .Select(r => r * r * Math.PI / (dpiScale * dpiScale))];
    }

    [Fact]
    public void ALineOnALogAxis_DrawsTheBudgetItWasGiven()
    {
        string svg = Plt.Create().WithSize(800, 600)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(Decades, Ys, s => s.MaxDisplayPoints = 4)
                .SetXScale(AxisScale.Log))
            .Build().ToSvg();

        Assert.Equal(4, PolylinePoints(svg));
    }

    [Fact]
    public void ALineOnALinearAxis_IsUnchangedByTheFix()
    {
        string svg = Plt.Create().WithSize(800, 600)
            .AddSubPlot(1, 1, 1, ax => ax.Plot(Decades, Ys, s => s.MaxDisplayPoints = 4))
            .Build().ToSvg();

        Assert.Equal(4, PolylinePoints(svg));
    }

    [Fact]
    public void ALineOnALogAxis_WithNoBudget_DrawsEveryPoint()
    {
        string svg = Plt.Create().WithSize(800, 600)
            .AddSubPlot(1, 1, 1, ax => ax.Plot(Decades, Ys).SetXScale(AxisScale.Log))
            .Build().ToSvg();

        Assert.Equal(6, PolylinePoints(svg));
    }

    [Fact]
    public void AStepSeriesOnALogAxis_DrawsTheBudgetItWasGiven()
    {
        string svg = Plt.Create().WithSize(800, 600)
            .AddSubPlot(1, 1, 1, ax => ax
                .Step(Decades, Ys, s => s.MaxDisplayPoints = 4)
                .SetXScale(AxisScale.Log))
            .Build().ToSvg();

        // A step series emits 2n-1 points for n samples: the budget of 4 becomes 7 drawn coordinates.
        Assert.Equal(7, PolylinePoints(svg));
    }

    [Fact]
    public void AnAreaSeriesOnALogAxis_KeepsItsPoints()
    {
        string svg = Plt.Create().WithSize(800, 600)
            .AddSubPlot(1, 1, 1, ax => ax
                .FillBetween(Decades, Ys, configure: s => s.MaxDisplayPoints = 4)
                .SetXScale(AxisScale.Log))
            .Build().ToSvg();

        Assert.Equal(4, PolylinePoints(svg));
    }

    [Fact]
    public void AScatterOnALogAxis_DrawsEveryPointInRange()
    {
        string svg = Plt.Create().WithSize(800, 600)
            .AddSubPlot(1, 1, 1, ax => ax
                .Scatter(Decades, Ys, s => s.MaxDisplayPoints = 4)
                .SetXScale(AxisScale.Log))
            .Build().ToSvg();

        Assert.Equal(6, MarkerCount(svg));
    }

    [Fact]
    public void ALineOnASymLogAxis_DrawsTheBudgetItWasGiven()
    {
        double[] x = [-1_000, -10, -1, 0, 1, 10, 1_000];
        double[] y = [1, 2, 3, 4, 5, 6, 7];

        string svg = Plt.Create().WithSize(800, 600)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(x, y, s => s.MaxDisplayPoints = 5)
                .SetXScale(AxisScale.SymLog))
            .Build().ToSvg();

        Assert.Equal(5, PolylinePoints(svg));
    }

    [Fact]
    public void ASignalSeriesOnALogAxis_KeepsItsPoints()
    {
        // The monotonic path slices by index instead of scanning, and it was handed the same scaled bounds.
        string svg = Plt.Create().WithSize(800, 600)
            .AddSubPlot(1, 1, 1, ax => ax
                .SignalXY(Decades, Ys, s => s.MaxDisplayPoints = 4)
                .SetXScale(AxisScale.Log))
            .Build().ToSvg();

        Assert.Equal(4, PolylinePoints(svg));
    }

    [Fact]
    public void AScatterWithPerPointSizes_GivesEveryMarkerItsOwnSize()
    {
        double[] sizes = [10, 20, 30, 40, 50, 60];

        string svg = Plt.Create().WithSize(800, 600)
            .AddSubPlot(1, 1, 1, ax => ax
                .Scatter(Decades, Ys, s => { s.Sizes = sizes; s.MaxDisplayPoints = 4; })
                .SetXScale(AxisScale.Log))
            .Build().ToSvg();

        Assert.Equal(sizes, MarkerSizes(svg).Select(v => Math.Round(v)).ToArray());
    }

    [Fact]
    public void AScatterWithPerPointColours_KeepsEachPointsOwnColour()
    {
        Color[] colours = [Colors.Red, Colors.Green, Colors.Blue, Colors.Red, Colors.Green, Colors.Blue];

        string svg = Plt.Create().WithSize(800, 600)
            .AddSubPlot(1, 1, 1, ax => ax
                .Scatter(Decades, Ys, s => { s.Colors = colours; s.MaxDisplayPoints = 4; })
                .SetXScale(AxisScale.Log))
            .Build().ToSvg();

        var drawn = Regex.Matches(svg, @"<circle[^>]*\bfill=""(#[0-9A-Fa-f]{6})""")
            .Select(m => m.Groups[1].Value.ToUpperInvariant())
            .ToArray();

        Assert.Equal(colours.Select(c => c.ToHex().ToUpperInvariant()).ToArray(), drawn);
    }
}
