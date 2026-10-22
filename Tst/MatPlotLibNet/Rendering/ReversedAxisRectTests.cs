// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A rectangle a chart draws between two corners has a positive width and height whichever way its axes run. On
/// an axis that runs backwards (limits set high to low, or <see cref="Axis.Inverted"/>) the corner that is the top left on
/// an upright axis is the bottom right, and a rectangle built as "that corner, then the distance to the other" came out
/// with a negative size, which a browser does not draw: two boxes under <c>SetYLim(15, 0)</c> in a 500 x 300 figure were
/// written -46.2 px high, and 12 of the 83 series types wrote such rectangles with both axes inverted. Every renderer now
/// builds such a rectangle with <see cref="Rect.FromCorners"/>. The sweep at the end renders every series type the library
/// ships with both axes inverted and reads the SVG.</summary>
public class ReversedAxisRectTests
{
    private static readonly Rect Bounds = new(0, 0, 400, 300);

    /// <summary>X from 4 (left edge, 0 px) down to 0 (right edge, 400 px): x lands at 400 - 100 x. Y from 10 (bottom edge,
    /// 300 px) down to 0 (top edge, 0 px): y lands at 30 y. Both axes run backwards.</summary>
    private static DataTransform Backwards() => new(4, 0, 10, 0, Bounds);

    private static RecordingRenderContext Render(DataTransform transform, Action<SeriesRenderContext> draw)
    {
        var ctx = new RecordingRenderContext();
        draw(new SeriesRenderContext(transform, ctx, Colors.Tab10Blue, new RenderArea(Bounds, ctx)));
        return ctx;
    }

    private static object? Arg(CallRecord call, string name) => call.Args!.GetType().GetProperty(name)!.GetValue(call.Args);

    private static List<Rect> Rects(RecordingRenderContext ctx) => [.. ctx.OfKind("DrawRectangle").Select(c => (Rect)Arg(c, "rect")!)];

    private static Rect RectFilled(RecordingRenderContext ctx, Color fill) =>
        ctx.OfKind("DrawRectangle").Where(c => Equals(Arg(c, "fill"), fill)).Select(c => (Rect)Arg(c, "rect")!).Single();

    private static void AssertRect(Rect expected, Rect actual)
    {
        Assert.Equal(expected.X, actual.X, 9);
        Assert.Equal(expected.Y, actual.Y, 9);
        Assert.Equal(expected.Width, actual.Width, 9);
        Assert.Equal(expected.Height, actual.Height, 9);
    }

    // ---- one test per renderer that draws a rectangle between two corners ----------------------------------------------

    /// <summary>A bar of 5 in the first slot (x 0.1 to 0.9): 310 to 390 px across, 0 to 150 px down.</summary>
    [Fact]
    public void ABar_IsTheAreaBetweenItsFootAndItsValue()
    {
        var ctx = Render(Backwards(), c => new BarSeriesRenderer(c).Render(new BarSeries(["a"], [5.0])));

        AssertRect(new Rect(310, 0, 80, 150), Assert.Single(Rects(ctx)));
    }

    /// <summary>A bar that lies down: its value 2 along X (200 to 400 px), its slot along Y (3 to 27 px).</summary>
    [Fact]
    public void ABarThatLiesDown_IsTheAreaBetweenItsFootAndItsValue()
    {
        var ctx = Render(Backwards(), c => new BarSeriesRenderer(c).Render(
            new BarSeries(["a"], [2.0]) { Orientation = BarOrientation.Horizontal }));

        AssertRect(new Rect(200, 3, 200, 24), Assert.Single(Rects(ctx)));
    }

    /// <summary>A box of 1..5 at position 0: quartiles 2 and 4 (60 to 120 px), half a width of 0.25 either side (375 to
    /// 425 px).</summary>
    [Fact]
    public void ABox_IsTheAreaBetweenItsQuartiles()
    {
        var ctx = Render(Backwards(), c => new BoxSeriesRenderer(c).Render(new BoxSeries([[1.0, 2, 3, 4, 5]])));

        AssertRect(new Rect(375, 60, 50, 60), Assert.Single(Rects(ctx)));
    }

