// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>On a log axis a mark through a value the axis cannot place (0 or below) is drawn as matplotlib draws it: its log
/// scale clips such a value (<c>nonpositive='clip'</c>), putting it a thousand decades beyond the axis, and the plot cuts every
/// mark at the edge where the axis begins. So a bar, a stem or an error bar that runs to such a value runs to that edge, a line
/// dives to it and climbs back, a fill runs along it, and a mark with no extent there (a marker, a dot) is never seen. A mark
/// wholly beyond the axis (a bar from 0 to a negative value) is not drawn. A symmetric-log axis and a linear axis place 0 and
/// are unchanged.</summary>
public class LogAxisClipTests
{
    private static readonly Rect Bounds = new(0, 0, 400, 300);

    /// <summary>The bottom edge of <see cref="Bounds"/>: where a log Y axis that is not reversed begins.</summary>
    private const double BottomEdge = 300;

    private static double PixelY(double value) => Bounds.LogY().DataToPixel(0, value).Y;

    private static double PixelX(double value) => Bounds.LogX().DataToPixel(value, 0).X;

    private static RecordingRenderContext Render(DataTransform transform, Action<SeriesRenderContext> draw)
    {
        var ctx = new RecordingRenderContext();
        draw(new SeriesRenderContext(transform, ctx, Colors.Tab10Blue, new RenderArea(Bounds, ctx)));
        return ctx;
    }

    // ---- what a render context was handed ---------------------------------------------------------------------------

    /// <summary>The lower and upper pixel Y a rectangle covers, whichever way round it was handed.</summary>
    private static double Low(Rect r) => Math.Max(r.Y, r.Y + r.Height);

    private static double High(Rect r) => Math.Min(r.Y, r.Y + r.Height);

    // ---- bars ---------------------------------------------------------------------------------------------------------

    [Fact]
    public void ABar_RunsFromTheBottomEdgeToWhereTheLogScalePutsItsValue()
    {
        var ctx = Render(Bounds.LogY(), c => new BarSeriesRenderer(c).Render(new BarSeries(["a", "b"], [10.0, 50])));

        ctx.AssertNothingUndrawableReached();
        var bars = ctx.Rects();
        Assert.Equal(2, bars.Count);
        Assert.Equal(BottomEdge, Low(bars[0]), 9);
        Assert.Equal(PixelY(10), High(bars[0]), 9);
        Assert.Equal(BottomEdge, Low(bars[1]), 9);
        Assert.Equal(PixelY(50), High(bars[1]), 9);
    }

    [Fact]
    public void AHorizontalBar_RunsFromTheLeftEdgeToWhereTheLogScalePutsItsValue()
    {
        var ctx = Render(Bounds.LogX(), c => new BarSeriesRenderer(c).Render(
            new BarSeries(["a", "b"], [10.0, 50]) { Orientation = BarOrientation.Horizontal }));

        ctx.AssertNothingUndrawableReached();
        var bars = ctx.Rects();
        Assert.Equal(2, bars.Count);
        Assert.Equal(0, bars[0].X, 9);
        Assert.Equal(PixelX(10), bars[0].X + bars[0].Width, 9);
        Assert.Equal(PixelX(50), bars[1].X + bars[1].Width, 9);
    }

    /// <summary>A reversed axis begins on the other edge, and so does what it clips.</summary>
    [Fact]
    public void OnAReversedLogAxis_ABar_RunsFromTheEdgeWhereTheAxisBegins()
    {
        var ctx = Render(Bounds.LogYReversed(), c => new BarSeriesRenderer(c).Render(new BarSeries(["a"], [10.0])));

        var bar = Assert.Single(ctx.Rects());
        Assert.Equal(0, High(bar), 9);
        Assert.Equal(Bounds.LogYReversed().DataToPixel(0, 10).Y, Low(bar), 9);
    }

    /// <summary>A negative bar, and a bar of 0, reach a value the log axis cannot place: masked, with its label.</summary>
    [Fact]
    public void ABarWhoseValueTheLogAxisCannotPlace_IsSkippedWithItsLabel_TheOthersStandOnTheBottomEdge()
    {
        var ctx = Render(Bounds.LogY(), c => new BarSeriesRenderer(c).Render(
            new BarSeries(["a", "b", "c"], [10.0, -5, 0]) { ShowLabels = true }));

        ctx.AssertNothingUndrawableReached();
        var bar = Assert.Single(ctx.Rects());
        Assert.Equal(BottomEdge, Low(bar), 9);
        var label = Assert.Single(ctx.Of<DrawTextCall>());
        Assert.Equal("10", label.Text);
    }

    /// <summary>A bar that stands on a base the axis can place stands on it, not on the edge.</summary>
    [Fact]
    public void ABarOnABaseTheLogAxisCanPlace_StandsOnThatBase()
    {
        var ctx = Render(Bounds.LogY(), c => new BarSeriesRenderer(c).Render(new BarSeries(["a"], [10.0]) { StackBaseline = [2.0] }));

        var bar = Assert.Single(ctx.Rects());
        Assert.Equal(PixelY(2), Low(bar), 9);
        Assert.Equal(PixelY(12), High(bar), 9);
    }

    /// <summary>A symmetric-log axis places 0: a bar stands on it, in the middle of the plot, as before.</summary>
    [Fact]
    public void OnASymlogAxis_ABar_StandsOnZero_NotOnTheEdge()
    {
        var ctx = Render(Bounds.SymLogY(), c => new BarSeriesRenderer(c).Render(new BarSeries(["a"], [10.0])));

        var bar = Assert.Single(ctx.Rects());
        double zero = Bounds.SymLogY().DataToPixel(0, 0).Y;
        Assert.Equal(zero, Low(bar), 9);
        Assert.NotEqual(BottomEdge, zero, 3);
    }

    // ---- histogram and count plot -------------------------------------------------------------------------------------

    /// <summary>Bins 1..2, 2..3 and 3..4 hold 2, 0 and 2 values: two bars on the bottom edge; the empty bin's bar has no
    /// height a log axis can place and is skipped.</summary>
    [Fact]
    public void AHistogramsBars_StandOnTheBottomEdge_AnEmptyBinIsSkipped()
    {
        var ctx = Render(Bounds.LogY(), c => new HistogramSeriesRenderer(c).Render(new HistogramSeries([1.0, 1, 3.5, 4]) { Bins = 3 }));

        ctx.AssertNothingUndrawableReached();
        var bars = ctx.Rects();
        Assert.Equal(2, bars.Count);
        Assert.All(bars, b => Assert.Equal(BottomEdge, Low(b), 9));
        Assert.All(bars, b => Assert.Equal(PixelY(2), High(b), 9));
    }

