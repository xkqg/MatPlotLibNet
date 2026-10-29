// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>On a log axis a mark that stands on a base reaches the edge where the axis begins, as in matplotlib, whose log
/// scale clips (<c>nonpositive='clip'</c>) what a bar, an area, a stack, a waterfall, a box or a violin stands on. Before,
/// the zero a bar, a histogram, a count plot, an area, a stack, a waterfall or a density curve stands on, and a quartile or
/// a violin outline at 0 or below, were values the log scale could not place: a bar vanished and the others wrote NaN into
/// the SVG, though every value of the data was positive. The rule: a mark clips the base it stands on (for a box, a whisker
/// and a violin, the lower statistic) and masks the value it reaches, so its top is where the log scale puts it, and a
/// mark whose value the axis cannot place (negative data) is skipped; a line, a marker, a median or a mean at a value the
/// axis cannot place is masked as before. A symmetric-log axis and a linear axis place 0 and are unchanged.</summary>
public class LogAxisClipTests
{
    private static readonly Rect Bounds = new(0, 0, 400, 300);

    /// <summary>The bottom edge of <see cref="Bounds"/>: where a log Y axis that is not reversed begins.</summary>
    private const double BottomEdge = 300;

    /// <summary>A log Y axis from 1 (the bottom edge, 300 px) to 100 (the top edge, 0 px).</summary>
    private static DataTransform LogY() =>
        new(0, 10, 0, 2, Bounds, null, null, 0, 10, 1, 100, AxisScale.Linear, AxisScale.Log, 1, 1);

    /// <summary>The same log Y axis set high to low: it begins (at 1) on the top edge.</summary>
    private static DataTransform LogYReversed() =>
        new(0, 10, 2, 0, Bounds, null, null, 0, 10, 100, 1, AxisScale.Linear, AxisScale.Log, 1, 1);

    /// <summary>A log X axis from 1 (the left edge, 0 px) to 100 (the right edge, 400 px).</summary>
    private static DataTransform LogX() =>
        new(0, 2, 0, 10, Bounds, null, null, 1, 100, 0, 10, AxisScale.Log, AxisScale.Linear, 1, 1);

    /// <summary>A symmetric-log Y axis from -100 to 100: it places 0, in the middle of the plot.</summary>
    private static DataTransform SymLogY() =>
        new(0, 10, SymlogTransform.Forward(-100, 1), SymlogTransform.Forward(100, 1), Bounds, null, null, 0, 10, -100, 100,
            AxisScale.Linear, AxisScale.SymLog, 1, 1);

    private static double PixelY(double value) => LogY().DataToPixel(0, value).Y;

    private static double PixelX(double value) => LogX().DataToPixel(value, 0).X;

    private static RecordingRenderContext Render(DataTransform transform, Action<SeriesRenderContext> draw)
    {
        var ctx = new RecordingRenderContext();
        draw(new SeriesRenderContext(transform, ctx, Colors.Tab10Blue, new RenderArea(Bounds, ctx)));
        return ctx;
    }

    // ---- what a render context was handed ---------------------------------------------------------------------------

    private static object? Arg(CallRecord call, string name) => call.Args!.GetType().GetProperty(name)!.GetValue(call.Args);

    private static List<Rect> Rects(RecordingRenderContext ctx) => [.. ctx.OfKind("DrawRectangle").Select(c => (Rect)Arg(c, "rect")!)];

    private static List<Point[]> Polygons(RecordingRenderContext ctx) => [.. ctx.OfKind("DrawPolygon").Select(c => (Point[])Arg(c, "points")!)];

    private static List<Point[]> Polylines(RecordingRenderContext ctx) => [.. ctx.OfKind("DrawLines").Select(c => (Point[])Arg(c, "points")!)];

    private static List<Point[]> Lines(RecordingRenderContext ctx) =>
        [.. ctx.OfKind("DrawLine").Select(c => new[] { (Point)Arg(c, "p1")!, (Point)Arg(c, "p2")! })];

    /// <summary>No call on the context carried a coordinate, size or radius that is not finite.</summary>
    private static void AssertNothingUndrawableReached(RecordingRenderContext ctx)
    {
        foreach (var call in ctx.Calls.Where(c => c.Args is not null))
        {
            foreach (var property in call.Args!.GetType().GetProperties())
            {
                IEnumerable<double> numbers = property.GetValue(call.Args) switch
                {
                    double d => [d],
                    Point p => [p.X, p.Y],
                    Rect r => [r.X, r.Y, r.Width, r.Height],
                    Point[] points => points.SelectMany(p => new[] { p.X, p.Y }),
                    _ => [],
                };
                foreach (double number in numbers)
                    Assert.True(double.IsFinite(number), $"{call.Kind}.{property.Name} was handed {number}");
            }
        }
    }

