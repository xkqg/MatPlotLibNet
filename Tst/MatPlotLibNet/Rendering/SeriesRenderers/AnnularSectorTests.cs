// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>A slice of a ring (a pie's slice, a dial's band, a polar heat map's cell) is one value, <see cref="RingWedge"/>, drawn as a
/// polyline at one resolution: its sweep's share of a turn of 120 steps, never fewer than three. A dial's band was a flat 20 steps and
/// a polar heat map's cell a flat 12 whatever they swept, so a slice of the same sweep was smoother in a pie than in either, and a
/// pie slice's step count moved by one with the angle it started at (the sweep was the difference of two accumulated angles, which
/// is not exact).</summary>
public class AnnularSectorTests
{
    private static readonly Rect Plot = new(80, 40, 400, 300);

    // ── The one resolution of a sector drawn as a polyline ──────────────────────────

    /// <summary>A gauge's band is drawn at the resolution a pie's slice is: its sweep's share of a turn of 120 steps, where it was a
    /// flat 20 whatever the sweep. The first band of the resting dial (0 to 60 of 100) sweeps 108 degrees.</summary>
    [Fact]
    public void ADialsBand_IsDrawnAtTheResolutionOfItsSweep()
    {
        var axes = new Axes();
        axes.Gauge(63);

        var bands = axes.Drawn(Plot).Polygons();

        int steps = RingWedge.StepsFor(108);
        Assert.Equal(36, steps);
        Assert.Equal(2 * (steps + 1), bands[0].Length);
    }

    /// <summary>A dial's bands meet on the arcs they share: each band's outer arc ends where the next one's begins.</summary>
    [Fact]
    public void TheBandsOfADial_MeetOnTheirArcs()
    {
        var axes = new Axes();
        axes.Gauge(63);

        var bands = axes.Drawn(Plot).Polygons();

        int steps = RingWedge.StepsFor(108);
        Assert.Equal(bands[0][steps].X, bands[1][0].X, 9);
        Assert.Equal(bands[0][steps].Y, bands[1][0].Y, 9);
    }

    /// <summary>A polar heat map's cell is drawn at the same resolution: eight theta bins are 45 degrees each, 15 steps of the 120 to
    /// the turn, where each cell was a flat 12.</summary>
    [Fact]
    public void APolarHeatmapCell_IsDrawnAtTheResolutionOfItsSweep()
    {
        var data = new double[8, 4];
        var axes = new Axes();
        axes.PolarHeatmap(data, 8, 4);

        var cells = axes.Drawn(Plot).Polygons();

        int steps = RingWedge.StepsFor(45);
        Assert.Equal(15, steps);
        Assert.Equal(8 * 4, cells.Count);
        // The cell of the second ring has a hole at its inner edge: outer arc, then inner arc back.
        Assert.Contains(cells, cell => cell.Length == 2 * (steps + 1));
    }

    /// <summary>A pie of thirty equal slices draws each in four steps, a slice being a thirtieth of the 120 steps of a turn: the step
    /// count was the ceiling of a product that came out a rounding error over four, so each slice drew five.</summary>
    [Fact]
    public void APieSliceOfAThirtiethOfATurn_IsFourSteps()
    {
        var axes = new Axes();
        axes.Pie([.. Enumerable.Repeat(1.0, 30)]);

        var slice = (IReadOnlyList<PathSegment>)axes.Drawn(Plot).Of<DrawPathCall>()[0].Segments;

        Assert.Equal(1 + 1 + 4 + 1, slice.Count);
    }

    /// <summary>A pie slice keeps its resolution: 90 degrees are 30 steps, the centre and the first rim point before them.</summary>
    [Fact]
    public void APieSlice_IsDrawnAtTheResolutionOfItsSweep()
    {
        var axes = new Axes();
        axes.Pie([1.0, 1.0, 1.0, 1.0]);

        var slice = ((IReadOnlyList<PathSegment>)axes.Drawn(Plot).Of<DrawPathCall>()[0].Segments);

        Assert.Equal(1 + 1 + RingWedge.StepsFor(90) + 1, slice.Count);
        Assert.IsType<MoveToSegment>(slice[0]);
        Assert.IsType<CloseSegment>(slice[^1]);
    }
}