    /// <summary>A step histogram's outline comes down to the bottom edge at its ends. An empty bin's top lies a thousand decades
    /// below a log axis, as matplotlib's log scale puts it: the line comes down to the edge there and goes up again, two lines.</summary>
    [Fact]
    public void AStepHistogram_ComesDownToTheBottomEdge_AtAnEmptyBinToo()
    {
        var ctx = Render(Bounds.LogY(), c => new HistogramSeriesRenderer(c).Render(
            new HistogramSeries([1.0, 1, 3.5, 4]) { Bins = 3, HistType = HistType.Step }));

        ctx.AssertNothingUndrawableReached();
        var outlines = ctx.Polylines();
        Assert.Equal(2, outlines.Count);
        Assert.All(outlines, o => Assert.Equal(BottomEdge, o[0].Y, 9));
        Assert.All(outlines, o => Assert.Equal(BottomEdge, o[^1].Y, 9));
        Assert.All(outlines, o => Assert.Contains(o.Skip(1), p => Math.Abs(p.Y - PixelY(2)) < 1e-9));
    }

    /// <summary>A filled step histogram is one patch in matplotlib: an empty bin on a log axis takes it down to the bottom edge,
    /// where it runs along the edge to the next bin, one fill; before, the empty bin broke it into two.</summary>
    [Fact]
    public void AFilledStepHistogram_RunsAlongTheBottomEdgeUnderAnEmptyBin_AsOneFill()
    {
        var ctx = Render(Bounds.LogY(), c => new HistogramSeriesRenderer(c).Render(
            new HistogramSeries([1.0, 1, 3.5, 4]) { Bins = 3, HistType = HistType.StepFilled }));

        ctx.AssertNothingUndrawableReached();
        AssertNothingBelowThePlot(ctx);
        var fill = Assert.Single(ctx.Polygons());
        Assert.Equal(BottomEdge, fill[0].Y, 9);
        Assert.Equal(4, fill.Count(p => Math.Abs(p.Y - PixelY(2)) < 1e-9));   // the two full bins' tops
    }

    [Fact]
    public void AStepHistogramWithoutAnEmptyBin_IsOneOutline()
    {
        var ctx = Render(Bounds.LogY(), c => new HistogramSeriesRenderer(c).Render(
            new HistogramSeries([1.0, 2.5, 3.5]) { Bins = 3, HistType = HistType.StepFilled }));

        var outline = Assert.Single(ctx.Polygons());
        Assert.Equal(9, outline.Length);   // base, 3 bins x 2 tops, base, closing base
        Assert.Equal(BottomEdge, outline[0].Y, 9);
        Assert.Equal(BottomEdge, outline[^2].Y, 9);
        Assert.Equal(outline[0], outline[^1]);
    }

    [Theory]
    [InlineData(BarOrientation.Vertical)]
    [InlineData(BarOrientation.Horizontal)]
    public void ACountPlot_StandsOnTheEdgeWhereTheLogAxisBegins(BarOrientation orientation)
    {
        bool vertical = orientation == BarOrientation.Vertical;
        var ctx = Render(vertical ? Bounds.LogY() : Bounds.LogX(), c => new CountSeriesRenderer(c).Render(
            new CountSeries(["a", "b", "b"]) { Orientation = orientation }));

        ctx.AssertNothingUndrawableReached();
        var bars = ctx.Rects();
        Assert.Equal(2, bars.Count);
        if (vertical)
            Assert.All(bars, b => Assert.Equal(BottomEdge, Low(b), 9));
        else
            Assert.All(bars, b => Assert.Equal(0, b.X, 9));
    }

    /// <summary>A count bar stands centred in its category's slot, so a bar 1.2 wide in the first slot spans -0.1..1.1, and -0.1
    /// lies a thousand decades left of a log X axis. matplotlib 3.11.2's <c>ax.bar([0.5, 2], [3, 4], width=1.2)</c> on a log X
    /// axis gives the first bar the rectangle from x <c>-1131506.25</c> to <c>96.14</c> (printed): it is cut at the left edge, where
    /// the library skipped it.</summary>
    [Fact]
    public void ACountBarReachingWhereTheLogXAxisCannotPlace_IsCutAtTheLeftEdge()
    {
        var ctx = Render(Bounds.LogX(), c => new CountSeriesRenderer(c).Render(new CountSeries(["a", "b", "b"]) { BarWidth = 1.2 }));

        ctx.AssertNothingUndrawableReached();
        var bars = ctx.Rects();
        Assert.Equal(2, bars.Count);
        Assert.Equal(0, bars[0].X, 9);
        Assert.Equal(PixelX(1.1), bars[0].X + bars[0].Width, 9);
    }

    // ---- areas and stacks ---------------------------------------------------------------------------------------------

    public static TheoryData<string> AreaKinds { get; } = new("zero baseline", "zero lower edge", "steps", "where");

    private static AreaSeries Area(string kind) => kind switch
    {
        "zero baseline" => new AreaSeries([0.0, 1, 2], [2.0, 10, 50]),
        "zero lower edge" => new AreaSeries([0.0, 1, 2], [2.0, 10, 50]) { YData2 = [0.0, 0, 0] },
        "steps" => new AreaSeries([0.0, 1, 2], [2.0, 10, 50]) { StepMode = DrawStyle.StepsPost },
        _ => new AreaSeries([0.0, 1, 2], [2.0, 10, 50]) { Where = (_, y) => y > 5 },
    };

    [Theory]
    [MemberData(nameof(AreaKinds))]
    public void AnArea_IsFilledDownToTheBottomEdge(string kind)
    {
        var ctx = Render(Bounds.LogY(), c => new AreaSeriesRenderer(c).Render(Area(kind)));

        ctx.AssertNothingUndrawableReached();
        var fill = Assert.Single(ctx.Polygons());
        Assert.All(fill, p => Assert.True(p.Y <= BottomEdge + 1e-9, $"{p} lies below the plot"));
        Assert.True(fill.Count(p => Math.Abs(p.Y - BottomEdge) < 1e-9) >= 2, "the fill reaches the bottom edge");
        Assert.Contains(fill, p => Math.Abs(p.Y - PixelY(50)) < 1e-9);
    }