    /// <summary>The lower and upper pixel Y a rectangle covers, whichever way round it was handed.</summary>
    private static double Low(Rect r) => Math.Max(r.Y, r.Y + r.Height);

    private static double High(Rect r) => Math.Min(r.Y, r.Y + r.Height);

    // ---- bars ---------------------------------------------------------------------------------------------------------

    [Fact]
    public void ABar_RunsFromTheBottomEdgeToWhereTheLogScalePutsItsValue()
    {
        var ctx = Render(LogY(), c => new BarSeriesRenderer(c).Render(new BarSeries(["a", "b"], [10.0, 50])));

        AssertNothingUndrawableReached(ctx);
        var bars = Rects(ctx);
        Assert.Equal(2, bars.Count);
        Assert.Equal(BottomEdge, Low(bars[0]), 9);
        Assert.Equal(PixelY(10), High(bars[0]), 9);
        Assert.Equal(BottomEdge, Low(bars[1]), 9);
        Assert.Equal(PixelY(50), High(bars[1]), 9);
    }

    [Fact]
    public void AHorizontalBar_RunsFromTheLeftEdgeToWhereTheLogScalePutsItsValue()
    {
        var ctx = Render(LogX(), c => new BarSeriesRenderer(c).Render(
            new BarSeries(["a", "b"], [10.0, 50]) { Orientation = BarOrientation.Horizontal }));

        AssertNothingUndrawableReached(ctx);
        var bars = Rects(ctx);
        Assert.Equal(2, bars.Count);
        Assert.Equal(0, bars[0].X, 9);
        Assert.Equal(PixelX(10), bars[0].X + bars[0].Width, 9);
        Assert.Equal(PixelX(50), bars[1].X + bars[1].Width, 9);
    }

    /// <summary>A reversed axis begins on the other edge, and so does what it clips.</summary>
    [Fact]
    public void OnAReversedLogAxis_ABar_RunsFromTheEdgeWhereTheAxisBegins()
    {
        var ctx = Render(LogYReversed(), c => new BarSeriesRenderer(c).Render(new BarSeries(["a"], [10.0])));

        var bar = Assert.Single(Rects(ctx));
        Assert.Equal(0, High(bar), 9);
        Assert.Equal(LogYReversed().DataToPixel(0, 10).Y, Low(bar), 9);
    }

    /// <summary>A negative bar, and a bar of 0, reach a value the log axis cannot place: masked, with its label.</summary>
    [Fact]
    public void ABarWhoseValueTheLogAxisCannotPlace_IsSkippedWithItsLabel_TheOthersStandOnTheBottomEdge()
    {
        var ctx = Render(LogY(), c => new BarSeriesRenderer(c).Render(
            new BarSeries(["a", "b", "c"], [10.0, -5, 0]) { ShowLabels = true }));

        AssertNothingUndrawableReached(ctx);
        var bar = Assert.Single(Rects(ctx));
        Assert.Equal(BottomEdge, Low(bar), 9);
        var label = Assert.Single(ctx.OfKind("DrawText"));
        Assert.Equal("10", Arg(label, "text"));
    }

    /// <summary>A bar that stands on a base the axis can place stands on it, not on the edge.</summary>
    [Fact]
    public void ABarOnABaseTheLogAxisCanPlace_StandsOnThatBase()
    {
        var ctx = Render(LogY(), c => new BarSeriesRenderer(c).Render(new BarSeries(["a"], [10.0]) { StackBaseline = [2.0] }));

        var bar = Assert.Single(Rects(ctx));
        Assert.Equal(PixelY(2), Low(bar), 9);
        Assert.Equal(PixelY(12), High(bar), 9);
    }

    /// <summary>A symmetric-log axis places 0: a bar stands on it, in the middle of the plot, as before.</summary>
    [Fact]
    public void OnASymlogAxis_ABar_StandsOnZero_NotOnTheEdge()
    {
        var ctx = Render(SymLogY(), c => new BarSeriesRenderer(c).Render(new BarSeries(["a"], [10.0])));

        var bar = Assert.Single(Rects(ctx));
        double zero = SymLogY().DataToPixel(0, 0).Y;
        Assert.Equal(zero, Low(bar), 9);
        Assert.NotEqual(BottomEdge, zero, 3);
    }

    // ---- histogram and count plot -------------------------------------------------------------------------------------

