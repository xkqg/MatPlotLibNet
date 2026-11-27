// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>A fill with a <see cref="AreaSeries.Where"/> predicate draws a polygon for each run of the data the predicate keeps, as
/// matplotlib 3.11.2's <c>fill_between(x, y1, y2, where=..., step=...)</c> does (<c>FillBetweenPolyCollection</c>,
/// lib/matplotlib/collections.py:1538-1541 and 1573-1584): the regions are found on the data points
/// (<c>cbook.contiguous_regions(where)</c>), and each region's x, upper curve and lower curve are sliced and then stepped together
/// (<c>cbook.STEP_LOOKUP_MAP["steps-" + step](t_slice, f1_slice, f2_slice)</c>). The expected numbers are matplotlib's polygons for
/// x = 0..9, y1 = [1, 3, 2, 5, 4, 6, 3, 7, 5, 8], y2 = [0.5, 1, 1.5, 1, 2, 2.5, 2, 3, 2.5, 3] (or 0), where = x &lt; 4 or x &gt; 6,
/// printed by <c>python -I</c> from <c>PolyCollection.get_paths()</c> (the start and end points, which repeat the lower curve's
/// ends, left out).</summary>
public class FillBetweenWhereTests
{
    private static readonly Rect Bounds = new(0, 0, 400, 300);
    private static readonly double[] X = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];
    private static readonly double[] Y = [1, 3, 2, 5, 4, 6, 3, 7, 5, 8];
    private static readonly double[] Y2 = [0.5, 1, 1.5, 1, 2, 2.5, 2, 3, 2.5, 3];

    // Data 0..10 on 400 x 300 pixels: x = 40 * value, y = 300 - 30 * value.
    private static readonly DataTransform Transform = new(0, 10, 0, 10, Bounds);

    private static RecordingRenderContext Draw(AreaSeries series)
    {
        var ctx = new RecordingRenderContext();
        new AreaSeriesRenderer(new SeriesRenderContext(Transform, ctx, Colors.Tab10Blue, new RenderArea(Bounds, ctx))).Render(series);
        return ctx;
    }

    private static List<Point[]> Polygons(AreaSeries series) => Draw(series).Polygons();

    private static void AssertAt(double x, double y, Point point)
    {
        Assert.Equal(x, point.X, 9);
        Assert.Equal(y, point.Y, 9);
    }

    private static double[] Numbers(string list) =>
        [.. list.Trim('[', ']').Split(", ").Select(s => double.Parse(s, CultureInfo.InvariantCulture))];

    private static AreaSeries Fill(DrawStyle step, bool lower) => new(X, Y)
    {
        YData2 = lower ? Y2 : null,
        StepMode = step,
        Where = (x, _) => x < 4 || x > 6,
    };

    [Theory]
    [InlineData(DrawStyle.Default, true, "[0, 1, 2, 3]|[1, 3, 2, 5]|[0.5, 1, 1.5, 1]", "[7, 8, 9]|[7, 5, 8]|[3, 2.5, 3]")]
    [InlineData(DrawStyle.Default, false, "[0, 1, 2, 3]|[1, 3, 2, 5]|[0, 0, 0, 0]", "[7, 8, 9]|[7, 5, 8]|[0, 0, 0]")]
    [InlineData(DrawStyle.StepsPre, true, "[0, 0, 1, 1, 2, 2, 3]|[1, 3, 3, 2, 2, 5, 5]|[0.5, 1, 1, 1.5, 1.5, 1, 1]", "[7, 7, 8, 8, 9]|[7, 5, 5, 8, 8]|[3, 2.5, 2.5, 3, 3]")]
    [InlineData(DrawStyle.StepsPre, false, "[0, 0, 1, 1, 2, 2, 3]|[1, 3, 3, 2, 2, 5, 5]|[0, 0, 0, 0, 0, 0, 0]", "[7, 7, 8, 8, 9]|[7, 5, 5, 8, 8]|[0, 0, 0, 0, 0]")]
    [InlineData(DrawStyle.StepsMid, true, "[0, 0.5, 0.5, 1.5, 1.5, 2.5, 2.5, 3]|[1, 1, 3, 3, 2, 2, 5, 5]|[0.5, 0.5, 1, 1, 1.5, 1.5, 1, 1]", "[7, 7.5, 7.5, 8.5, 8.5, 9]|[7, 7, 5, 5, 8, 8]|[3, 3, 2.5, 2.5, 3, 3]")]
    [InlineData(DrawStyle.StepsMid, false, "[0, 0.5, 0.5, 1.5, 1.5, 2.5, 2.5, 3]|[1, 1, 3, 3, 2, 2, 5, 5]|[0, 0, 0, 0, 0, 0, 0, 0]", "[7, 7.5, 7.5, 8.5, 8.5, 9]|[7, 7, 5, 5, 8, 8]|[0, 0, 0, 0, 0, 0]")]
    [InlineData(DrawStyle.StepsPost, true, "[0, 1, 1, 2, 2, 3, 3]|[1, 1, 3, 3, 2, 2, 5]|[0.5, 0.5, 1, 1, 1.5, 1.5, 1]", "[7, 8, 8, 9, 9]|[7, 7, 5, 5, 8]|[3, 3, 2.5, 2.5, 3]")]
    [InlineData(DrawStyle.StepsPost, false, "[0, 1, 1, 2, 2, 3, 3]|[1, 1, 3, 3, 2, 2, 5]|[0, 0, 0, 0, 0, 0, 0]", "[7, 8, 8, 9, 9]|[7, 7, 5, 5, 8]|[0, 0, 0, 0, 0]")]
    public void EachRegionTheDataKeeps_IsMatplotlibsPolygon(DrawStyle step, bool lower, string first, string second)
    {
        var polygons = Polygons(Fill(step, lower));

        Assert.Equal(2, polygons.Count);
        foreach (var (polygon, expected) in polygons.Zip([first, second]))
        {
            var parts = expected.Split('|');
            double[] along = Numbers(parts[0]), top = Numbers(parts[1]), bottom = Numbers(parts[2]);
            // The top edge left to right, then the lower edge right to left, under the same x.
            var outline = along.Select((a, i) => new Point(40 * a, 300 - 30 * top[i]))
                .Concat(along.Select((a, i) => new Point(40 * a, 300 - 30 * bottom[i])).Reverse());
            var want = outline.Corners();
            var got = polygon.Corners();
            Assert.Equal(want.Count, got.Count);
            for (int i = 0; i < want.Count; i++) AssertAt(want[i].X, want[i].Y, got[i]);
        }
    }

    /// <summary>A smoothed fill whose second curve is given draws each region the predicate keeps (it threw while drawing before:
    /// the lower curve, one value per data point, was cut by the indices of the smoothed top's samples). The region's top is the
    /// curve drawn through the whole run, from the region's first data point to its last, so the fill stands under the line; its
    /// lower edge is the second curve's points of the region.</summary>
    [Fact]
    public void ASmoothedFillWithASecondCurve_DrawsEachRegionUnderTheLine()
    {
        var series = new AreaSeries(X, Y) { YData2 = Y2, Smooth = true, Where = (x, _) => x < 4 || x > 6 };
        int r = series.SmoothResolution;

        var ctx = Draw(series);
        var polygons = ctx.Polygons();
        var line = Assert.Single(ctx.Polylines());

        Assert.Equal(2, polygons.Count);
        Assert.Equal(line[..(3 * r + 1)], polygons[0][..(3 * r + 1)]);
        Assert.Equal(line[(7 * r)..], polygons[1][..(2 * r + 1)]);
        Assert.Equal([new Point(40 * 3, 300 - 30 * 1), new Point(40 * 2, 300 - 30 * 1.5), new Point(40 * 1, 300 - 30 * 1), new Point(0, 300 - 30 * 0.5)],
            polygons[0][(3 * r + 1)..].Select(p => new Point(Math.Round(p.X, 9), Math.Round(p.Y, 9))));
    }
}