    /// <summary>The same box lying down: quartiles along X (0 to 200 px), the position along Y (-7.5 to 7.5 px).</summary>
    [Fact]
    public void ABoxThatLiesDown_IsTheAreaBetweenItsQuartiles()
    {
        var ctx = Render(Backwards(), c => new BoxSeriesRenderer(c).Render(new BoxSeries([[1.0, 2, 3, 4, 5]]) { Vert = false }));

        AssertRect(new Rect(0, -7.5, 200, 15), Assert.Single(Rects(ctx)));
    }

    /// <summary>Bins 1..2 and 2..3 hold 2 and 1 values: 200 to 300 px across and 0 to 60 px down, 100 to 200 px and 0 to
    /// 30 px.</summary>
    [Fact]
    public void AHistogramsBars_AreTheAreasOfTheirBins()
    {
        var ctx = Render(Backwards(), c => new HistogramSeriesRenderer(c).Render(new HistogramSeries([1.0, 1, 3]) { Bins = 2 }));

        var bars = Rects(ctx);
        Assert.Equal(2, bars.Count);
        AssertRect(new Rect(200, 0, 100, 60), bars[0]);
        AssertRect(new Rect(100, 0, 100, 30), bars[1]);
    }

    /// <summary>Two a's and one b: the a bar counts 2 in the first slot, upright and lying down.</summary>
    [Theory]
    [InlineData(BarOrientation.Vertical, 310, 0, 80, 60)]
    [InlineData(BarOrientation.Horizontal, 200, 3, 200, 24)]
    public void ACountPlotsBar_IsTheAreaBetweenZeroAndItsCount(BarOrientation orientation, double x, double y, double w, double h)
    {
        var ctx = Render(Backwards(), c => new CountSeriesRenderer(c).Render(
            new CountSeries(["a", "a", "b"]) { Orientation = orientation }));

        AssertRect(new Rect(x, y, w, h), Rects(ctx)[0]);
    }

    /// <summary>A step up from 0 to 4 (slot 0.2 to 0.8), then down to 3 (slot 1.2 to 1.8).</summary>
    [Fact]
    public void AWaterfallsSteps_AreTheAreasBetweenTheTotals()
    {
        var ctx = Render(Backwards(), c => new WaterfallSeriesRenderer(c).Render(new WaterfallSeries(["a", "b"], [4.0, -1])));

        var steps = Rects(ctx);
        Assert.Equal(2, steps.Count);
        AssertRect(new Rect(320, 0, 60, 120), steps[0]);
        AssertRect(new Rect(220, 90, 60, 30), steps[1]);
    }

    /// <summary>Open 2, close 4: the body runs 60 to 120 px down, 0.6 wide around the slot's centre 0.5.</summary>
    [Fact]
    public void ACandlesBody_IsTheAreaBetweenItsOpenAndItsClose()
    {
        var ctx = Render(Backwards(), c => new CandlestickSeriesRenderer(c).Render(new CandlestickSeries([2.0], [5.0], [1.0], [4.0])));

        AssertRect(new Rect(320, 60, 60, 60), Assert.Single(Rects(ctx)));
    }

    /// <summary>A task from 1 to 3 (100 to 300 px) on row 0, 0.6 high (-9 to 9 px).</summary>
    [Fact]
    public void AGanttBar_IsTheAreaBetweenItsStartAndItsEnd()
    {
        var ctx = Render(Backwards(), c => new GanttSeriesRenderer(c).Render(new GanttSeries(["t"], [1.0], [3.0])));

        AssertRect(new Rect(100, -9, 200, 18), Assert.Single(Rects(ctx)));
    }

    /// <summary>A segment from 1, 2 wide (100 to 300 px) on row 0, 0.8 high (-12 to 12 px).</summary>
    [Fact]
    public void ABrokenBarSegment_IsTheAreaItCovers()
    {
        var ctx = Render(Backwards(), c => new BrokenBarSeriesRenderer(c).Render(new BrokenBarSeries([[new BarRange(1.0, 2.0)]])));

        AssertRect(new Rect(100, -12, 200, 24), Assert.Single(Rects(ctx)));
    }