    /// <summary>Bins 1..2, 2..3 and 3..4 hold 2, 0 and 2 values: two bars on the bottom edge; the empty bin's bar has no
    /// height a log axis can place and is skipped.</summary>
    [Fact]
    public void AHistogramsBars_StandOnTheBottomEdge_AnEmptyBinIsSkipped()
    {
        var ctx = Render(LogY(), c => new HistogramSeriesRenderer(c).Render(new HistogramSeries([1.0, 1, 3.5, 4]) { Bins = 3 }));

        AssertNothingUndrawableReached(ctx);
        var bars = Rects(ctx);
        Assert.Equal(2, bars.Count);
        Assert.All(bars, b => Assert.Equal(BottomEdge, Low(b), 9));
        Assert.All(bars, b => Assert.Equal(PixelY(2), High(b), 9));
    }

    /// <summary>A step histogram's outline comes down to the bottom edge at its ends; an empty bin breaks it.</summary>
    [Theory]
    [InlineData(HistType.Step, "DrawLines")]
    [InlineData(HistType.StepFilled, "DrawPolygon")]
    public void AStepHistogram_ComesDownToTheBottomEdge_AndBreaksAtAnEmptyBin(HistType type, string kind)
    {
        var ctx = Render(LogY(), c => new HistogramSeriesRenderer(c).Render(
            new HistogramSeries([1.0, 1, 3.5, 4]) { Bins = 3, HistType = type }));

        AssertNothingUndrawableReached(ctx);
        var outlines = ctx.OfKind(kind).Select(c => (Point[])Arg(c, "points")!).ToList();
        Assert.Equal(2, outlines.Count);
        Assert.All(outlines, o => Assert.Equal(BottomEdge, o[0].Y, 9));
        Assert.All(outlines, o => Assert.Contains(o.Skip(1), p => Math.Abs(p.Y - PixelY(2)) < 1e-9));
    }

    [Fact]
    public void AStepHistogramWithoutAnEmptyBin_IsOneOutline()
    {
        var ctx = Render(LogY(), c => new HistogramSeriesRenderer(c).Render(
            new HistogramSeries([1.0, 2.5, 3.5]) { Bins = 3, HistType = HistType.StepFilled }));

        var outline = Assert.Single(Polygons(ctx));
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
        var ctx = Render(vertical ? LogY() : LogX(), c => new CountSeriesRenderer(c).Render(
            new CountSeries(["a", "b", "b"]) { Orientation = orientation }));

        AssertNothingUndrawableReached(ctx);
        var bars = Rects(ctx);
        Assert.Equal(2, bars.Count);
        if (vertical)
            Assert.All(bars, b => Assert.Equal(BottomEdge, Low(b), 9));
        else
            Assert.All(bars, b => Assert.Equal(0, b.X, 9));
    }

    /// <summary>A count bar stands centred in its category's slot, so a bar 1.2 wide in the first slot spans -0.1..1.1, which
    /// a log X axis cannot place: skipped, the other (0.9..2.1) is drawn.</summary>
    [Fact]
    public void ACountBarTheAxesCannotPlace_IsSkipped()
    {
        var ctx = Render(LogX(), c => new CountSeriesRenderer(c).Render(new CountSeries(["a", "b", "b"]) { BarWidth = 1.2 }));

        AssertNothingUndrawableReached(ctx);
        Assert.Single(Rects(ctx));
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
        var ctx = Render(LogY(), c => new AreaSeriesRenderer(c).Render(Area(kind)));

        AssertNothingUndrawableReached(ctx);
        var fill = Assert.Single(Polygons(ctx));
        Assert.All(fill, p => Assert.True(p.Y <= BottomEdge + 1e-9, $"{p} lies below the plot"));
        Assert.True(fill.Count(p => Math.Abs(p.Y - BottomEdge) < 1e-9) >= 2, "the fill reaches the bottom edge");
        Assert.Contains(fill, p => Math.Abs(p.Y - PixelY(50)) < 1e-9);
    }

    /// <summary>The top of an area is data: a top the log axis cannot place breaks the area there, as it breaks a line.</summary>
    [Fact]
    public void AnAreaWhoseTopTheLogAxisCannotPlace_BreaksThere()
    {
        var ctx = Render(LogY(), c => new AreaSeriesRenderer(c).Render(new AreaSeries([0.0, 1, 2, 3, 4], [1.0, 2, -2, 3, 4])));

        AssertNothingUndrawableReached(ctx);
        Assert.Equal(2, Polygons(ctx).Count);
        Assert.Equal(2, Polylines(ctx).Count);
    }

