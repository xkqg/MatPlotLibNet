// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Downsampling;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.Downsampling;

/// <summary>A line holding NaN separators carries several runs at once (the segments of a vertical-lines chart, the
/// edges of a triangulation, the stems of a correlation), so a line that is downsampled or smoothed must never join two
/// runs across a gap. Measured before the fix: a line of 100 points with one gap, downsampled to four, was drawn as one
/// polyline holding points of both runs, because the largest-triangle downsampler read the NaN as a value, which
/// poisoned its bucket average and dropped the separator.</summary>
public class LineGapDownsamplingTests
{
    private static readonly Rect Bounds = new(0, 0, 400, 300);
    private static readonly DataTransform Transform = new(0, 99, -1, 8, Bounds);

    /// <summary>x = 0..99, y = i mod 7, missing at x = 50: a run of 50 points, the gap, a run of 49.</summary>
    private static (double[] X, double[] Y) GappedLine()
    {
        double[] x = [.. Enumerable.Range(0, 100).Select(i => (double)i)];
        double[] y = [.. Enumerable.Range(0, 100).Select(i => i == 50 ? double.NaN : i % 7)];
        return (x, y);
    }

    private static List<Point[]> DrawnLines(LineSeries series)
    {
        var ctx = new RecordingRenderContext();
        var context = new SeriesRenderContext(Transform, ctx, Colors.Tab10Blue, new RenderArea(Bounds, ctx));
        new LineSeriesRenderer(context).Render(series);
        return [.. ctx.OfKind("DrawLines").Select(c => (Point[])c.Args!.GetType().GetProperty("points")!.GetValue(c.Args)!)];
    }

    /// <summary>Every polyline lies wholly on one side of the gap at x = 50.</summary>
    private static void AssertNoPolylineCrossesTheGap(List<Point[]> lines)
    {
        double gap = Transform.DataToPixel(50, 0).X;
        Assert.All(lines, line => Assert.True(line.All(p => p.X < gap) || line.All(p => p.X > gap),
            $"a polyline joins the two runs: x from {line.Min(p => p.X)} to {line.Max(p => p.X)} across the gap at {gap}"));
    }

    [Fact]
    public void AGappedLine_DownsampledToFour_NeverJoinsTwoRuns()
    {
        var (x, y) = GappedLine();

        var lines = DrawnLines(new LineSeries(x, y) { MaxDisplayPoints = 4 });

        Assert.Equal(2, lines.Count);
        AssertNoPolylineCrossesTheGap(lines);
        AssertAt(0, y[0], lines[0][0]);
        AssertAt(49, y[49], lines[0][^1]);
        AssertAt(51, y[51], lines[1][0]);
        AssertAt(99, y[99], lines[1][^1]);
    }

    /// <summary>The pixel <paramref name="actual"/> is where the data point (<paramref name="x"/>, <paramref name="y"/>) is
    /// drawn.</summary>
    private static void AssertAt(double x, double y, Point actual)
    {
        var expected = Transform.DataToPixel(x, y);
        Assert.Equal(expected.X, actual.X, 9);
        Assert.Equal(expected.Y, actual.Y, 9);
    }

    [Fact]
    public void AGappedLine_Smoothed_NeverJoinsTwoRuns()
    {
        var (x, y) = GappedLine();

        var lines = DrawnLines(new LineSeries(x, y) { MaxDisplayPoints = 4, Smooth = true, SmoothResolution = 10 });

        Assert.Equal(2, lines.Count);
        AssertNoPolylineCrossesTheGap(lines);
    }

    /// <summary>The cull keeps the visible slice as one contiguous piece, so a separator inside it (a missing y, or a
    /// point missing in both x and y as a segment carrier writes it) stays where it was.</summary>
    [Fact]
    public void ViewportCuller_KeepsSeparators()
    {
        double[] x = [0, 1, 2, 3, 4, 5, 6, double.NaN, 8, 9, 10, 11];
        double[] y = [0, 1, 2, 3, double.NaN, 5, 6, double.NaN, 8, 9, 10, 11];

        var culled = ViewportCuller.Cull(x, y, 2.5, 8.5);

        Assert.Equal(x[2..10], culled.X);
        Assert.Equal(y[2..10], culled.Y);
    }

    /// <summary>Each run keeps its two ends and the runs are joined by one NaN point.</summary>
    [Fact]
    public void Lttb_KeepsEachRunsEnds_AndOneSeparatorBetweenThem()
    {
        var (x, y) = GappedLine();

        var sampled = new LttbDownsampler().Downsample(x, y, 4);

        Assert.Equal([0, 49, double.NaN, 51, 99], sampled.X);
        Assert.Equal([y[0], y[49], double.NaN, y[51], y[99]], sampled.Y);
    }

    /// <summary>The budget counts the separator and is shared in proportion to each run's length: runs of 60 and 40
    /// points with a budget of 21 keep 12 and 8, with the separator between them.</summary>
    [Fact]
    public void Lttb_SharesTheBudgetBetweenTheRunsInProportion()
    {
        double[] x = [.. Enumerable.Range(0, 101).Select(i => (double)i)];
        double[] y = [.. Enumerable.Range(0, 101).Select(i => i == 60 ? double.NaN : i % 5)];

        var sampled = new LttbDownsampler().Downsample(x, y, 21);

        Assert.Equal(21, sampled.X.Length);
        Assert.True(double.IsNaN(sampled.X[12]));
        Assert.Equal(1, sampled.X.Count(double.IsNaN));
        Assert.Equal(59, sampled.X[11]);
        Assert.Equal(61, sampled.X[13]);
    }

    /// <summary>A segment carrier (two points per segment, a separator between segments) has more runs than a small
    /// budget can hold: every segment keeps both its ends, so the downsampler hands it back whole rather than drop a
    /// segment or join two.</summary>
    [Fact]
    public void Lttb_KeepsEverySegmentOfASegmentCarrier()
    {
        var x = new List<double>();
        var y = new List<double>();
        for (int i = 0; i < 30; i++)
        {
            if (i > 0) { x.Add(double.NaN); y.Add(double.NaN); }
            x.AddRange([i, i]);
            y.AddRange([0, i + 1]);
        }

        double[] carrierX = [.. x], carrierY = [.. y];

        var sampled = new LttbDownsampler().Downsample(carrierX, carrierY, 10);

        Assert.Equal(carrierX, sampled.X);
        Assert.Equal(carrierY, sampled.Y);
    }

    /// <summary>A point with a gap on either side is a run of one: it is kept, so its marker is still drawn.</summary>
    [Fact]
    public void Lttb_KeepsALonePointBetweenTwoGaps()
    {
        double[] x = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];
        double[] y = [0, 1, 2, double.NaN, 4, double.NaN, 6, 7, 8, 9];

        var sampled = new LttbDownsampler().Downsample(x, y, 5);

        Assert.Equal([0, 2, double.NaN, 4, double.NaN, 6, 9], sampled.X);
    }

    [Fact]
    public void Lttb_OfNothingDrawable_IsEmpty()
    {
        var sampled = new LttbDownsampler().Downsample([0, 1, 2, 3], [double.NaN, double.NaN, double.NaN, double.NaN], 2);

        Assert.Empty(sampled.X);
        Assert.Empty(sampled.Y);
    }
}