    /// <summary>One cell from x 0 to 1 (300 to 400 px) and y 0 to 2 (0 to 60 px).</summary>
    [Fact]
    public void APcolormeshCell_IsTheAreaBetweenItsEdges()
    {
        var ctx = Render(Backwards(), c => new PcolormeshSeriesRenderer(c).Render(
            new PcolormeshSeries(new double[] { 0.0, 1.0 }, new double[] { 0.0, 2.0 }, new double[,] { { 1 } })));

        AssertRect(new Rect(300, 0, 100, 60), Assert.Single(Rects(ctx)));
    }

    [Fact]
    public void ASpectrogramsCells_HaveAPositiveSize()
    {
        double[] signal = [.. Enumerable.Range(0, 64).Select(i => (i % 8) / 8.0 - 0.5)];
        var ctx = Render(Backwards(), c => new SpectrogramSeriesRenderer(c).Render(
            new SpectrogramSeries(signal) { WindowSize = 16, Overlap = 8 }));

        var cells = Rects(ctx);
        Assert.NotEmpty(cells);
        Assert.All(cells, cell => Assert.True(cell.Width > 0 && cell.Height > 0, $"a cell of {cell.Width} x {cell.Height}"));
    }

    /// <summary>A state from 0 to 2 covers 200 to 400 px across the full height.</summary>
    [Fact]
    public void AStateSegment_IsTheAreaBetweenItsStartAndItsEnd()
    {
        var ctx = Render(Backwards(), c => new StateTimelineSeriesRenderer(c).Render(
            new StateTimelineSeries([new StateSegment(0, 2, "Up", Colors.Tab10Green)])));

        AssertRect(new Rect(200, 0, 200, 300), Assert.Single(Rects(ctx)));
    }

    // ---- relative rotation quadrants ------------------------------------------------------------------------------------

    private static readonly Color Leading = new(0, 180, 0, 20);
    private static readonly Color Weakening = new(230, 200, 0, 20);
    private static readonly Color Lagging = new(200, 0, 0, 20);
    private static readonly Color Improving = new(0, 100, 200, 20);

    private static RecordingRenderContext Quadrants(DataTransform transform) =>
        Render(transform, c => new RelativeRotationSeriesRenderer(c).Render(new RelativeRotationSeries([], [], [])));

    /// <summary>On axes from 102 down to 98 both ways the centre (100, 100) stands at (200, 150) px, the higher RS-Ratio on
    /// the left and the higher RS-Momentum below: the leading quadrant (both above 100) is the lower left one.</summary>
    [Fact]
    public void TheQuadrantsOfARotationGraph_FollowAxesThatRunBackwards()
    {
        var ctx = Quadrants(new DataTransform(102, 98, 102, 98, Bounds));

        AssertRect(new Rect(0, 150, 200, 150), RectFilled(ctx, Leading));
        AssertRect(new Rect(0, 0, 200, 150), RectFilled(ctx, Weakening));
        AssertRect(new Rect(200, 0, 200, 150), RectFilled(ctx, Lagging));
        AssertRect(new Rect(200, 150, 200, 150), RectFilled(ctx, Improving));
    }

    /// <summary>On axes from 101 to 105 the centre (100, 100) lies left of and below the plot, so the whole plot leads; on
    /// axes from 95 to 99 it lies right of and above it, so the whole plot lags. The other three quadrants cover none of it
    /// (before, they were written thousands of pixels wide, with negative sizes), and neither crosshair is drawn outside
    /// the plot.</summary>
    [Theory]
    [InlineData(101.0, 105.0, true)]
    [InlineData(95.0, 99.0, false)]
    public void TheQuadrantsOfARotationGraph_StayInsideThePlot_WhenItsCentreIsOutside(double from, double to, bool leads)
    {
        var ctx = Quadrants(new DataTransform(from, to, from, to, Bounds));

        var whole = leads ? Leading : Lagging;
        AssertRect(Bounds, RectFilled(ctx, whole));
        foreach (var other in new[] { Leading, Weakening, Lagging, Improving }.Where(c => c != whole))
        {
            var quadrant = RectFilled(ctx, other);
            Assert.True(quadrant.Width >= 0 && quadrant.Height >= 0, $"a quadrant of {quadrant.Width} x {quadrant.Height}");
            Assert.Equal(0, quadrant.Width * quadrant.Height, 9);
        }
        Assert.Empty(ctx.OfKind("DrawLine"));
    }