    /// <summary>The top of an area is data: matplotlib 3.11.2's <c>fill_between([1, 2, 3], [1, 0, 10])</c> on a log Y axis is one
    /// polygon whose top dives to <c>-279942</c> at the 0 (printed). The fill is one polygon cut at the bottom edge, and the line
    /// along its top dives to the edge and climbs from it, as a line does; before, the area was broken into two.</summary>
    [Fact]
    public void AnAreaWhoseTopTheLogAxisCannotPlace_DivesToTheBottomEdge_AsOneFill()
    {
        var ctx = Render(Bounds.LogY(), c => new AreaSeriesRenderer(c).Render(new AreaSeries([0.0, 1, 2, 3, 4], [2.0, 3, -2, 3, 4])));

        ctx.AssertNothingUndrawableReached();
        AssertNothingBelowThePlot(ctx);
        var fill = Assert.Single(ctx.Polygons());
        Assert.Equal(PixelY(2), fill[0].Y, 9);
        var lines = ctx.Polylines();
        Assert.Equal(2, lines.Count);
        Assert.Equal(BottomEdge, lines[0][^1].Y, 9);
        Assert.Equal(BottomEdge, lines[1][0].Y, 9);
    }

    /// <summary>matplotlib 3.11.2's <c>fill_between([1, 2, 3], [2, 0, 10], [1, 1, 1])</c> on a log Y axis fills from the lower
    /// edge down to the bottom of the plot around the 0 (its top dives there, printed <c>[307.5, -279942.0]</c>): the fill reaches
    /// the bottom edge twice and keeps its lower edge. Before, the 0 broke it into two runs of one point each.</summary>
    [Fact]
    public void AnAreaOverALowerEdge_WhoseTopTheLogAxisCannotPlace_IsFilledDownToTheBottomEdgeThere()
    {
        var ctx = Render(Bounds.LogY(), c => new AreaSeriesRenderer(c).Render(new AreaSeries([1.0, 2, 3], [20.0, 0, 20]) { YData2 = [5.0, 5, 5] }));

        ctx.AssertNothingUndrawableReached();
        AssertNothingBelowThePlot(ctx);
        var fill = Assert.Single(ctx.Polygons());
        Assert.Equal(2, fill.Count(p => p.Y == BottomEdge));
        Assert.Contains(new Point(80, PixelY(5)), fill);
    }

    /// <summary>A lower edge is a curve as the top is: one that dips to 0 dives to the bottom edge, as matplotlib's fill_between
    /// draws it, where clipping that one point to the edge drew a V.</summary>
    [Fact]
    public void AnAreasLowerEdgeAtAValueTheLogAxisCannotPlace_DivesToTheBottomEdge_NotAV()
    {
        var ctx = Render(Bounds.LogY(), c => new AreaSeriesRenderer(c).Render(new AreaSeries([1.0, 2, 3], [20.0, 20, 20]) { YData2 = [5.0, 0, 5] }));

        var fill = Assert.Single(ctx.Polygons());
        Assert.Equal(2, fill.Count(p => p.Y == BottomEdge));
        Assert.DoesNotContain(new Point(80, BottomEdge), fill);
    }

    /// <summary>A stack is matplotlib's fill_between layer on layer: a layer whose top reaches 0 is one fill cut at the bottom
    /// edge, and the layer above stands on that top, so its fill reaches the edge there too.</summary>
    [Fact]
    public void AStackedLayerThroughAValueTheLogAxisCannotPlace_IsOneFillCutAtTheBottomEdge_AndTheNextStandsOnIt()
    {
        var ctx = Render(Bounds.LogY(), c => new StackedAreaSeriesRenderer(c).Render(
            new StackedAreaSeries([1.0, 2, 3], [[2.0, 0, 3], [2.0, 2, 2]])));

        ctx.AssertNothingUndrawableReached();
        AssertNothingBelowThePlot(ctx);
        var layers = ctx.Polygons();
        Assert.Equal(2, layers.Count);
        Assert.Equal(2, layers[1].Count(p => p.Y == BottomEdge));
        Assert.DoesNotContain(new Point(80, BottomEdge), layers[1]);
    }

    [Fact]
    public void AStackedArea_ItsFirstLayerStandsOnTheBottomEdge_TheNextOnTheLayerBelow()
    {
        var ctx = Render(Bounds.LogY(), c => new StackedAreaSeriesRenderer(c).Render(
            new StackedAreaSeries([0.0, 1, 2], [[1.0, 2, 3], [1.0, 1, 1]])));

        ctx.AssertNothingUndrawableReached();
        var layers = ctx.Polygons();
        Assert.Equal(2, layers.Count);
        Assert.All(layers[0].Skip(3), p => Assert.Equal(BottomEdge, p.Y, 9));
        double[] firstTops = [.. layers[0].Take(3).Select(p => p.Y).Reverse()];
        double[] secondBases = [.. layers[1].Skip(3).Select(p => p.Y)];
        Assert.All(Enumerable.Range(0, 3), i => Assert.Equal(firstTops[i], secondBases[i], 9));
    }

    [Fact]
    public void ADensityCurve_IsFilledDownToTheBottomEdge()
    {
        var ctx = Render(Bounds.LogY(), c => new KdeSeriesRenderer(c).Render(new KdeSeries([1.0, 2, 3, 5, 8])));

        ctx.AssertNothingUndrawableReached();
        var fill = Assert.Single(ctx.Polygons());
        Assert.Equal(BottomEdge, fill[^1].Y, 9);
        Assert.Equal(BottomEdge, fill[^2].Y, 9);
    }

    /// <summary>An ECDF starts at 0, which a log axis puts a thousand decades below it: matplotlib 3.11.2's
    /// <c>ax.ecdf([1, 2, 3, 4])</c> on a log Y axis is the path <c>[[96.14, -486864.12], [96.14, -486864.12], [96.14, 58.67],
    /// ...]</c>, so its first step rises from the bottom edge.</summary>
    [Fact]
    public void AnEcdf_RisesFromTheBottomEdge_AtItsFirstStep()
    {
        // A log Y axis from 0.1 (the bottom edge) to 1 (the top edge), so every value of the ECDF lies on the plot.
        var transform = Bounds.LogY(yMin: 0.1, yMax: 1);
        var ctx = Render(transform, c => new EcdfSeriesRenderer(c).Render(new EcdfSeries([1.0, 2, 3])));

        ctx.AssertNothingUndrawableReached();
        var line = Assert.Single(ctx.Polylines());
        Assert.Equal(new Point(40, BottomEdge), line[0]);
        Assert.Equal(transform.DataToPixel(1, 1.0 / 3), line[1]);
    }

    // ---- lines --------------------------------------------------------------------------------------------------------

    /// <summary>Where the segment from <paramref name="a"/> to <paramref name="b"/> crosses the bottom edge.</summary>
    private static Point OnBottomEdge(Point a, Point b) => new(a.X + (BottomEdge - a.Y) / (b.Y - a.Y) * (b.X - a.X), BottomEdge);