    [Fact]
    public void AStackedArea_ItsFirstLayerStandsOnTheBottomEdge_TheNextOnTheLayerBelow()
    {
        var ctx = Render(LogY(), c => new StackedAreaSeriesRenderer(c).Render(
            new StackedAreaSeries([0.0, 1, 2], [[1.0, 2, 3], [1.0, 1, 1]])));

        AssertNothingUndrawableReached(ctx);
        var layers = Polygons(ctx);
        Assert.Equal(2, layers.Count);
        Assert.All(layers[0].Skip(3), p => Assert.Equal(BottomEdge, p.Y, 9));
        double[] firstTops = [.. layers[0].Take(3).Select(p => p.Y).Reverse()];
        double[] secondBases = [.. layers[1].Skip(3).Select(p => p.Y)];
        Assert.All(Enumerable.Range(0, 3), i => Assert.Equal(firstTops[i], secondBases[i], 9));
    }

    [Fact]
    public void ADensityCurve_IsFilledDownToTheBottomEdge()
    {
        var ctx = Render(LogY(), c => new KdeSeriesRenderer(c).Render(new KdeSeries([1.0, 2, 3, 5, 8])));

        AssertNothingUndrawableReached(ctx);
        var fill = Assert.Single(Polygons(ctx));
        Assert.Equal(BottomEdge, fill[^1].Y, 9);
        Assert.Equal(BottomEdge, fill[^2].Y, 9);
    }

    /// <summary>An ECDF is a line: its start at 0, which a log axis cannot place, is masked, and the line starts at the
    /// first step instead of handing the render context NaN.</summary>
    [Fact]
    public void AnEcdf_MasksItsStartAtZero_AndStartsAtItsFirstStep()
    {
        var ctx = Render(LogY(), c => new EcdfSeriesRenderer(c).Render(new EcdfSeries([1.0, 2, 3])));

        AssertNothingUndrawableReached(ctx);
        var line = Assert.Single(Polylines(ctx));
        Assert.Equal(PixelY(1.0 / 3), line[0].Y, 9);
    }

    // ---- waterfall ----------------------------------------------------------------------------------------------------

    [Fact]
    public void AWaterfallsFirstStep_RunsFromTheBottomEdge_TheNextStandsOnTheTotalBeforeIt()
    {
        var ctx = Render(LogY(), c => new WaterfallSeriesRenderer(c).Render(new WaterfallSeries(["a", "b"], [10.0, 5])));

        AssertNothingUndrawableReached(ctx);
        var steps = Rects(ctx);
        Assert.Equal(2, steps.Count);
        Assert.Equal(BottomEdge, Low(steps[0]), 9);
        Assert.Equal(PixelY(10), High(steps[0]), 9);
        Assert.Equal(PixelY(10), Low(steps[1]), 9);
        Assert.Equal(PixelY(15), High(steps[1]), 9);
    }

    /// <summary>10, then -15 to a total of -5 (masked: the log axis cannot place it), then +20 from that total: the third
    /// step stands on a base the axis cannot place and runs from the bottom edge to 15.</summary>
    [Fact]
    public void AWaterfallStepEndingWhereTheLogAxisCannotPlace_IsSkipped_TheNextRunsFromTheBottomEdge()
    {
        var ctx = Render(LogY(), c => new WaterfallSeriesRenderer(c).Render(new WaterfallSeries(["a", "b", "c"], [10.0, -15, 20])));

        AssertNothingUndrawableReached(ctx);
        var steps = Rects(ctx);
        Assert.Equal(2, steps.Count);
        Assert.Equal(BottomEdge, Low(steps[1]), 9);
        Assert.Equal(PixelY(15), High(steps[1]), 9);
        Assert.Single(Lines(ctx));   // the connector at 10; the one at -5 is masked
    }

    // ---- box and violin: statistics -----------------------------------------------------------------------------------

    /// <summary>[-1, -1, 10, 10, 10]: first quartile -1, third quartile, median and upper whisker 10, mean 5.6. The box
    /// runs from the bottom edge to 10; the lower whisker (from -1 to -1) has no end the axis can place and is masked.</summary>
    [Fact]
    public void ABox_ClipsAQuartileTheLogAxisCannotPlace_AndMasksAWhiskerItCannotPlaceAtAll()
    {
        var ctx = Render(LogY(), c => new BoxSeriesRenderer(c).Render(
            new BoxSeries([[-1.0, -1, 10, 10, 10]]) { ShowMeans = true }));

        AssertNothingUndrawableReached(ctx);
        var box = Assert.Single(Rects(ctx));
        Assert.Equal(BottomEdge, Low(box), 9);
        Assert.Equal(PixelY(10), High(box), 9);
        Assert.Equal(2, Lines(ctx).Count);   // the median and the upper whisker
        Assert.Equal(1, ctx.CountOf("DrawCircle"));
    }