    /// <summary>On upright axes from 98 to 102 the quadrants stand where they always stood: leading upper right.</summary>
    [Fact]
    public void TheQuadrantsOfARotationGraph_OnUprightAxes_AreTheFamiliarOnes()
    {
        var ctx = Quadrants(new DataTransform(98, 102, 98, 102, Bounds));

        AssertRect(new Rect(200, 0, 200, 150), RectFilled(ctx, Leading));
        AssertRect(new Rect(200, 150, 200, 150), RectFilled(ctx, Weakening));
        AssertRect(new Rect(0, 150, 200, 150), RectFilled(ctx, Lagging));
        AssertRect(new Rect(0, 0, 200, 150), RectFilled(ctx, Improving));
        Assert.Equal(2, ctx.CountOf("DrawLine"));
    }

    // ---- bullet graph bands ---------------------------------------------------------------------------------------------

    /// <summary>Bands out of order (80, then 40) on a strip up to 50: the second band runs from the end of the strip back to
    /// 0.8 of it. Lying down and upright it covers the same share of the strip; lying down it covered none.</summary>
    [Fact]
    public void ABulletGraphBand_CoversTheSameShare_LyingDownAndUpright()
    {
        GaugeBand[] bands = [new(80, Colors.Gray), new(40, Colors.LightGray)];
        var lying = Render(Backwards(), c => new BulletGraphSeriesRenderer(c).Render(new BulletGraphSeries(50) { Bands = bands }));
        var upright = Render(Backwards(), c => new BulletGraphSeriesRenderer(c).Render(
            new BulletGraphSeries(50) { Bands = bands, Orientation = Orientation.Vertical }));

        AssertRect(new Rect(320, 0, 80, 300), Rects(lying)[1]);
        AssertRect(new Rect(0, 0, 400, 60), Rects(upright)[1]);
    }

    // ---- the sweep ------------------------------------------------------------------------------------------------------

    private static readonly Regex RectSize = new(
        "<rect\\b[^>]*?\\bwidth=\"([^\"]+)\"[^>]*?\\bheight=\"([^\"]+)\"", RegexOptions.Compiled);

    /// <summary>The rectangles of <paramref name="svg"/> whose width or height is negative, as "width x height".</summary>
    internal static List<string> NegativeRects(string svg) => RectSize.Matches(svg)
        .Select(m => (W: double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
                      H: double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture)))
        .Where(r => r.W < 0 || r.H < 0)
        .Select(r => string.Create(CultureInfo.InvariantCulture, $"{r.W} x {r.H}"))
        .ToList();

    /// <summary>Every series type alone on axes whose X and Y both run backwards writes no rectangle of negative size.</summary>
    [Theory]
    [MemberData(nameof(AllSeriesTests.AllSeriesInstances), MemberType = typeof(AllSeriesTests))]
    public void NoSeries_WritesARectangleOfNegativeSize_WhenBothAxesRunBackwards(ISeries series, string name)
    {
        var figure = Plt.Create().WithSize(400, 300).AddSubPlot(1, 1, 1, ax => ax.AddSeries(series)).Build();
        figure.SubPlots[0].XAxis.Inverted = true;
        figure.SubPlots[0].YAxis.Inverted = true;

        var negative = NegativeRects(figure.ToSvg());

        Assert.True(negative.Count == 0, $"{name} wrote {negative.Count} rectangle(s) of negative size: {string.Join(", ", negative)}");
    }

    /// <summary>The figures the defect was measured on: two boxes under <c>SetYLim(15, 0)</c>, two lying boxes under
    /// <c>SetYLim(2, -1)</c>, and the same boxes on limits set low to high.</summary>
    [Theory]
    [InlineData(true, 15.0, 0.0)]
    [InlineData(false, 2.0, -1.0)]
    [InlineData(true, 0.0, 15.0)]
    public void TwoBoxes_OnLimitsSetEitherWay_WriteNoRectangleOfNegativeSize(bool upright, double from, double to)
    {
        double[][] data = [[1, 2, 3, 4, 5, 6, 7], [3, 4, 5, 6, 7, 8, 9]];
        string svg = Plt.Create().WithSize(500, 300)
            .AddSubPlot(1, 1, 1, ax => ax.BoxPlot(data, s => s.Vert = upright).SetYLim(from, to))
            .Build().ToSvg();

        Assert.Empty(NegativeRects(svg));
    }
}