    /// <summary>A thousand decades below 1 on <see cref="LogY"/>: where matplotlib's log scale puts 0 or a negative value.</summary>
    private const double BeyondY = BottomEdge + 1000 * 150.0;

    /// <summary>No point a context was handed lies below the bottom edge of the plot.</summary>
    private static void AssertNothingBelowThePlot(RecordingRenderContext ctx)
    {
        foreach (var points in ctx.Polylines().Concat(ctx.Polygons()))
            Assert.All(points, p => Assert.True(p.Y <= BottomEdge + 1e-9, $"{p} lies below the plot"));
    }

    /// <summary>matplotlib 3.11.2: <c>ax.plot([1, 2, 3], [1, 0, 10]); ax.set_yscale('log')</c> gives the line pixels
    /// <c>[[96.14, 58.0], [307.5, -279942.0], [518.86, 338.0]]</c>: the line dives to the bottom edge and climbs from it, where
    /// the library broke it and drew nothing between the two points.</summary>
    [Fact]
    public void ALine_DivesToTheBottomEdgeAtAValueTheLogAxisCannotPlace_AndClimbsFromIt()
    {
        var ctx = Render(Bounds.LogY(), c => new LineSeriesRenderer(c).Render(new LineSeries([1.0, 2, 3], [10.0, 0, 10])));

        ctx.AssertNothingUndrawableReached();
        var lines = ctx.Polylines();
        Assert.Equal(2, lines.Count);
        Point a = new(40, PixelY(10)), beyond = new(80, BeyondY), b = new(120, PixelY(10));
        a.AssertCloseTo(lines[0][0], 1e-9);
        OnBottomEdge(a, beyond).AssertCloseTo(lines[0][1], 1e-9);
        OnBottomEdge(beyond, b).AssertCloseTo(lines[1][0], 1e-9);
        b.AssertCloseTo(lines[1][1], 1e-9);
    }

    /// <summary>A marker has no extent: at a value the log axis cannot place it stands a thousand decades below the plot, where
    /// nothing of it is seen.</summary>
    [Fact]
    public void ALinesMarker_AtAValueTheLogAxisCannotPlace_IsNotDrawn()
    {
        var ctx = Render(Bounds.LogY(), c => new LineSeriesRenderer(c).Render(
            new LineSeries([1.0, 2, 3], [10.0, 0, 10]) { Marker = MarkerStyle.Circle }));

        ctx.AssertNothingUndrawableReached();
        Assert.Equal(2, ctx.Count<DrawCircleCall>());
        Assert.All(ctx.Of<DrawCircleCall>(), c => Assert.True(c.Center.Y <= BottomEdge));
    }

    /// <summary>matplotlib 3.11.2: <c>ax.step([1, 2, 3], [1, 0, 10])</c> on a log Y axis (where='pre') is
    /// <c>[[96.14, 58.0], [96.14, -279942.0], [307.5, -279942.0], [307.5, 338.0], [518.86, 338.0]]</c>: a drop straight down
    /// to the bottom edge, then a rise straight up from it.</summary>
    [Fact]
    public void AStep_DropsToTheBottomEdge_AndRisesFromIt()
    {
        var ctx = Render(Bounds.LogY(), c => new StepSeriesRenderer(c).Render(
            new StepSeries([1.0, 2, 3], [10.0, 0, 10]) { StepPosition = StepPosition.Pre }));

        ctx.AssertNothingUndrawableReached();
        var lines = ctx.Polylines();
        Assert.Equal(2, lines.Count);
        Assert.Equal([new Point(40, PixelY(10)), new Point(40, BottomEdge)], lines[0]);
        Assert.Equal([new Point(80, BottomEdge), new Point(80, PixelY(10)), new Point(120, PixelY(10))], lines[1]);
    }

    [Fact]
    public void ASignal_DivesToTheBottomEdgeAtAValueTheLogAxisCannotPlace()
    {
        var ctx = Render(Bounds.LogY(), c => new MonotonicLineRenderer<SignalSeries>(c).Render(new SignalSeries([10.0, 0, 10], 1.0, 1.0)));

        ctx.AssertNothingUndrawableReached();
        AssertNothingBelowThePlot(ctx);
        Assert.Equal(2, ctx.Polylines().Count);
    }

    [Fact]
    public void ASignalXY_DivesToTheBottomEdgeAtAValueTheLogAxisCannotPlace()
    {
        var ctx = Render(Bounds.LogY(), c => new MonotonicLineRenderer<SignalXYSeries>(c).Render(new SignalXYSeries([1.0, 2, 3], [10.0, -1, 10])));

        ctx.AssertNothingUndrawableReached();
        AssertNothingBelowThePlot(ctx);
        Assert.Equal(2, ctx.Polylines().Count);
    }

    /// <summary>A fit through 10, 5 and -2 runs below 0 on its right: the line reaches the bottom edge there, and the band
    /// (filled) runs along it; before, the render context was handed NaN.</summary>
    [Fact]
    public void ARegressionLineAndItsBand_ReachTheBottomEdge_WhereTheFitFallsBelowZero()
    {
        var ctx = Render(Bounds.LogY(), c => new RegressionSeriesRenderer(c).Render(
            new RegressionSeries([1.0, 2, 3], [10.0, 5, -2]) { ShowConfidence = true }));

        ctx.AssertNothingUndrawableReached();
        AssertNothingBelowThePlot(ctx);
        var line = Assert.Single(ctx.Polylines());
        Assert.Equal(BottomEdge, line[^1].Y, 9);
        Assert.Contains(Assert.Single(ctx.Polygons()), p => Math.Abs(p.Y - BottomEdge) < 1e-9);
    }

    /// <summary>A density over -1, 1 and 2 on a log X axis: the part of the curve at or below x = 0 lies a thousand decades
    /// left of the plot, so the curve and its fill are cut at the left edge; before, the render context was handed NaN.</summary>
    [Fact]
    public void ADensityCurveOnALogXAxis_IsCutAtTheLeftEdge()
    {
        var ctx = Render(Bounds.LogX(), c => new KdeSeriesRenderer(c).Render(new KdeSeries([-1.0, 1, 2])));

        ctx.AssertNothingUndrawableReached();
        var line = Assert.Single(ctx.Polylines());
        Assert.Equal(0, line[0].X, 9);
        Assert.All(line, p => Assert.True(p.X >= -1e-9));
        Assert.All(Assert.Single(ctx.Polygons()), p => Assert.True(p.X >= -1e-9));
    }