    [Fact]
    public void AHorizontalBox_ClipsAQuartileTheLogAxisCannotPlace_ToTheLeftEdge()
    {
        var ctx = Render(LogX(), c => new BoxSeriesRenderer(c).Render(new BoxSeries([[-1.0, -1, 10, 10, 10]]) { Vert = false }));

        AssertNothingUndrawableReached(ctx);
        var box = Assert.Single(Rects(ctx));
        Assert.Equal(0, box.X, 9);
        Assert.Equal(PixelX(10), box.X + box.Width, 9);
    }

    public static TheoryData<bool> Orientations { get; } = new(true, false);

    /// <summary>A sample of negative values lies wholly beyond a log axis: its box is masked, nothing of it is drawn.</summary>
    [Theory]
    [MemberData(nameof(Orientations))]
    public void ABoxOfValuesTheLogAxisCannotPlace_IsNotDrawn(bool vertical)
    {
        var ctx = Render(vertical ? LogY() : LogX(), c => new BoxSeriesRenderer(c).Render(
            new BoxSeries([[-3.0, -2, -1]]) { ShowMeans = true, Vert = vertical }));

        Assert.Empty(ctx.Calls);
    }

    [Fact]
    public void AViolin_ClipsTheValuesTheLogAxisCannotPlace_ToTheBottomEdge()
    {
        var ctx = Render(LogY(), c => new ViolinSeriesRenderer(c).Render(
            new ViolinSeries([[-3.0, -1, 0.5, 2, 4, 30]]) { ShowExtrema = true, ShowMedians = true, ShowMeans = true }));

        AssertNothingUndrawableReached(ctx);
        // The outline runs over 100 levels from -3 to 30 on each side, a third of a unit apart; the levels from -3 up to 0 (the
        // first ten) lie at or below 0 and are clipped to the bottom edge, and the thirteenth, the level 1, stands on it (the
        // axis' floor is 1); the others are where the log scale puts them (the eleventh, at one third, below the floor).
        var body = Assert.Single(Polygons(ctx));
        Assert.Equal(200, body.Length);
        Assert.Equal(22, body.Count(p => p.Y == BottomEdge));
        Assert.Equal(BottomEdge, body[0].Y);
        Assert.Equal(BottomEdge, body[^1].Y);
        Assert.Equal(PixelY(-3 + 33.0 * 10 / 99), body[10].Y, 9);
        Assert.Equal(PixelY(30), body.Min(p => p.Y), 9);
        // the extrema bar from the bottom edge to 30, the tick at 30, the median and the mean; the tick at -3 is masked
        var lines = Lines(ctx);
        Assert.Equal(4, lines.Count);
        Assert.Contains(lines, l => Math.Abs(l[0].Y - BottomEdge) < 1e-9 && Math.Abs(l[1].Y - PixelY(30)) < 1e-9);
    }

    /// <summary>A violin at x = 0.1 on a log X axis: its centre can be placed, the left half of its outline (x at or
    /// below 0) cannot, so the outline is skipped rather than handed NaN.</summary>
    [Fact]
    public void AViolinWhoseOutlineTheAxesCannotPlace_IsSkipped()
    {
        var ctx = Render(LogX(), c => new ViolinSeriesRenderer(c).Render(new ViolinSeries([[1.0, 2, 2, 3]]) { Positions = [0.1] }));

        AssertNothingUndrawableReached(ctx);
        Assert.Empty(Polygons(ctx));
    }

    [Fact]
    public void AViolinOfValuesTheLogAxisCannotPlace_IsNotDrawn() =>
        Assert.Empty(Render(LogY(), c => new ViolinSeriesRenderer(c).Render(
            new ViolinSeries([[-3.0, -2, -1]]) { ShowExtrema = true, ShowMedians = true, ShowMeans = true })).Calls);

    // ---- the whole figure ---------------------------------------------------------------------------------------------

    /// <summary>The NaN and Infinity tokens of an SVG, in an attribute or between tags; the scripts are left out.</summary>
    private static List<string> NonFiniteTokens(string svg)
    {
        string body = Regex.Replace(svg, "<script[\\s\\S]*?</script>", "");
        return [.. Regex.Matches(body, "[^\"<>]*(?:NaN|Infinity)[^\"<>]*").Select(m => m.Value)];
    }

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
        Assert.Empty(NonFiniteTokens(Svg(ax => Figure(kind, ax))));

    private static int BarsIn(string svg, string fill) => Regex.Matches(svg, $"<rect [^>]*fill=\"{fill}\"").Count;

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
        Assert.Empty(NonFiniteTokens(svg));
    }
}
