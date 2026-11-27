// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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
        var lines = svg.Elements("polyline");
        return lines.Count > 0 ? lines[0]["points"]!.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length : 0;
    }

    private static int MarkerCount(string svg) => svg.Elements("circle").Count;

    /// <summary>The marker areas the document actually carries, in draw order. The renderer writes a radius of
    /// <c>sqrt(size) / 2 * 100 / 72</c> (a marker is sqrt(size) points across), so the size is recoverable from it.</summary>
    private static double[] MarkerSizes(string svg)
    {
        const double dpiScale = 100.0 / 72.0;
        return [.. svg.Elements("circle").Where(circle => circle["r"] is not null).Select(circle => Math.Pow(2 * circle.Number("r") / dpiScale, 2))];
    }

    public static TheoryData<string, Action<AxesBuilder>, AxisScale, int> Budgets => new()
    {
        { "a line on a log axis", ax => ax.Plot(Decades, Ys, s => s.MaxDisplayPoints = 4), AxisScale.Log, 4 },
        { "a line on a linear axis, unchanged by the fix", ax => ax.Plot(Decades, Ys, s => s.MaxDisplayPoints = 4), AxisScale.Linear, 4 },
        { "a line on a log axis with no budget, every point", ax => ax.Plot(Decades, Ys), AxisScale.Log, 6 },
        // A step series emits 2n-1 points for n samples: the budget of 4 becomes 7 drawn coordinates.
        { "a step series on a log axis", ax => ax.Step(Decades, Ys, s => s.MaxDisplayPoints = 4), AxisScale.Log, 7 },
        // Its top line counts the points; the library's own theme draws one (a matplotlib theme's fill has none).
        { "an area on a log axis", ax => ax.FillBetween(Decades, Ys, configure: s => s.MaxDisplayPoints = 4), AxisScale.Log, 4 },
        // The monotonic path slices by index instead of scanning, and it was handed the same scaled bounds.
        { "a signal on a log axis", ax => ax.SignalXY(Decades, Ys, s => s.MaxDisplayPoints = 4), AxisScale.Log, 4 },
        { "a line on a symlog axis", ax => ax.Plot([-1_000, -10, -1, 0, 1, 10, 1_000], [1, 2, 3, 4, 5, 6, 7], s => s.MaxDisplayPoints = 5), AxisScale.SymLog, 5 },
    };

    /// <summary>A series drawn as one line draws the number of points its budget gives it, on every scale, under the library's own
    /// theme.</summary>
    [Theory]
    [MemberData(nameof(Budgets))]
    public void ASeriesDrawnAsALine_DrawsTheBudgetItWasGiven(string series, Action<AxesBuilder> draw, AxisScale scale, int points)
    {
        string svg = Plt.Create().WithTheme(Theme.Default).WithSize(800, 600)
            .AddSubPlot(1, 1, 1, ax => draw(ax.SetXScale(scale)))
            .Build().ToSvg();

        Assert.True(PolylinePoints(svg) == points, $"{series} drew {PolylinePoints(svg)} points, not {points}");
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

        var drawn = svg.Elements("circle").Select(circle => circle["fill"]).OfType<string>().Where(fill => fill.StartsWith('#')).Select(fill => fill.ToUpperInvariant()).ToArray();

        Assert.Equal(colours.Select(c => c.ToHex().ToUpperInvariant()).ToArray(), drawn);
    }
}