    /// <summary>Stream lines rising from y = -1 on a log Y axis: the part at or below 0 lies a thousand decades below the plot,
    /// so a line that climbs out of it is cut where it crosses the bottom edge (before, it began at its first point above 0).</summary>
    [Fact]
    public void AStreamplotOnALogAxis_IsCutAtTheBottomEdge()
    {
        double[] x = [1.0, 2, 3, 4], y = [-1.0, 0.5, 1, 2];
        var u = new double[4, 4];
        var v = new double[4, 4];
        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++) { u[i, j] = 0.2; v[i, j] = 1; }

        var ctx = Render(Bounds.LogY(), c => new StreamplotSeriesRenderer(c).Render(new StreamplotSeries(x, y, u, v)));

        ctx.AssertNothingUndrawableReached();
        Assert.Contains(ctx.Polylines(), line => line[0].Y == BottomEdge || line[^1].Y == BottomEdge);
    }

    // ---- waterfall ----------------------------------------------------------------------------------------------------

    [Fact]
    public void AWaterfallsFirstStep_RunsFromTheBottomEdge_TheNextStandsOnTheTotalBeforeIt()
    {
        var ctx = Render(Bounds.LogY(), c => new WaterfallSeriesRenderer(c).Render(new WaterfallSeries(["a", "b"], [10.0, 5])));

        ctx.AssertNothingUndrawableReached();
        var steps = ctx.Rects();
        Assert.Equal(2, steps.Count);
        Assert.Equal(BottomEdge, Low(steps[0]), 9);
        Assert.Equal(PixelY(10), High(steps[0]), 9);
        Assert.Equal(PixelY(10), Low(steps[1]), 9);
        Assert.Equal(PixelY(15), High(steps[1]), 9);
    }

    /// <summary>10, then -15 to a total of -5, then +20 from that total. A step is a bar from the total before it to the total
    /// after it, and matplotlib 3.11.2 draws a bar whose end a log axis cannot place down to the edge
    /// (<c>ax.bar([1], [-10], bottom=[5])</c> on a log Y axis is the rectangle from 5 to <c>-376917.85</c>, printed): the second
    /// step runs from 10 down to the bottom edge, where the library skipped it, and the third from the bottom edge up to 15.</summary>
    [Fact]
    public void AWaterfallStepEndingWhereTheLogAxisCannotPlace_RunsDownToTheBottomEdge_TheNextRunsUpFromIt()
    {
        var ctx = Render(Bounds.LogY(), c => new WaterfallSeriesRenderer(c).Render(new WaterfallSeries(["a", "b", "c"], [10.0, -15, 20])));

        ctx.AssertNothingUndrawableReached();
        var steps = ctx.Rects();
        Assert.Equal(3, steps.Count);
        Assert.Equal(PixelY(10), High(steps[1]), 9);
        Assert.Equal(BottomEdge, Low(steps[1]), 9);
        Assert.Equal(BottomEdge, Low(steps[2]), 9);
        Assert.Equal(PixelY(15), High(steps[2]), 9);
        Assert.Single(ctx.Of<DrawLineCall>());   // the connector at 10; the one at -5 lies wholly below the plot
    }

    // ---- marks that run straight across an axis -------------------------------------------------------------------------

    /// <summary>matplotlib 3.11.2's <c>ax.bar([1], [-10], bottom=[5])</c> on a log Y axis is the rectangle from 5 down to
    /// <c>-376917.85</c> (printed): it is drawn from its base down to the bottom edge, where the library skipped it.</summary>
    [Fact]
    public void ABarOnAPlacedBase_WhoseEndTheLogAxisCannotPlace_RunsDownToTheBottomEdge()
    {
        var ctx = Render(Bounds.LogY(), c => new BarSeriesRenderer(c).Render(new BarSeries(["a"], [-10.0]) { StackBaseline = [5.0] }));

        ctx.AssertNothingUndrawableReached();
        var bar = Assert.Single(ctx.Rects());
        Assert.Equal(PixelY(5), High(bar), 9);
        Assert.Equal(BottomEdge, Low(bar), 9);
    }

    /// <summary>matplotlib 3.11.2's <c>ax.errorbar([1, 2, 3], [1, 0, 10], yerr=[0.5, 5, 1], capsize=3)</c> on a log Y axis draws
    /// the bar of the point at 0 as the segment <c>[[307.5, -208457.33], [307.5, 266.58]]</c> (printed): from the bottom edge up
    /// to 5, with its upper cap at 5. The point's marker and its lower cap stand a thousand decades down, never seen. The library
    /// skipped the point with its bar and both caps.</summary>
    [Fact]
    public void AnErrorBarOfAPointTheLogAxisCannotPlace_RunsUpFromTheBottomEdge_WithItsUpperCap()
    {
        var ctx = Render(Bounds.LogY(), c => new ErrorBarSeriesRenderer(c).Render(
            new ErrorBarSeries([1.0, 2, 3], [20.0, 0, 10], [2.0, 5, 1], [2.0, 5, 1]) { CapSize = 3 }));

        ctx.AssertNothingUndrawableReached();
        Assert.Equal(2, ctx.Count<DrawCircleCall>());
        var lines = ctx.Of<DrawLineCall>();
        var bar = Assert.Single(lines, l => l.P1.X == 80 && l.P2.X == 80);
        Assert.Equal(BottomEdge, bar.P1.Y, 9);
        Assert.Equal(PixelY(5), bar.P2.Y, 9);
        Assert.Single(lines, l => l.P1.Y == PixelY(5) && l.P2.Y == PixelY(5));
        Assert.DoesNotContain(lines, l => l.P1.Y == BottomEdge && l.P2.Y == BottomEdge);
    }

    /// <summary>matplotlib 3.11.2's <c>ax.errorbar([1, 5, 10], [1, 2, 3], xerr=[0.5, 7, 1])</c> on a log X axis draws the bar of
    /// the point at 5, which reaches -2, from the left edge (<c>[[-391614.96, 198.0], [518.86, 198.0]]</c>, printed); the cap at
    /// -2 is never seen.</summary>
    [Fact]
    public void AHorizontalErrorBarReachingWhereTheLogAxisCannotPlace_RunsFromTheLeftEdge()
    {
        var ctx = Render(Bounds.LogX(), c => new ErrorBarSeriesRenderer(c).Render(
            new ErrorBarSeries([5.0], [5.0], [1.0], [1.0]) { XErrorLow = [7.0], XErrorHigh = [5.0], CapSize = 3 }));

        ctx.AssertNothingUndrawableReached();
        var lines = ctx.Of<DrawLineCall>();
        Assert.Contains(lines, l => l.P1.X == 0 && Math.Abs(l.P2.X - PixelX(10)) < 1e-9 && l.P1.Y == l.P2.Y);
        Assert.DoesNotContain(lines, l => l.P1.X == 0 && l.P2.X == 0);
    }

    /// <summary>A bar whose left end can be placed keeps its left cap; one whose end is missing (NaN) draws neither that bar nor
    /// that end's cap, and keeps the other cap and its dot.</summary>
    [Fact]
    public void AnErrorBarKeepsTheCapsAtEndsItCanPlace_AndDropsAMissingEnd()
    {
        var ctx = Render(Bounds.LogX(), c => new ErrorBarSeriesRenderer(c).Render(
            new ErrorBarSeries([20.0], [5.0], [double.NaN], [1.0]) { XErrorLow = [5.0], XErrorHigh = [5.0], CapSize = 3 }));

        ctx.AssertNothingUndrawableReached();
        var lines = ctx.Of<DrawLineCall>();
        Assert.Contains(lines, l => l.P1.X == l.P2.X && Math.Abs(l.P1.X - PixelX(15)) < 1e-9);   // the left cap
        Assert.Contains(lines, l => l.P1.X == l.P2.X && Math.Abs(l.P1.X - PixelX(25)) < 1e-9);   // the right cap
        Assert.DoesNotContain(lines, l => l.P1.X == l.P2.X && Math.Abs(l.P1.X - PixelX(20)) < 1e-9);   // no vertical bar
        Assert.Equal(1, ctx.Count<DrawCircleCall>());
    }

    /// <summary>A gradient blends from where the area stands to its tip: through a value the log axis cannot place, the blend
    /// runs to the bottom edge, never to a corner a thousand decades below it.</summary>
    [Fact]
    public void AGradientAreaThroughAValueTheLogAxisCannotPlace_BlendsWithinThePlot()
    {
        string svg = Svg(ax => ax.FillBetween([0.0, 1, 2, 3], [2.0, 3, 0, 4], null, s => s.GradientColor = Colors.Red)
            .SetYScale(AxisScale.Log));

        svg.AssertNoNonFiniteNumber();
        var gradient = Assert.Single(svg.Elements("linearGradient"));
        foreach (string end in gradient.Attributes.Keys.Where(name => name is "x1" or "y1" or "x2" or "y2"))
            Assert.InRange(gradient.Number(end), -1, 1000);
    }

    /// <summary>matplotlib 3.11.2's <c>ax.stem([1, 2, 3], [1, 0, 10])</c> on a log Y axis draws the stems
    /// <c>[[96.14, -279942], [96.14, 58]]</c>, <c>[[307.5, -279942], [307.5, -279942]]</c> and
    /// <c>[[518.86, -279942], [518.86, 338]]</c> (printed): a stem rises from the bottom edge to its head, the one whose head is at 0
    /// lies wholly below the plot, and so does the baseline at 0. The library drew the heads without their stems.</summary>
    [Fact]
    public void AStem_RisesFromTheBottomEdgeToItsHead()
    {
        var ctx = Render(Bounds.LogY(), c => new StemSeriesRenderer(c).Render(new StemSeries([1.0, 2, 3], [5.0, 0, 10])));

        ctx.AssertNothingUndrawableReached();
        var stems = ctx.Of<DrawLineCall>();
        Assert.Equal(2, stems.Count);
        Assert.All(stems, s => Assert.Equal(BottomEdge, Math.Max(s.P1.Y, s.P2.Y), 9));
        Assert.Contains(stems, s => Math.Abs(Math.Min(s.P1.Y, s.P2.Y) - PixelY(5)) < 1e-9);
    }

    /// <summary>A point plot's interval is a line of two points (seaborn's <c>ax.plot([x, x], [low, high])</c>): an interval of
    /// 1, 1 and 30, whose lower end falls below 0, runs up from the bottom edge, as a line to a value the log axis cannot place
    /// does; its lower cap stands beyond the axis, never seen. The library skipped the interval and both its caps.</summary>
    [Fact]
    public void APointPlotsIntervalReachingBelowZero_RunsUpFromTheBottomEdge()
    {
        var ctx = Render(Bounds.LogY(), c => new PointplotSeriesRenderer(c).Render(new PointplotSeries([[1.0, 1, 30]])));

        ctx.AssertNothingUndrawableReached();
        var lines = ctx.Of<DrawLineCall>();
        Assert.Contains(lines, l => l.P1.X == l.P2.X && Math.Max(l.P1.Y, l.P2.Y) >= BottomEdge - 1 && Math.Min(l.P1.Y, l.P2.Y) < PixelY(10));
        Assert.Equal(1, ctx.Count<DrawCircleCall>());
    }

    /// <summary>matplotlib's eventplot is a LineCollection through the axes' data transform: a tick of row 0, four units long,
    /// runs from -2 to 2 and is drawn from the bottom edge of a log axis up to 2. The library skipped it.</summary>
    [Fact]
    public void AnEventTickReachingBelowZero_RunsUpFromTheBottomEdge()
    {
        var ctx = Render(Bounds.LogY(), c => new EventplotSeriesRenderer(c).Render(new EventplotSeries([[2.0, 5.0]]) { LineLength = 4 }));

        ctx.AssertNothingUndrawableReached();
        var ticks = ctx.Of<DrawLineCall>();
        Assert.Equal(2, ticks.Count);
        Assert.All(ticks, t => Assert.Equal(BottomEdge, Math.Max(t.P1.Y, t.P2.Y), 0));
    }

    /// <summary>A scatter point has no extent: matplotlib 3.11.2's <c>ax.scatter([1, 2, 3], [1, 0, 10])</c> on a log Y axis masks
    /// the offset of (2, 0) (printed: <c>None</c>), so it is not drawn, and the two others are.</summary>
    [Fact]
    public void AScatterPointAtAValueTheLogAxisCannotPlace_IsNotDrawn()
    {
        var ctx = Render(Bounds.LogY(), c => new ScatterSeriesRenderer(c).Render(new ScatterSeries([1.0, 2, 3], [10.0, 0, 10])));

        ctx.AssertNothingUndrawableReached();
        Assert.Equal(2, ctx.Calls.Count(c => c is DrawCircleCall or DrawPathCall or DrawPolygonCall));
    }

    /// <summary>A residual plot's zero line runs along 0, which a log axis puts a thousand decades below it: wholly below the
    /// plot, it is not drawn. The library handed the render context NaN.</summary>
    [Fact]
    public void AResidualZeroLineOnALogAxis_IsNotDrawn()
    {
        var ctx = Render(Bounds.LogY(), c => new ResidualSeriesRenderer(c).Render(new ResidualSeries(new double[] { 1, 2, 3, 4 }, new double[] { 2, 5, 3, 9 }) { ShowZeroLine = true }));

        ctx.AssertNothingUndrawableReached();
        Assert.Empty(ctx.Of<DrawLineCall>());
    }

    /// <summary>A residual plot's zero line is seaborn's <c>ax.axhline(0, ls=":", c=".2")</c> (seaborn 0.13.2 residplot): it spans
    /// the axes from the left edge to the right, whatever the X axis' scale. matplotlib 3.11.2, <c>axhline(0)</c> on a log X axis
    /// over 10..1000 by y -5..5, 400x300 px (y down): [[0, 150], [400, 150]]. The library placed the bounds of the drawn (log10)
    /// space again as data, so the line ran from 1 to 3, left of the plot.</summary>
    [Fact]
    public void AResidualZeroLineOnALogXAxis_SpansThePlot()
    {
        var logX = Bounds.LogX(yMax: 5, xMin: 10, xMax: 1000, yMin: -5);
        var ctx = Render(logX, c => new ResidualSeriesRenderer(c).Render(
            new ResidualSeries(new double[] { 20, 50, 200, 500 }, new double[] { 2, 5, 3, 9 }) { ShowZeroLine = true }));

        var zero = Assert.Single(ctx.Of<DrawLineCall>());
        Assert.Equal(new Point(0, 150), zero.P1);
        Assert.Equal(new Point(400, 150), zero.P2);
    }

    /// <summary>A residual plot's zero line above a Y range a caller set (1..5) lies outside the plot and is not drawn, as an
    /// <c>axhline</c> beyond the view is clipped away.</summary>
    [Fact]
    public void AResidualZeroLineOutsideTheYRange_IsNotDrawn()
    {
        var ctx = Render(new DataTransform(0, 10, 1, 5, Bounds), c => new ResidualSeriesRenderer(c).Render(
            new ResidualSeries(new double[] { 1, 2, 3, 4 }, new double[] { 2, 5, 3, 9 }) { ShowZeroLine = true }));

        ctx.AssertNothingUndrawableReached();
        Assert.Empty(ctx.Of<DrawLineCall>());
    }

    // ---- box and violin: statistics -----------------------------------------------------------------------------------

    /// <summary>[-1, -1, 10, 10, 10]: first quartile -1, third quartile, median and upper whisker 10, mean 5.6. The box
    /// runs from the bottom edge to 10; the lower whisker (from -1 to -1) has no end the axis can place and is masked.</summary>
    [Fact]
    public void ABox_ClipsAQuartileTheLogAxisCannotPlace_AndMasksAWhiskerItCannotPlaceAtAll()
    {
        var ctx = Render(Bounds.LogY(), c => new BoxSeriesRenderer(c).Render(
            new BoxSeries([[-1.0, -1, 10, 10, 10]]) { ShowMeans = true }));

        ctx.AssertNothingUndrawableReached();
        var box = Assert.Single(ctx.Rects());
        Assert.Equal(BottomEdge, Low(box), 9);
        Assert.Equal(PixelY(10), High(box), 9);
        Assert.Equal(2, ctx.Of<DrawLineCall>().Count);   // the median and the upper whisker
        Assert.Equal(1, ctx.Count<DrawCircleCall>());
    }

    [Fact]
    public void AHorizontalBox_ClipsAQuartileTheLogAxisCannotPlace_ToTheLeftEdge()
    {
        var ctx = Render(Bounds.LogX(), c => new BoxSeriesRenderer(c).Render(new BoxSeries([[-1.0, -1, 10, 10, 10]]) { Vert = false }));

        ctx.AssertNothingUndrawableReached();
        var box = Assert.Single(ctx.Rects());
        Assert.Equal(0, box.X, 9);
        Assert.Equal(PixelX(10), box.X + box.Width, 9);
    }

    public static TheoryData<bool> Orientations { get; } = new(true, false);

    /// <summary>A sample of negative values lies wholly beyond a log axis: its box is masked, nothing of it is drawn.</summary>
    [Theory]
    [MemberData(nameof(Orientations))]
    public void ABoxOfValuesTheLogAxisCannotPlace_IsNotDrawn(bool vertical)
    {
        var ctx = Render(vertical ? Bounds.LogY() : Bounds.LogX(), c => new BoxSeriesRenderer(c).Render(
            new BoxSeries([[-3.0, -2, -1]]) { ShowMeans = true, Vert = vertical }));

        Assert.Empty(ctx.Calls);
    }

    /// <summary>A violin's body is matplotlib's <c>fill_betweenx</c> polygon: a level at or below 0 lies a thousand decades below a
    /// log axis, so the body is cut where its outline crosses the bottom edge and runs along the edge between the two sides. The
    /// library pinned every such level to the edge and drew the outline from there to the first level above 0.</summary>
    [Fact]
    public void AViolin_IsCutAtTheBottomEdge_WhereItsLevelsReachWhatTheLogAxisCannotPlace()
    {
        var ctx = Render(Bounds.LogY(), c => new ViolinSeriesRenderer(c).Render(
            new ViolinSeries([[-3.0, -1, 0.5, 2, 4, 30]]) { ShowExtrema = true, ShowMedians = true, ShowMeans = true }));

        ctx.AssertNothingUndrawableReached();
        AssertNothingBelowThePlot(ctx);
        // The outline runs over 100 levels from -3 to 30 on each side, a third of a unit apart; the thirteenth, the level 1, stands
        // on the bottom edge (the axis' floor is 1), every level below it lies under the plot and is cut away.
        var body = Assert.Single(ctx.Polygons());
        Assert.Equal(2 * (100 - 12), body.Length);
        Assert.Equal(2, body.Count(p => p.Y == BottomEdge));
        Assert.Equal(PixelY(30), body.Min(p => p.Y), 9);
        // the extrema bar from the bottom edge to 30, the tick at 30, the median and the mean; the tick at -3 is masked
        var lines = ctx.Of<DrawLineCall>();
        Assert.Equal(4, lines.Count);
        Assert.Contains(lines, l => Math.Abs(l.P1.Y - BottomEdge) < 1e-9 && Math.Abs(l.P2.Y - PixelY(30)) < 1e-9);
    }

    /// <summary>A violin at x = 0.1 on a log X axis, half a unit wide: its left half (x at or below 0) lies a thousand decades
    /// left of the axis and its right half, below x = 1, left of the plot. Cut at the left edge, nothing of it is left.</summary>
    [Fact]
    public void AViolinWhollyLeftOfALogXAxis_DrawsNoBody()
    {
        var ctx = Render(Bounds.LogX(), c => new ViolinSeriesRenderer(c).Render(new ViolinSeries([[1.0, 2, 2, 3]]) { Positions = [0.1] }));

        ctx.AssertNothingUndrawableReached();
        Assert.Empty(ctx.Polygons());
    }

    /// <summary>matplotlib 3.11.2's <c>ax.violinplot([[1, 2, 3, 4, 5]], positions=[0.3], widths=1.0)</c> on a log X axis puts 100
    /// of the body's 203 vertices a thousand decades left of the axis (printed, x down to <c>-350514.52</c>): the body is cut at
    /// the left edge. A violin at 1, four units wide, reaches from -1 to 3: the library skipped it, matplotlib draws its right
    /// part from the left edge.</summary>
    [Fact]
    public void AViolinReachingWhereTheLogXAxisCannotPlace_IsCutAtTheLeftEdge()
    {
        var ctx = Render(Bounds.LogX(), c => new ViolinSeriesRenderer(c).Render(new ViolinSeries([[1.0, 2, 2, 3]]) { Positions = [1.0], Widths = 4 }));

        ctx.AssertNothingUndrawableReached();
        var body = Assert.Single(ctx.Polygons());
        Assert.All(body, p => Assert.True(p.X >= 0, $"{p} lies left of the plot"));
        Assert.Contains(body, p => p.X == 0);
        Assert.Contains(body, p => p.X > PixelX(2));
    }

    [Fact]
    public void AViolinOfValuesTheLogAxisCannotPlace_IsNotDrawn() =>
        Assert.Empty(Render(Bounds.LogY(), c => new ViolinSeriesRenderer(c).Render(
            new ViolinSeries([[-3.0, -2, -1]]) { ShowExtrema = true, ShowMedians = true, ShowMeans = true })).Calls);

    // ---- the whole figure ---------------------------------------------------------------------------------------------

    private static string Svg(Action<AxesBuilder> axes) => Plt.Create().WithSize(600, 400).AddSubPlot(1, 1, 1, axes).ToSvg();

    private static readonly double[] Sample = [1.0, 2, 3, 5, 8, 13, 21, 40];

    public static TheoryData<string> FigureKinds { get; } = new(
        "bar", "barh", "stacked bars", "hist", "hist step", "hist stepfilled", "count", "count horizontal", "area",
        "area zero lower edge", "area steps", "area where", "stackplot", "waterfall", "box", "box with negatives",
        "violin", "violin with negatives", "kde", "ecdf");

    /// <summary>One mark of <paramref name="kind"/> on a log value axis (Y; X for the horizontal ones), all its data
    /// positive unless the kind says otherwise.</summary>
    private static void Figure(string kind, AxesBuilder ax)
    {
        string[] ab = ["a", "b"];
        double[] x = [0.0, 1, 2];
        static void Stats(ViolinSeries s) { s.ShowExtrema = true; s.ShowMedians = true; s.ShowMeans = true; }
        switch (kind)
        {
            case "barh":
                ax.Bar(ab, [10.0, 100], s => s.Orientation = BarOrientation.Horizontal).SetXScale(AxisScale.Log);
                return;
            case "count horizontal":
                ax.Countplot(["a", "b", "b"], s => s.Orientation = BarOrientation.Horizontal).SetXScale(AxisScale.Log);
                return;
        }

        ax.SetYScale(AxisScale.Log);
        switch (kind)
        {
            case "bar": ax.Bar(ab, [10.0, 100]); break;
            case "stacked bars": ax.Bar(ab, [10.0, 100]).Bar(ab, [5.0, 50]).SetBarMode(BarMode.Stacked); break;
            case "hist": ax.Hist(Sample, 4); break;
            case "hist step": ax.Hist(Sample, 4, s => s.HistType = HistType.Step); break;
            case "hist stepfilled": ax.Hist(Sample, 4, s => s.HistType = HistType.StepFilled); break;
            case "count": ax.Countplot(["a", "b", "b", "c", "c", "c"]); break;
            case "area": ax.FillBetween(x, [1.0, 2, 3]); break;
            case "area zero lower edge": ax.FillBetween(x, [1.0, 2, 3], [0.0, 0, 0]); break;
            case "area steps": ax.FillBetween(x, [1.0, 2, 3], null, s => s.StepMode = DrawStyle.StepsPost); break;
            case "area where": ax.FillBetween(x, [1.0, 2, 3], null, s => s.Where = (_, v) => v > 1.5); break;
            case "stackplot": ax.StackPlot(x, [[1.0, 2, 3], [1.0, 1, 1]]); break;
            case "waterfall": ax.Waterfall(["a", "b", "c"], [10.0, -15, 20]); break;
            case "box": ax.BoxPlot([Sample, [1.0, 2, 3]], s => s.ShowMeans = true); break;
            case "box with negatives": ax.BoxPlot([[-3.0, -1, 0.5, 2, 4, 30], [-3.0, -2, -1]], s => s.ShowMeans = true); break;
            case "violin": ax.Violin([Sample], Stats); break;
            case "violin with negatives": ax.Violin([[-3.0, -1, 0.5, 2, 4, 30], [-3.0, -2, -1]], Stats); break;
            case "kde": ax.Kde(Sample); break;
            default: ax.Ecdf(Sample); break;
        }
    }

    [Theory]
    [MemberData(nameof(FigureKinds))]
    public void AFigureOnALogAxis_WritesNoNaN_ForAMarkThatStandsOnZero(string kind) =>
        Svg(ax => Figure(kind, ax)).AssertNoNonFiniteNumber();

    private static int BarsIn(string svg, string fill) => svg.Elements("rect").Count(rect => rect["fill"] == fill);

    /// <summary>A bar chart on a log axis draws its bars (before, every bar on a zero baseline vanished); a stacked one
    /// draws its lowest bars too.</summary>
    [Fact]
    public void ABarChartOnALogAxis_DrawsItsBars()
    {
        Assert.Equal(2, BarsIn(Svg(ax => ax.Bar(["a", "b"], [10.0, 100]).SetYScale(AxisScale.Log)), "#1F77B4"));
        Assert.Equal(2, BarsIn(Svg(ax => ax.Bar(["a", "b"], [10.0, 100]).Bar(["a", "b"], [5.0, 50])
            .SetBarMode(BarMode.Stacked).SetYScale(AxisScale.Log)), "#1F77B4"));
    }

    /// <summary>Negative bars on a log axis reach a value it cannot place: masked, nothing drawn, no NaN.</summary>
    [Fact]
    public void NegativeBarsOnALogAxis_AreNotDrawn()
    {
        string svg = Svg(ax => ax.Bar(["a", "b"], [-10.0, -100]).SetYScale(AxisScale.Log));

        Assert.Equal(0, BarsIn(svg, "#1F77B4"));
        svg.AssertNoNonFiniteNumber();
    }
}
