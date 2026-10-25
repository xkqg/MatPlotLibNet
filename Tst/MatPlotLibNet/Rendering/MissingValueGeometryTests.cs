// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.CartesianParts;
using MatPlotLibNet.Rendering.LegendRendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A coordinate the chart cannot draw — a missing value (NaN), an infinity, or a value the axis cannot place —
/// never reaches a render context, so no SVG attribute, PNG path or native shape is handed NaN. Before, every renderer
/// that draws one thing per data point drew it at NaN: a marker (<c>cy="NaN"</c>), a stem, an error bar, a bar and its
/// label (<c>height="NaN"</c>, the text "NaN"), a waterfall step, a reference line, a span, a trendline, a level, a
/// signal, an annotation, and the legend wrote "= NaN". One owner (<c>MissingValueExtensions</c>) says what can be drawn;
/// every renderer asks it before it draws a unit, and skips the unit when the answer is no — as matplotlib masks an
/// invalid point. A stacked area breaks at the gap as a line does; a box or a violin is drawn from the observations that
/// are there. Whatever can be drawn is drawn exactly as it is without the missing point.</summary>
public class MissingValueGeometryTests
{
    private const double NaN = double.NaN;
    private static readonly Rect Bounds = new(0, 0, 400, 300);

    private static DataTransform Linear() => new(0, 10, 0, 10, Bounds);

    /// <summary>A log Y axis from 1 to 100 (scaled 0..2): it cannot place 0 or a negative value.</summary>
    private static DataTransform LogY() =>
        new(0, 10, 0, 2, Bounds, null, null, 0, 10, 1, 100, AxisScale.Linear, AxisScale.Log, 1, 1);

    private static RecordingRenderContext Render(Action<SeriesRenderContext> draw, DataTransform? transform = null)
    {
        var ctx = new RecordingRenderContext();
        draw(new SeriesRenderContext(transform ?? Linear(), ctx, Colors.Tab10Blue, new RenderArea(Bounds, ctx)));
        return ctx;
    }

    // ---- what a render context was handed ---------------------------------------------------------------------------

    private static IEnumerable<double> Numbers(object? value) => value switch
    {
        double d => [d],
        Point p => [p.X, p.Y],
        Rect r => [r.X, r.Y, r.Width, r.Height],
        Point[] points => points.SelectMany(p => Numbers(p)),
        PathSegment[] segments => segments.SelectMany(s => s.GetType().GetProperties().SelectMany(p => Numbers(p.GetValue(s)))),
        _ => [],
    };

    /// <summary>No call on the context carried a coordinate, size or radius that is not finite, nor a text that says
    /// NaN.</summary>
    private static void AssertNothingUndrawableReached(RecordingRenderContext ctx)
    {
        foreach (var call in ctx.Calls.Where(c => c.Args is not null))
        {
            foreach (var property in call.Args!.GetType().GetProperties())
            {
                object? value = property.GetValue(call.Args);
                foreach (double number in Numbers(value))
                    Assert.True(double.IsFinite(number), $"{call.Kind}.{property.Name} was handed {number}");
                if (value is string text) Assert.DoesNotContain("NaN", text);
            }
        }
    }

    private static string Describe(object? value) => value switch
    {
        Point[] points => string.Join(" ", points),
        PathSegment[] segments => string.Join(" ", segments.Select(s => s.ToSvgPathData())),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "",
    };

    /// <summary>Every call, in order, with every argument: two drawings with the same signature are the same drawing.</summary>
    private static List<string> Signature(RecordingRenderContext ctx, string? kind = null) =>
        [.. ctx.Calls.Where(c => kind is null || c.Kind == kind)
            .Select(c => c.Kind + "(" + string.Join("; ", c.Args?.GetType().GetProperties().Select(p => Describe(p.GetValue(c.Args))) ?? []) + ")")];

    /// <summary>The NaN and Infinity tokens of an SVG, in an attribute or between tags; the scripts (which test
    /// <c>isNaN</c>) are left out.</summary>
    private static List<string> NonFiniteTokens(string svg)
    {
        string body = Regex.Replace(svg, "<script[\\s\\S]*?</script>", "");
        return [.. Regex.Matches(body, "[^\"<>]*(?:NaN|Infinity)[^\"<>]*").Select(m => m.Value)];
    }

    // ---- markers ------------------------------------------------------------------------------------------------------

    public static TheoryData<MarkerStyle> Markers { get; } = new(Enum.GetValues<MarkerStyle>().Where(m => m != MarkerStyle.None));

    [Theory]
    [MemberData(nameof(Markers))]
    public void AScatterMarkerAtAMissingValue_IsSkipped_TheOthersAreDrawnAsWithoutIt(MarkerStyle marker)
    {
        var withGap = Render(c => new ScatterSeriesRenderer(c).Render(new ScatterSeries([1.0, 2, 3, 4], [1, NaN, 3, 4]) { Marker = marker }));
        var without = Render(c => new ScatterSeriesRenderer(c).Render(new ScatterSeries([1.0, 3, 4], [1, 3, 4]) { Marker = marker }));

        AssertNothingUndrawableReached(withGap);
        Assert.Equal(Signature(without), Signature(withGap));
    }

    [Theory]
    [InlineData(NaN, 2)]
    [InlineData(2, NaN)]
    [InlineData(2, double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity, 2)]
    public void AScatterPointThatCannotBePlaced_IsSkipped(double x, double y)
    {
        var withGap = Render(c => new ScatterSeriesRenderer(c).Render(new ScatterSeries([1.0, x, 3], [1, y, 3])));
        var without = Render(c => new ScatterSeriesRenderer(c).Render(new ScatterSeries([1.0, 3], [1, 3])));

        AssertNothingUndrawableReached(withGap);
        Assert.Equal(Signature(without), Signature(withGap));
    }

    /// <summary>matplotlib's scatter drops a point whose size is not a number with the point (<c>delete_masked_points</c>
    /// masks x, y and s together).</summary>
    [Fact]
    public void AScatterPointWithoutADrawableSize_IsSkipped()
    {
        var withGap = Render(c => new ScatterSeriesRenderer(c).Render(new ScatterSeries([1.0, 2, 3], [1, 2, 3]) { Sizes = [20, NaN, 40] }));
        var without = Render(c => new ScatterSeriesRenderer(c).Render(new ScatterSeries([1.0, 3], [1, 3]) { Sizes = [20, 40] }));

        AssertNothingUndrawableReached(withGap);
        Assert.Equal(Signature(without), Signature(withGap));
    }

    [Fact]
    public void ASkippedScatterPoint_WritesNoTooltip()
    {
        var svg = new SvgRenderContext();
        var context = new SeriesRenderContext(Linear(), svg, Colors.Tab10Blue, new RenderArea(Bounds, svg)) { TooltipsEnabled = true };

        new ScatterSeriesRenderer(context).Render(new ScatterSeries([1.0, 2, 3], [1, NaN, 3]));

        string output = svg.GetOutput();
        Assert.Equal(2, Regex.Matches(output, "<title>").Count);
        Assert.Empty(NonFiniteTokens(output));
    }

    [Theory]
    [InlineData(DrawStyle.Default)]
    [InlineData(DrawStyle.StepsPost)]
    public void ALineMarkerAtAMissingValue_IsSkipped_TheOthersAreDrawnAsWithoutIt(DrawStyle style)
    {
        var withGap = Render(c => new LineSeriesRenderer(c).Render(new LineSeries([1.0, 2, 3, 4], [1, NaN, 3, 4]) { Marker = MarkerStyle.Circle, DrawStyle = style }));
        var without = Render(c => new LineSeriesRenderer(c).Render(new LineSeries([1.0, 3, 4], [1, 3, 4]) { Marker = MarkerStyle.Circle, DrawStyle = style }));

        AssertNothingUndrawableReached(withGap);
        Assert.Equal(Signature(without, "DrawCircle"), Signature(withGap, "DrawCircle"));
    }

    // ---- stems --------------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(2, NaN)]
    [InlineData(NaN, 2)]
    public void AStemAtAMissingValue_IsSkipped_TheOthersAndTheBaselineAreDrawnAsWithoutIt(double x, double y)
    {
        var withGap = Render(c => new StemSeriesRenderer(c).Render(new StemSeries([1.0, x, 3, 4], [1, y, 3, 4])));
        var without = Render(c => new StemSeriesRenderer(c).Render(new StemSeries([1.0, 3, 4], [1, 3, 4])));

        AssertNothingUndrawableReached(withGap);
        Assert.Equal(Signature(without), Signature(withGap));
    }

    [Fact]
    public void StemsWithNoDrawableX_DrawNothing_NotEvenABaseline() =>
        Assert.Empty(Render(c => new StemSeriesRenderer(c).Render(new StemSeries([NaN, NaN], [1, 2]))).Calls);

    /// <summary>A log axis cannot place the stems' baseline at 0: the heads are drawn, a stem and a baseline that would
    /// run to 0 are not.</summary>
    [Fact]
    public void OnALogAxis_TheStemHeadsAreDrawn_TheStemsAndBaselineToZeroAreNot()
    {
        var ctx = Render(c => new StemSeriesRenderer(c).Render(new StemSeries([1.0, 2], [10, 50])), LogY());

        AssertNothingUndrawableReached(ctx);
        Assert.Equal(2, ctx.CountOf("DrawCircle"));
        Assert.Equal(0, ctx.CountOf("DrawLine"));
    }

    // ---- error bars ---------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(2, NaN)]
    [InlineData(NaN, 2)]
    public void AnErrorBarPointThatCannotBePlaced_IsSkippedWithItsBars(double x, double y)
    {
        var withGap = Render(c => new ErrorBarSeriesRenderer(c).Render(new ErrorBarSeries([1.0, x, 3], [1, y, 3], [0.5, 0.5, 0.5], [0.5, 0.5, 0.5])));
        var without = Render(c => new ErrorBarSeriesRenderer(c).Render(new ErrorBarSeries([1.0, 3], [1, 3], [0.5, 0.5], [0.5, 0.5])));

        AssertNothingUndrawableReached(withGap);
        Assert.Equal(Signature(without), Signature(withGap));
    }

    /// <summary>matplotlib draws an error bar's line and its two caps as separate marks, each masked on its own: a missing
    /// upper error leaves the point and its lower cap.</summary>
    [Theory]
    [InlineData(NaN, 0.5)]
    [InlineData(0.5, NaN)]
    public void AMissingError_SkipsTheBarAndItsCap_TheOtherCapAndThePointStay(double low, double high)
    {
        var ctx = Render(c => new ErrorBarSeriesRenderer(c).Render(new ErrorBarSeries([1.0, 2, 3], [1, 2, 3], [0.5, low, 0.5], [0.5, high, 0.5])));

        AssertNothingUndrawableReached(ctx);
        Assert.Equal(3, ctx.CountOf("DrawCircle"));
        Assert.Equal(3 + 1 + 3, ctx.CountOf("DrawLine"));
    }

    [Theory]
    [InlineData(NaN, 0.2)]
    [InlineData(0.2, NaN)]
    public void AMissingXError_SkipsTheXBarAndItsCap_TheOtherCapStays(double low, double high)
    {
        var ctx = Render(c => new ErrorBarSeriesRenderer(c).Render(new ErrorBarSeries([1.0, 2, 3], [1, 2, 3], [0.5, 0.5, 0.5], [0.5, 0.5, 0.5])
        {
            XErrorLow = [0.2, low, 0.2], XErrorHigh = [0.2, high, 0.2],
        }));

        AssertNothingUndrawableReached(ctx);
        Assert.Equal(3, ctx.CountOf("DrawCircle"));
        Assert.Equal(6 + (3 + 1) + 6, ctx.CountOf("DrawLine"));
    }

    // ---- bars ---------------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(BarOrientation.Vertical)]
    [InlineData(BarOrientation.Horizontal)]
    public void ABarOfAMissingValue_IsSkippedWithItsLabel(BarOrientation orientation)
    {
        var ctx = Render(c => new BarSeriesRenderer(c).Render(new BarSeries(["a", "b", "c"], [1, NaN, 3]) { Orientation = orientation, ShowLabels = true }));

        AssertNothingUndrawableReached(ctx);
        Assert.Equal(2, ctx.CountOf("DrawRectangle"));
        Assert.Equal(2, ctx.CountOf("DrawText"));
    }

    [Fact]
    public void ABarAtAMissingX_IsSkipped()
    {
        var ctx = Render(c => new BarSeriesRenderer(c).Render(new BarSeries([1.0, NaN, 3], [1, 2, 3])));

        AssertNothingUndrawableReached(ctx);
        Assert.Equal(2, ctx.CountOf("DrawRectangle"));
    }

    /// <summary>A waterfall step is drawn at the running total, and a total that has met a missing step is not known:
    /// that step and every one after it are skipped (numpy's cumulative sum carries NaN on). The steps before it are
    /// drawn, and so are their connectors, the last one still marking the level the missing step starts from.</summary>
    [Fact]
    public void AWaterfall_DrawsTheStepsBeforeAMissingStep_AndNothingWhoseLevelIsUnknown()
    {
        var ctx = Render(c => new WaterfallSeriesRenderer(c).Render(new WaterfallSeries(["a", "b", "c", "d"], [4, 2, NaN, 1])));

        AssertNothingUndrawableReached(ctx);
        Assert.Equal(2, ctx.CountOf("DrawRectangle"));
        Assert.Equal(2, ctx.CountOf("DrawLine"));
    }

    // ---- a sample: box and violin -------------------------------------------------------------------------------------

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ABox_IsDrawnFromTheObservationsThatAreThere(bool vertical)
    {
        var withGap = Render(c => new BoxSeriesRenderer(c).Render(new BoxSeries([[1.0, 2, NaN, 4, 9, double.PositiveInfinity], [NaN, NaN]]) { Vert = vertical, ShowMeans = true }));
        var without = Render(c => new BoxSeriesRenderer(c).Render(new BoxSeries([[1.0, 2, 4, 9], []]) { Vert = vertical, ShowMeans = true }));

        AssertNothingUndrawableReached(withGap);
        Assert.Equal(Signature(without), Signature(withGap));
    }

    [Fact]
    public void AViolin_IsDrawnFromTheObservationsThatAreThere()
    {
        var withGap = Render(c => new ViolinSeriesRenderer(c).Render(new ViolinSeries([[1.0, 2, NaN, 4, 9, double.NegativeInfinity], [NaN]]) { ShowExtrema = true, ShowMedians = true, ShowMeans = true }));
        var without = Render(c => new ViolinSeriesRenderer(c).Render(new ViolinSeries([[1.0, 2, 4, 9], []]) { ShowExtrema = true, ShowMedians = true, ShowMeans = true }));

        AssertNothingUndrawableReached(withGap);
        Assert.Equal(Signature(without), Signature(withGap));
    }

    /// <summary>A density curve is estimated from the observations that are there: one NaN in the sample made the
    /// bandwidth NaN and every point of the curve and its fill NaN.</summary>
    [Fact]
    public void AKde_IsEstimatedFromTheObservationsThatAreThere()
    {
        var withGap = Render(c => new KdeSeriesRenderer(c).Render(new KdeSeries([1.0, NaN, 3, 4, double.PositiveInfinity])));
        var without = Render(c => new KdeSeriesRenderer(c).Render(new KdeSeries([1.0, 3, 4])));

        AssertNothingUndrawableReached(withGap);
        Assert.Equal(Signature(without), Signature(withGap));
    }

    [Fact]
    public void AKdeWithNoObservationThatIsThere_DrawsNothing() =>
        Assert.Empty(Render(c => new KdeSeriesRenderer(c).Render(new KdeSeries([NaN]))).Calls);

    /// <summary>An ECDF rises by 1/n over the n observations that are there: with a NaN counted, the curve topped out
    /// below 1 or started at a step over a value that was never measured.</summary>
    [Fact]
    public void AnEcdf_IsMadeFromTheObservationsThatAreThere()
    {
        var ecdf = new EcdfSeries([3.0, NaN, 1, double.NegativeInfinity]);

        Assert.Equal([1.0, 3], ecdf.SortedX);
        Assert.Equal([0.5, 1], ecdf.CdfY);
    }

    // ---- a stacked area -----------------------------------------------------------------------------------------------

    /// <summary>matplotlib's stackplot fills each layer with fill_between, which breaks at a masked point; a gap in a
    /// layer is a gap in the edge of every layer stacked on it.</summary>
    [Fact]
    public void AStackedArea_BreaksEveryLayerAtAGap()
    {
        var ctx = Render(c => new StackedAreaSeriesRenderer(c).Render(new StackedAreaSeries([0.0, 1, 2, 3], [[1, 2, NaN, 1], [1, 1, 1, 1]])));

        AssertNothingUndrawableReached(ctx);
        Assert.Equal(4, ctx.CountOf("DrawPolygon"));
    }

    [Fact]
    public void AStackedArea_BreaksAtAMissingX()
    {
        var ctx = Render(c => new StackedAreaSeriesRenderer(c).Render(new StackedAreaSeries([0.0, NaN, 2, 3], [[1, 2, 3, 1], [1, 1, 1, 1]])));

        AssertNothingUndrawableReached(ctx);
        Assert.Equal(4, ctx.CountOf("DrawPolygon"));
    }

    [Fact]
    public void AStackedAreaWithoutAGap_IsOnePolygonPerLayer() =>
        Assert.Equal(2, Render(c => new StackedAreaSeriesRenderer(c).Render(new StackedAreaSeries([0.0, 1, 2], [[1, 2, 1], [1, 1, 1]]))).CountOf("DrawPolygon"));

    // ---- the overlays of an axes --------------------------------------------------------------------------------------

    private static readonly DataRange Range = new(0, 10, 0, 10);

    [Fact]
    public void AReferenceLineAtAMissingValue_IsSkippedWithItsLabel()
    {
        var axes = new Axes();
        axes.AxHLine(NaN).Label = "h";
        axes.AxVLine(NaN).Label = "v";
        axes.AxHLine(5).Label = "drawn";
        var ctx = new RecordingRenderContext();

        new CartesianReferenceLinesPart(axes, Bounds, ctx, Theme.Default, Linear(), Range).Render();

        AssertNothingUndrawableReached(ctx);
        Assert.Equal(1, ctx.CountOf("DrawLine"));
        Assert.Equal("drawn", ((dynamic)Assert.Single(ctx.OfKind("DrawText")).Args!).text);
    }

    [Fact]
    public void ASpanWithAMissingEdge_IsSkippedWithItsBorderAndLabel()
    {
        var axes = new Axes();
        foreach (var span in new[] { axes.AxHSpan(NaN, 2), axes.AxVSpan(1, NaN), axes.AxHSpan(1, 2) })
        {
            span.Label = "s";
            span.LineStyle = LineStyle.Solid;
        }
        var ctx = new RecordingRenderContext();

        new CartesianSpansPart(axes, Bounds, ctx, Theme.Default, Linear(), Range).Render();

        AssertNothingUndrawableReached(ctx);
        Assert.Equal(1, ctx.CountOf("DrawRectangle"));
        Assert.Equal(4, ctx.CountOf("DrawLine"));
        Assert.Equal(1, ctx.CountOf("DrawText"));
    }

    [Fact]
    public void ATrendlineLevelOrRetracementAtAMissingValue_IsSkippedWithItsLabel()
    {
        var axes = new Axes();
        axes.AddTrendline(0, 1, 5, NaN).Label = "t";
        var extended = axes.AddTrendline(0, 1, 5, NaN);
        extended.Label = "te";
        extended.IsExtended = true;
        axes.AddLevel(NaN).Label = "l";
        axes.AddFibonacci(NaN, 1);
        axes.AddTrendline(0, 1, 5, 6);
        var ctx = new RecordingRenderContext();

        new CartesianToolsPart(axes, Bounds, ctx, Theme.Default, Linear()).Render();

        AssertNothingUndrawableReached(ctx);
        Assert.Equal(1, ctx.CountOf("DrawLine"));
        Assert.Equal(0, ctx.CountOf("DrawText"));
    }

    [Fact]
    public void ASignalAtAMissingValue_IsSkipped()
    {
        var axes = new Axes();
        axes.AddSignal(2, NaN, SignalDirection.Buy);
        axes.AddSignal(NaN, 3, SignalDirection.Sell);
        axes.AddSignal(2, 3, SignalDirection.Buy);
        var ctx = new RecordingRenderContext();

        new CartesianSignalsPart(axes, Bounds, ctx, Theme.Default, Linear()).Render();

        AssertNothingUndrawableReached(ctx);
        Assert.Equal(1, ctx.CountOf("DrawPolygon"));
    }

    /// <summary>An annotation stands at its text position: there it is skipped whole (text, box and arrow, which starts
    /// at the text). An arrow pointing at a coordinate that cannot be drawn is skipped, its text stays.</summary>
    [Fact]
    public void AnAnnotationAtAMissingValue_IsSkipped_AnArrowToOneLosesOnlyTheArrow()
    {
        var axes = new Axes();
        axes.Annotate("a", 2, NaN).BackgroundColor = Colors.White;
        var fromGap = axes.Annotate("b", NaN, 3);
        fromGap.ArrowTargetX = 5;
        fromGap.ArrowTargetY = 5;
        fromGap.ArrowStyle = ArrowStyle.Simple;
        var toGap = axes.Annotate("c", 2, 3);
        toGap.ArrowTargetX = NaN;
        toGap.ArrowTargetY = 4;
        toGap.ArrowStyle = ArrowStyle.Simple;
        axes.Annotate("d", 4, 4);
        var ctx = new RecordingRenderContext();

        new CartesianAnnotationsPart(axes, Bounds, ctx, Theme.Default, Linear()).Render();

        AssertNothingUndrawableReached(ctx);
        Assert.Equal(["c", "d"], ctx.OfKind("DrawText").Select(c => (string)((dynamic)c.Args!).text));
        Assert.Equal(0, ctx.CountOf("DrawPath") + ctx.CountOf("DrawPolygon") + ctx.CountOf("DrawRectangle"));
    }

    // ---- the legend ---------------------------------------------------------------------------------------------------

    /// <summary>The legend's value is the value at the end of the line, by the rule the end-of-line label uses: the
    /// last point that can be drawn. A trailing gap no longer writes "= NaN".</summary>
    [Theory]
    [InlineData(new[] { 0.0, 1, 2 }, new[] { 1.0, 2, NaN }, "a = 2.00")]
    [InlineData(new[] { 0.0, 1, NaN }, new[] { 1.0, 2, 3 }, "a = 2.00")]
    [InlineData(new[] { 0.0, 1 }, new[] { NaN, NaN }, "a")]
    public void TheLegendValue_IsTheLastValueThatCanBeDrawn(double[] x, double[] y, string expected)
    {
        var axes = new Axes();
        axes.Legend = axes.Legend with { LegendValues = true };
        axes.Plot(x, y).Label = "a";

        Assert.Equal(expected, Assert.Single(axes.LegendEntries(directLabelsDrawn: false)).Label);
    }

    // ---- contour: a cell with a missing corner is masked, the levels come from the values that are there --------------

    /// <summary>Two cells side by side; the left one has a missing corner. The finite values span 1..4 in both grids, so
    /// the levels are the same; the masked cell draws nothing, and the right cell is traced exactly as on its own.</summary>
    private static readonly double[] TwoCellsX = [0, 1, 2];
    private static readonly double[,] TwoCellsWithAMissingCorner = { { NaN, 1, 2 }, { 1.5, 3, 4 } };
    private static readonly double[] RightCellX = [1, 2];
    private static readonly double[,] RightCell = { { 1, 2 }, { 3, 4 } };

    /// <summary>matplotlib masks a contour cell with a missing corner. Before, the levels were taken with Math.Min over
    /// the NaN, every level came out NaN, and a grid with one missing value drew no line at all.</summary>
    [Theory]
    [InlineData(NaN)]
    [InlineData(double.PositiveInfinity)]
    public void AContour_MasksACellWithAMissingCorner_AndTakesItsLevelsFromTheValuesThatAreThere(double missing)
    {
        var z = (double[,])TwoCellsWithAMissingCorner.Clone();
        z[0, 0] = missing;
        var withGap = Render(c => new ContourSeriesRenderer(c).Render(new ContourSeries(TwoCellsX, [0, 1], z) { Levels = 4, ShowLabels = true }));
        var without = Render(c => new ContourSeriesRenderer(c).Render(new ContourSeries(RightCellX, [0, 1], RightCell) { Levels = 4, ShowLabels = true }));

        AssertNothingUndrawableReached(withGap);
        Assert.NotEmpty(Signature(withGap, "DrawLines"));
        Assert.Equal(Signature(without), Signature(withGap));
    }

    /// <summary>A grid line that cannot be placed masks the cells on both sides of it.</summary>
    [Fact]
    public void AContour_MasksTheCellsOfAGridLineThatCannotBePlaced()
    {
        var withGap = Render(c => new ContourSeriesRenderer(c).Render(new ContourSeries([NaN, 1, 2], [0, 1], new double[,] { { 3, 1, 2 }, { 1.5, 3, 4 } }) { Levels = 4 }));
        var without = Render(c => new ContourSeriesRenderer(c).Render(new ContourSeries(RightCellX, [0, 1], RightCell) { Levels = 4 }));

        AssertNothingUndrawableReached(withGap);
        Assert.Equal(Signature(without), Signature(withGap));
    }

    /// <summary>A contour filled through its heatmap underlay traces the same lines as unfilled, and its missing cell
    /// takes the colour map's bad colour (transparent), as a heatmap's does.</summary>
    [Fact]
    public void AFilledContour_TracesTheSameLines_AndLeavesTheMissingCellTransparent()
    {
        var filled = Render(c => new ContourSeriesRenderer(c).Render(new ContourSeries(TwoCellsX, [0, 1], TwoCellsWithAMissingCorner) { Levels = 4, Filled = true }));
        var lines = Render(c => new ContourSeriesRenderer(c).Render(new ContourSeries(TwoCellsX, [0, 1], TwoCellsWithAMissingCorner) { Levels = 4 }));

        AssertNothingUndrawableReached(filled);
        Assert.Equal(Signature(lines, "DrawLines"), Signature(filled, "DrawLines"));
        var fills = filled.Calls.Where(c => c.Kind == "DrawRectangle")
            .Select(c => (Color?)c.Args!.GetType().GetProperty("fill")!.GetValue(c.Args)).ToList();
        Assert.Contains(fills, f => f is { A: 0 });
    }

    [Fact]
    public void AContourWithNoValueThatIsThere_DrawsNothing() =>
        Assert.Empty(Render(c => new ContourSeriesRenderer(c).Render(new ContourSeries([0, 1], [0, 1], new double[,] { { NaN, NaN }, { NaN, NaN } }))).Calls);

    /// <summary>A filled contour's bands are traced as without the masked cell, and the masked cell is left empty: the
    /// lowest band no longer paints the whole plot under it (it painted a value that was never measured).</summary>
    [Fact]
    public void AFilledContour_TracesItsBandsAsWithoutTheMaskedCell_AndLeavesThatCellEmpty()
    {
        var withGap = Render(c => new ContourfSeriesRenderer(c).Render(new ContourfSeries(TwoCellsX, [0, 1], TwoCellsWithAMissingCorner) { Levels = 4 }));
        var without = Render(c => new ContourfSeriesRenderer(c).Render(new ContourfSeries(RightCellX, [0, 1], RightCell) { Levels = 4 }));

        AssertNothingUndrawableReached(withGap);
        Assert.Equal(Signature(without, "DrawPolygon"), Signature(withGap, "DrawPolygon"));
        Assert.Equal(Signature(without, "DrawLines"), Signature(withGap, "DrawLines"));

        // the lowest band covers the right cell, x 1..2 (pixels 40..80), and nothing of the masked left cell, x 0..1
        var bottom = Assert.Single(withGap.Calls, c => c.Kind == "DrawRectangle").Args!;
        var rect = (Rect)bottom.GetType().GetProperty("rect")!.GetValue(bottom)!;
        Assert.Equal(40, rect.X, 9);
        Assert.Equal(40, rect.Width, 9);
    }

    [Fact]
    public void AFilledContourWithoutAMissingValue_PaintsItsLowestBandUnderTheWholePlot()
    {
        var ctx = Render(c => new ContourfSeriesRenderer(c).Render(new ContourfSeries(RightCellX, [0, 1], RightCell) { Levels = 4 }));

        var bottom = Assert.Single(ctx.Calls, c => c.Kind == "DrawRectangle").Args!;
        Assert.Equal(Bounds, (Rect)bottom.GetType().GetProperty("rect")!.GetValue(bottom)!);
    }

    /// <summary>A triangle with a vertex that is missing (in z, or in x or y) is masked; the levels come from the values
    /// that are there. Points A, B, C and D: the triangulation is ABC and BCD, and D carries the missing value, so the
    /// contour is traced exactly as for ABC alone. Before, the levels were NaN and nothing was drawn; a missing x or y
    /// made the whole triangulation empty.</summary>
    [Theory]
    [InlineData(10, 0, NaN)]
    [InlineData(10, NaN, 3.5)]
    [InlineData(NaN, 0, 3.5)]
    [InlineData(10, double.PositiveInfinity, 3.5)]
    public void ATriangleContour_MasksATriangleWithAMissingVertex(double dx, double dy, double dz)
    {
        var withGap = Render(c => new TricontourSeriesRenderer(c).Render(new TricontourSeries(new double[] { 0, 1, 1, dx }, new double[] { 0, 1, -1, dy }, new double[] { 1, 4, 2, dz }) { Levels = 5 }));
        var without = Render(c => new TricontourSeriesRenderer(c).Render(new TricontourSeries(new double[] { 0, 1, 1 }, new double[] { 0, 1, -1 }, new double[] { 1, 4, 2 }) { Levels = 5 }));

        AssertNothingUndrawableReached(withGap);
        Assert.NotEmpty(Signature(withGap, "DrawLine"));
        // the triangulation nudges near-collinear points by 1e-10 of the extent, which D widens: equal to 6 decimals
        Assert.Equal(RoundedSignature(without), RoundedSignature(withGap));
    }

    /// <summary><see cref="Signature"/> with every coordinate rounded to 6 decimals.</summary>
    private static List<string> RoundedSignature(RecordingRenderContext ctx) =>
        [.. ctx.Calls.Select(c => c.Kind + "(" + string.Join("; ", c.Args?.GetType().GetProperties().Select(p => p.GetValue(c.Args) switch
        {
            Point pt => Math.Round(pt.X, 6).ToString(CultureInfo.InvariantCulture) + "," + Math.Round(pt.Y, 6).ToString(CultureInfo.InvariantCulture),
            var v => Describe(v),
        }) ?? []) + ")")];

    /// <summary>A point that cannot be placed is no vertex: the others are triangulated as without it (before, one NaN
    /// coordinate made the bounding super-triangle NaN and the whole mesh empty).</summary>
    [Fact]
    public void TheTriangulation_LeavesOutAPointThatCannotBePlaced_AndKeepsTheIndicesOfTheOthers()
    {
        var mesh = MatPlotLibNet.Numerics.Delaunay.Triangulate([0, NaN, 1, 1, 10], [0, 5, 1, -1, 0]);
        var twin = MatPlotLibNet.Numerics.Delaunay.Triangulate([0, 1, 1, 10], [0, 1, -1, 0]);

        int[] mapped = [.. twin.Triangles.Select(i => i == 0 ? 0 : i + 1)];
        Assert.Equal(mapped, mesh.Triangles);
        Assert.DoesNotContain(1, mesh.Triangles);
    }

    // ---- every other mark: skipped when it cannot be placed, the others drawn as without it ---------------------------

    /// <summary>A series with a value that cannot be drawn and its twin without that mark.</summary>
    private readonly record struct MarkCase(Action<SeriesRenderContext> WithGap, Action<SeriesRenderContext> Twin);

    private static readonly Dictionary<string, MarkCase> MarkCases = new()
    {
        // a cell with an edge that cannot be placed is skipped; the colours span the same values in both
        ["pcolormesh"] = new(c => new PcolormeshSeriesRenderer(c).Render(new PcolormeshSeries(new double[] { 0, NaN, 2, 3, 4 }, new double[] { 0, 1 }, new double[,] { { 1, 3, 3, 1 } })),
            c => new PcolormeshSeriesRenderer(c).Render(new PcolormeshSeries(new double[] { 2, 3, 4 }, new double[] { 0, 1 }, new double[,] { { 3, 1 } }))),
        ["eventplot"] = new(c => new EventplotSeriesRenderer(c).Render(new EventplotSeries([[1, NaN, 3, double.PositiveInfinity]])),
            c => new EventplotSeriesRenderer(c).Render(new EventplotSeries([[1, 3]]))),
        ["gantt"] = new(c => new GanttSeriesRenderer(c).Render(new GanttSeries(["a", "b"], [0, NaN], [1, 4])),
            c => new GanttSeriesRenderer(c).Render(new GanttSeries(["a"], [0], [1]))),
        ["rugplot"] = new(c => new RugplotSeriesRenderer(c).Render(new RugplotSeries(new double[] { 1, NaN, 5 })),
            c => new RugplotSeriesRenderer(c).Render(new RugplotSeries(new double[] { 1, 5 }))),
        // a barb at a position or a direction that cannot be placed is skipped with its flags and barbs
        ["barbs"] = new(c => new BarbsSeriesRenderer(c).Render(new BarbsSeries(new double[] { 1, NaN, 3, 4 }, new double[] { 1, 2, 3, 4 }, new double[] { 65, 65, 65, 65 }, new double[] { 0, 0, 0, NaN })),
            c => new BarbsSeriesRenderer(c).Render(new BarbsSeries(new double[] { 1, 3 }, new double[] { 1, 3 }, new double[] { 65, 65 }, new double[] { 0, 0 }))),
        ["quiver"] = new(c => new QuiverSeriesRenderer(c).Render(new QuiverSeries([1, 2, 3], [1, 2, 3], [1, NaN, 1], [0, 0, 0])),
            c => new QuiverSeriesRenderer(c).Render(new QuiverSeries([1, 3], [1, 3], [1, 1], [0, 0]))),
        ["broken-bar"] = new(c => new BrokenBarSeriesRenderer(c).Render(new BrokenBarSeries([[new(1, 2), new(NaN, 1), new(5, 1)]])),
            c => new BrokenBarSeriesRenderer(c).Render(new BrokenBarSeries([[new(1, 2), new(5, 1)]]))),
        ["state-timeline"] = new(c => new StateTimelineSeriesRenderer(c).Render(new StateTimelineSeries([new(0, 1, "a", Colors.Red), new(NaN, 3, "b", Colors.Blue), new(3, 4, "c", Colors.Green)])),
            c => new StateTimelineSeriesRenderer(c).Render(new StateTimelineSeries([new(0, 1, "a", Colors.Red), new(3, 4, "c", Colors.Green)]))),
        // a fit is made over the pairs that are there (one NaN made every coefficient NaN: no line, NaN markers)
        ["regression"] = new(c => new RegressionSeriesRenderer(c).Render(new RegressionSeries([1, 2, NaN, 4, 5], [1, 2.5, 3, 4, 4.5]) { ShowConfidence = true }),
            c => new RegressionSeriesRenderer(c).Render(new RegressionSeries([1, 2, 4, 5], [1, 2.5, 4, 4.5]) { ShowConfidence = true })),
        ["residual"] = new(c => new ResidualSeriesRenderer(c).Render(new ResidualSeries(new double[] { 1, 2, NaN, 4, 5 }, new double[] { 1, 2.5, 3, 4, double.PositiveInfinity })),
            c => new ResidualSeriesRenderer(c).Render(new ResidualSeries(new double[] { 1, 2, 4 }, new double[] { 1, 2.5, 4 }))),
        // a point that cannot be placed is not binned (it piled into one bin, or a bin far off the plot)
        ["hexbin"] = new(c => new HexbinSeriesRenderer(c).Render(new HexbinSeries([1, NaN, 3, 2, 5], [1, 2, 3, 2, double.NegativeInfinity])),
            c => new HexbinSeriesRenderer(c).Render(new HexbinSeries([1, 3, 2], [1, 3, 2]))),
        ["histogram2d"] = new(c => new Histogram2DSeriesRenderer(c).Render(new Histogram2DSeries([1, NaN, 3, 2, 5], [1, 2, 3, 2, double.NegativeInfinity], 4, 4)),
            c => new Histogram2DSeriesRenderer(c).Render(new Histogram2DSeries([1, 3, 2], [1, 3, 2], 4, 4))),
        // a polar mark at a radius or an angle that cannot be placed is skipped, and the radial scale spans the radii that
        // can be (one infinite radius put every other mark at the centre)
        ["polar-scatter"] = new(c => new PolarScatterSeriesRenderer(c).Render(new PolarScatterSeries([1, NaN, 2, double.PositiveInfinity, 3, 2], [0, 1, 2, 3, 4, NaN])),
            c => new PolarScatterSeriesRenderer(c).Render(new PolarScatterSeries([1, 2, 3], [0, 2, 4]))),
        ["polar-bar"] = new(c => new PolarBarSeriesRenderer(c).Render(new PolarBarSeries([5, NaN, 10, double.PositiveInfinity, 4], [0, 1, 2, 3, NaN])),
            c => new PolarBarSeriesRenderer(c).Render(new PolarBarSeries([5, 10], [0, 2]))),
        // a polar line breaks at a point it cannot place, as a line does; a point alone between two gaps draws no line
        ["polar-line"] = new(c => new PolarLineSeriesRenderer(c).Render(new PolarLineSeries([1, NaN, 2, 3, double.PositiveInfinity], [0, 1, 2, 3, 4])),
            c => new PolarLineSeriesRenderer(c).Render(new PolarLineSeries([2, 3], [2, 3]))),
        // a bubble at a point that cannot be placed, or whose size is no area (missing, infinite, negative), is skipped:
        // the scatter's rule (matplotlib masks x, y and s together)
        ["bubble"] = new(c => new BubbleSeriesRenderer(c).Render(new BubbleSeries([1, NaN, 3, 4, 5, 6, 7], [1, 2, NaN, 4, double.PositiveInfinity, 6, 7], [100, 100, 100, NaN, 100, -50, 400])),
            c => new BubbleSeriesRenderer(c).Render(new BubbleSeries([1, 7], [1, 7], [100, 400]))),
    };

    public static TheoryData<string> MarkKinds { get; } = new(MarkCases.Keys);

    /// <summary>Before, each of these drew its mark at NaN (a cell, an event tick, a task bar, a rug tick, a barb, an
    /// arrow, a broken-bar segment, a state segment and its label), drew nothing at all (a regression), or binned a point
    /// it could not place.</summary>
    [Theory]
    [MemberData(nameof(MarkKinds))]
    public void AMarkThatCannotBePlaced_IsSkipped_TheOthersAreDrawnAsWithoutIt(string kind)
    {
        var withGap = Render(MarkCases[kind].WithGap);
        var without = Render(MarkCases[kind].Twin);

        AssertNothingUndrawableReached(withGap);
        Assert.NotEmpty(withGap.Calls);
        Assert.Equal(Signature(without), Signature(withGap));
    }

    /// <summary>A grid's colours and its colour bar span the values a colour scale can place, as an image's and a scatter's
    /// already do: one infinite cell stretched the range to infinity, so every finite cell took the lowest colour. The
    /// infinite cell itself takes the end colour the normaliser clamps it to (the colormap's rule).</summary>
    [Fact]
    public void AGridsColours_SpanTheValuesThatCanBePlaced()
    {
        var withInfinity = Render(c => new HeatmapSeriesRenderer(c).Render(new HeatmapSeries(new double[,] { { 1, 2 }, { 3, double.PositiveInfinity } })));
        var finite = Render(c => new HeatmapSeriesRenderer(c).Render(new HeatmapSeries(new double[,] { { 1, 2 }, { 3, 3 } })));

        Assert.Equal(Signature(finite, "DrawRectangle"), Signature(withInfinity, "DrawRectangle"));
        Assert.Equal(new MatPlotLibNet.Numerics.MinMaxRange(1, 3), new HeatmapSeries(new double[,] { { 1, double.NegativeInfinity }, { 3, NaN } }).GetColorBarRange());
    }

    /// <summary>A sparkline is scaled to the values that are there and breaks at a gap: one NaN made its whole scale NaN,
    /// so nothing of it was drawn.</summary>
    [Fact]
    public void ASparkline_IsScaledToTheValuesThatAreThere_AndBreaksAtAGap()
    {
        var ctx = Render(c => new SparklineSeriesRenderer(c).Render(new SparklineSeries([1, NaN, 3, 2])));

        AssertNothingUndrawableReached(ctx);
        var run = (Point[])Assert.Single(ctx.Calls, c => c.Kind == "DrawLines").Args!.GetType().GetProperty("points")!.GetValue(Assert.Single(ctx.Calls, c => c.Kind == "DrawLines").Args)!;
        Assert.Equal(2, run.Length);           // the run after the gap; the lone first point draws no line
        Assert.Equal(Bounds.Y, run[0].Y, 9);   // 3, the highest value that is there, at the top
    }

    public static TheoryData<string> NothingDrawable2DKinds { get; } = new(
        "pcolormesh", "eventplot", "gantt", "rugplot", "barbs", "quiver", "broken-bar", "state-timeline", "regression", "residual",
        "hexbin", "histogram2d", "sparkline", "candlestick", "ohlc-bar", "contourf", "tricontour", "tripcolor",
        "polar-scatter", "polar-bar", "polar-line", "bubble", "radar", "pie", "donut", "funnel");

    private static void DrawNothingDrawable2D(string kind, SeriesRenderContext c)
    {
        double[] none = [NaN, NaN, NaN];
        switch (kind)
        {
            case "pcolormesh": new PcolormeshSeriesRenderer(c).Render(new PcolormeshSeries(none, none, new double[2, 2])); break;
            case "eventplot": new EventplotSeriesRenderer(c).Render(new EventplotSeries([none])); break;
            case "gantt": new GanttSeriesRenderer(c).Render(new GanttSeries(["a", "b", "c"], none, none)); break;
            case "rugplot": new RugplotSeriesRenderer(c).Render(new RugplotSeries(none)); break;
            case "barbs": new BarbsSeriesRenderer(c).Render(new BarbsSeries(none, none, none, none)); break;
            case "quiver": new QuiverSeriesRenderer(c).Render(new QuiverSeries(none, none, none, none)); break;
            case "broken-bar": new BrokenBarSeriesRenderer(c).Render(new BrokenBarSeries([[new(NaN, 1)]])); break;
            case "state-timeline": new StateTimelineSeriesRenderer(c).Render(new StateTimelineSeries([new(NaN, NaN, "a", Colors.Red)])); break;
            case "regression": new RegressionSeriesRenderer(c).Render(new RegressionSeries(none, none)); break;
            case "residual": new ResidualSeriesRenderer(c).Render(new ResidualSeries(none, none)); break;
            case "hexbin": new HexbinSeriesRenderer(c).Render(new HexbinSeries(none, none)); break;
            case "histogram2d": new Histogram2DSeriesRenderer(c).Render(new Histogram2DSeries(none, none)); break;
            case "sparkline": new SparklineSeriesRenderer(c).Render(new SparklineSeries(none)); break;
            case "candlestick": new CandlestickSeriesRenderer(c).Render(new CandlestickSeries(none, none, none, none)); break;
            case "ohlc-bar": new OhlcBarSeriesRenderer(c).Render(new OhlcBarSeries(none, none, none, none)); break;
            case "contourf": new ContourfSeriesRenderer(c).Render(new ContourfSeries([0, 1], [0, 1], new double[,] { { NaN, NaN }, { NaN, NaN } })); break;
            case "tricontour": new TricontourSeriesRenderer(c).Render(new TricontourSeries(none, none, none)); break;
            case "polar-scatter": new PolarScatterSeriesRenderer(c).Render(new PolarScatterSeries(none, none)); break;
            case "polar-bar": new PolarBarSeriesRenderer(c).Render(new PolarBarSeries(none, none)); break;
            case "polar-line": new PolarLineSeriesRenderer(c).Render(new PolarLineSeries(none, none)); break;
            case "bubble": new BubbleSeriesRenderer(c).Render(new BubbleSeries(none, none, none)); break;
            case "radar": new RadarSeriesRenderer(c).Render(new RadarSeries(["a", "b", "c"], none)); break;
            case "pie": new PieSeriesRenderer(c).Render(new PieSeries(none) { AutoPct = "{0:F0}%", Labels = ["a", "b", "c"] }); break;
            case "donut": new DonutSeriesRenderer(c).Render(new DonutSeries(none)); break;
            case "funnel": new FunnelSeriesRenderer(c).Render(new FunnelSeries(["a", "b", "c"], none)); break;
            default: new TripcolorSeriesRenderer(c).Render(new TripcolorSeries(none, none, none)); break;
        }
    }

    /// <summary>A series none of whose values can be placed draws nothing it cannot place, and does not throw (a hexbin
    /// with no point it could bin asked Max() of no bins).</summary>
    [Theory]
    [MemberData(nameof(NothingDrawable2DKinds))]
    public void ASeriesWithNothingDrawable_DrawsNothingItCannotPlace(string kind) =>
        AssertNothingUndrawableReached(Render(c => DrawNothingDrawable2D(kind, c)));

    /// <summary>A candle's wick (low to high) and body (open to close) are marks of their own, as an error bar's bar and
    /// caps are: a missing high skips the wick and keeps the body, a missing open skips the body and keeps the wick. An
    /// OHLC bar's line, open tick and close tick likewise.</summary>
    [Fact]
    public void ACandleOrOhlcPartThatCannotBePlaced_IsSkipped_TheOtherPartsStay()
    {
        double[] open = [5, NaN, 5], high = [8, 8, NaN], low = [2, 2, 2], close = [6, 6, 6];
        var candles = Render(c => new CandlestickSeriesRenderer(c).Render(new CandlestickSeries(open, high, low, close)));
        var ohlc = Render(c => new OhlcBarSeriesRenderer(c).Render(new OhlcBarSeries(open, high, low, close)));

        AssertNothingUndrawableReached(candles);
        AssertNothingUndrawableReached(ohlc);
        Assert.Equal(2, candles.CountOf("DrawLine"));       // wicks of candles 0 and 1
        Assert.Equal(2, candles.CountOf("DrawRectangle"));  // bodies of candles 0 and 2
        Assert.Equal(2 + 2 + 3, ohlc.CountOf("DrawLine"));  // lines 0, 1; open ticks 0, 2; close ticks 0, 1, 2
    }

    // ---- 3-D: a vertex that cannot be placed is skipped with every face, segment and mark that needs it -----------------

    /// <summary>The projection of a 3-D axes over 0..10 on every axis, as the 3-D axes hands it to its series.</summary>
    private static readonly Projection3D Box10 = new(30, -60, Bounds, 0, 10, 0, 10, 0, 10);

    /// <summary>Renders with the 3-D axes' projection, or (<paramref name="axesProjection"/> false) as a 3-D series placed
    /// on 2-D axes, which fits a projection to its own data.</summary>
    private static RecordingRenderContext Render3D(Action<SeriesRenderContext> draw, bool axesProjection)
    {
        var ctx = new RecordingRenderContext();
        draw(new SeriesRenderContext(Linear(), ctx, Colors.Tab10Blue, new RenderArea(Bounds, ctx)) { Projection3D = axesProjection ? Box10 : null });
        return ctx;
    }

    /// <summary>A 3-D series with a value that cannot be drawn and its twin without it. Where the drawable extent of the
    /// two is the same (<see cref="SameExtent"/>), they also draw the same on 2-D axes, where the series fits its own
    /// projection to that extent.</summary>
    private readonly record struct ThreeDCase(Action<SeriesRenderContext> WithGap, Action<SeriesRenderContext> Twin, bool SameExtent);

    private static readonly double[,] SurfaceWithAMissingCorner = { { NaN, 1, 2 }, { 1.5, 3, 4 } };
    private static readonly double[,] SurfaceUnderAMissingGridLine = { { 3, 1, 2 }, { 1.5, 3, 4 } };

    private static readonly Dictionary<string, ThreeDCase> ThreeDCases = new()
    {
        ["scatter3d"] = new(c => new Scatter3DSeriesRenderer(c).Render(new Scatter3DSeries(new double[] { 1, NaN, 3, 2 }, new double[] { 1, 2, 3, 2 }, new double[] { 1, 2, 3, double.PositiveInfinity })),
            c => new Scatter3DSeriesRenderer(c).Render(new Scatter3DSeries(new double[] { 1, 3 }, new double[] { 1, 3 }, new double[] { 1, 3 })), true),
        // the colours span the values that are there (one NaN made every colour the transparent bad colour)
        ["scatter3d-colormap"] = new(c => new Scatter3DSeriesRenderer(c).Render(new Scatter3DSeries(new double[] { 1, 3, 2 }, new double[] { 1, 3, 2 }, new double[] { 1, 3, NaN }) { ColorMap = MatPlotLibNet.Styling.ColorMaps.ColorMaps.Viridis }),
            c => new Scatter3DSeriesRenderer(c).Render(new Scatter3DSeries(new double[] { 1, 3 }, new double[] { 1, 3 }, new double[] { 1, 3 }) { ColorMap = MatPlotLibNet.Styling.ColorMaps.ColorMaps.Viridis }), true),
        ["line3d"] = new(c => new Line3DSeriesRenderer(c).Render(new Line3DSeries(new double[] { 3, NaN, 3, 4 }, new double[] { 3, 3.5, 3, 4 }, new double[] { 3, 3.5, 3, 4 })),
            c => new Line3DSeriesRenderer(c).Render(new Line3DSeries(new double[] { 3, 4 }, new double[] { 3, 4 }, new double[] { 3, 4 })), true),
        ["trisurf"] = new(c => new Trisurf3DSeriesRenderer(c).Render(new Trisurf3DSeries(new double[] { 0, 1, 0.5, 0.2, 0.8, 0.5 }, new double[] { 0, 0, 1, 0.2, 0.2, 0.8 }, new double[] { 1, 2, 3, 2, NaN, 2.5 }) { ColorMap = MatPlotLibNet.Styling.ColorMaps.ColorMaps.Viridis }),
            c => new Trisurf3DSeriesRenderer(c).Render(new Trisurf3DSeries(new double[] { 0, 1, 0.5 }, new double[] { 0, 0, 1 }, new double[] { 1, 2, 3 }) { ColorMap = MatPlotLibNet.Styling.ColorMaps.ColorMaps.Viridis }), true),
        ["surface-corner"] = new(c => new SurfaceSeriesRenderer(c).Render(new SurfaceSeries([0, 1, 2], [0, 1], SurfaceWithAMissingCorner) { ShowWireframe = true }),
            c => new SurfaceSeriesRenderer(c).Render(new SurfaceSeries(RightCellX, [0, 1], RightCell) { ShowWireframe = true }), false),
        ["surface-gridline"] = new(c => new SurfaceSeriesRenderer(c).Render(new SurfaceSeries([NaN, 1, 2], [0, 1], SurfaceUnderAMissingGridLine)),
            c => new SurfaceSeriesRenderer(c).Render(new SurfaceSeries(RightCellX, [0, 1], RightCell)), true),
        ["contour3d-corner"] = new(c => new Contour3DSeriesRenderer(c).Render(new Contour3DSeries([0, 1, 2], [0, 1], SurfaceWithAMissingCorner) { Levels = 4 }),
            c => new Contour3DSeriesRenderer(c).Render(new Contour3DSeries(RightCellX, [0, 1], RightCell) { Levels = 4 }), false),
        ["contour3d-gridline"] = new(c => new Contour3DSeriesRenderer(c).Render(new Contour3DSeries([NaN, 1, 2], [0, 1], SurfaceUnderAMissingGridLine) { Levels = 4 }),
            c => new Contour3DSeriesRenderer(c).Render(new Contour3DSeries(RightCellX, [0, 1], RightCell) { Levels = 4 }), true),
        ["bar3d"] = new(c => new Bar3DSeriesRenderer(c).Render(new Bar3DSeries(new double[] { 1, NaN, 3, 2 }, new double[] { 1, 2, 3, 2 }, new double[] { 1, 2, 3, double.NaN })),
            c => new Bar3DSeriesRenderer(c).Render(new Bar3DSeries(new double[] { 1, 3 }, new double[] { 1, 3 }, new double[] { 1, 3 })), true),
        ["planar-bar3d"] = new(c => new PlanarBar3DSeriesRenderer(c).Render(new PlanarBar3DSeries(new double[] { 1, NaN, 3, 2 }, new double[] { 1, 2, 3, 2 }, new double[] { 1, 2, 3, double.NegativeInfinity })),
            c => new PlanarBar3DSeriesRenderer(c).Render(new PlanarBar3DSeries(new double[] { 1, 3 }, new double[] { 1, 3 }, new double[] { 1, 3 })), true),
        ["quiver3d"] = new(c => new Quiver3DSeriesRenderer(c).Render(new Quiver3DSeries(new double[] { 1, 2 }, new double[] { 1, 2 }, new double[] { 1, 2 }, new double[] { 1, NaN }, new double[] { 0, 0 }, new double[] { 0, 0 })),
            c => new Quiver3DSeriesRenderer(c).Render(new Quiver3DSeries(new double[] { 1 }, new double[] { 1 }, new double[] { 1 }, new double[] { 1 }, new double[] { 0 }, new double[] { 0 })), true),
        ["text3d"] = new(c => new Text3DSeriesRenderer(c).Render(new Text3DSeries([new(1, 1, 1, "a"), new(2, NaN, 2, "b"), new(3, 3, 3, "c")])),
            c => new Text3DSeriesRenderer(c).Render(new Text3DSeries([new(1, 1, 1, "a"), new(3, 3, 3, "c")])), true),
    };

    public static TheoryData<string, bool> ThreeDKinds
    {
        get
        {
            var data = new TheoryData<string, bool>();
            foreach (var kv in ThreeDCases)
            {
                data.Add(kv.Key, true);
                if (kv.Value.SameExtent) data.Add(kv.Key, false);
            }

            return data;
        }
    }

    /// <summary>The number of points of each polyline drawn, in order.</summary>
    private static List<int> PolylineLengths(RecordingRenderContext ctx) =>
        [.. ctx.Calls.Where(c => c.Kind == "DrawLines").Select(c => ((Point[])c.Args!.GetType().GetProperty("points")!.GetValue(c.Args)!).Length)];

    /// <summary>Before, every 3-D renderer projected a missing value: NaN corners, segments, markers and labels, and on 2-D
    /// axes a projection fitted with LINQ Min over the NaN made every coordinate NaN (a surface wrote 8 NaN tokens, a 3-D
    /// bar 144).</summary>
    [Theory]
    [MemberData(nameof(ThreeDKinds))]
    public void A3DMarkWithAVertexThatCannotBePlaced_IsSkipped_TheOthersAreDrawnAsWithoutIt(string kind, bool axesProjection)
    {
        var withGap = Render3D(ThreeDCases[kind].WithGap, axesProjection);
        var without = Render3D(ThreeDCases[kind].Twin, axesProjection);

        AssertNothingUndrawableReached(withGap);
        Assert.NotEmpty(withGap.Calls);
        Assert.Equal(Signature(without), Signature(withGap));
    }

    private static readonly double[] TwoMissing = [NaN, NaN];
    private static readonly double[,] FourMissing = { { NaN, NaN }, { NaN, NaN } };

    public static TheoryData<string> NothingDrawable3DKinds { get; } = new(
        "scatter3d", "line3d", "stem3d", "trisurf", "surface", "wireframe", "contour3d", "bar3d", "planar-bar3d", "quiver3d", "text3d");

    private static void DrawNothingDrawable(string kind, SeriesRenderContext c)
    {
        switch (kind)
        {
            case "scatter3d": new Scatter3DSeriesRenderer(c).Render(new Scatter3DSeries(TwoMissing, TwoMissing, TwoMissing)); break;
            case "line3d": new Line3DSeriesRenderer(c).Render(new Line3DSeries(TwoMissing, TwoMissing, TwoMissing)); break;
            case "stem3d": new Stem3DSeriesRenderer(c).Render(new Stem3DSeries(TwoMissing, TwoMissing, TwoMissing)); break;
            case "trisurf": new Trisurf3DSeriesRenderer(c).Render(new Trisurf3DSeries(new double[] { NaN, NaN, NaN }, new double[] { NaN, NaN, NaN }, new double[] { NaN, NaN, NaN })); break;
            case "surface": new SurfaceSeriesRenderer(c).Render(new SurfaceSeries(TwoMissing, TwoMissing, FourMissing)); break;
            case "wireframe": new WireframeSeriesRenderer(c).Render(new WireframeSeries(TwoMissing, TwoMissing, FourMissing)); break;
            case "contour3d": new Contour3DSeriesRenderer(c).Render(new Contour3DSeries(TwoMissing, TwoMissing, FourMissing)); break;
            case "bar3d": new Bar3DSeriesRenderer(c).Render(new Bar3DSeries(TwoMissing, TwoMissing, TwoMissing)); break;
            case "planar-bar3d": new PlanarBar3DSeriesRenderer(c).Render(new PlanarBar3DSeries(TwoMissing, TwoMissing, TwoMissing)); break;
            case "quiver3d": new Quiver3DSeriesRenderer(c).Render(new Quiver3DSeries(TwoMissing, TwoMissing, TwoMissing, TwoMissing, TwoMissing, TwoMissing)); break;
            default: new Text3DSeriesRenderer(c).Render(new Text3DSeries([new(NaN, NaN, NaN, "a")])); break;
        }
    }

    /// <summary>A 3-D series none of whose values can be placed draws nothing, on 3-D axes and on 2-D axes (where it
    /// fits its projection to an extent of 0..1 on each axis it has nothing for).</summary>
    [Theory]
    [MemberData(nameof(NothingDrawable3DKinds))]
    public void A3DSeriesWithNothingDrawable_DrawsNothing(string kind)
    {
        foreach (bool axesProjection in new[] { true, false })
        {
            var ctx = Render3D(c => DrawNothingDrawable(kind, c), axesProjection);
            Assert.DoesNotContain(ctx.Calls, c => c.Kind is "DrawPolygon" or "DrawLine" or "DrawLines" or "DrawCircle" or "DrawText");
        }
    }

    /// <summary>A wireframe line breaks at a vertex that cannot be placed, as a 2-D line does: of the two rows and three
    /// columns of a 2 x 3 grid with one missing corner, the row through it keeps its other two points, the column through
    /// it keeps one point and draws nothing.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AWireframe_BreaksItsLinesAtAVertexThatCannotBePlaced(bool axesProjection)
    {
        var ctx = Render3D(c => new WireframeSeriesRenderer(c).Render(new WireframeSeries([0, 1, 2], [0, 1], SurfaceWithAMissingCorner)), axesProjection);

        AssertNothingUndrawableReached(ctx);
        Assert.Equal([2, 3, 2, 2], PolylineLengths(ctx));
    }

    /// <summary>A 3-D stem at a point that cannot be placed is skipped with its head; its foot stays on the baseline when
    /// x and y can be placed, and the baseline breaks where they cannot.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A3DStem_IsSkippedWithItsHead_AndTheBaselineBreaksOnlyWhereItsFootCannotBePlaced(bool axesProjection)
    {
        var missingHeight = Render3D(c => new Stem3DSeriesRenderer(c).Render(new Stem3DSeries(new double[] { 1, 2, 3 }, new double[] { 1, 2, 3 }, new double[] { 1, NaN, 3 })), axesProjection);
        var missingFoot = Render3D(c => new Stem3DSeriesRenderer(c).Render(new Stem3DSeries(new double[] { 1, NaN, 3, 4 }, new double[] { 1, 2, 3, 4 }, new double[] { 1, 2, 3, 4 })), axesProjection);

        AssertNothingUndrawableReached(missingHeight);
        AssertNothingUndrawableReached(missingFoot);
        Assert.Equal(2, missingHeight.CountOf("DrawLine"));
        Assert.Equal(2, missingHeight.CountOf("DrawCircle"));
        Assert.Equal([3], PolylineLengths(missingHeight));
        Assert.Equal(3, missingFoot.CountOf("DrawLine"));
        Assert.Equal([2], PolylineLengths(missingFoot));
    }

    /// <summary>The 3-D axes range over the values that can be placed: one infinity took the whole range with it, so the
    /// projection divided by an infinite width and every vertex of the figure came out NaN.</summary>
    [Fact]
    public void A3DAxes_RangesOverTheValuesThatCanBePlaced()
    {
        string svg = Plt.Create().WithSize(500, 400).AddSubPlot(1, 1, 1, ax => ax
            .Surface([0, 1, 2], [0, 1], new double[,] { { 1, double.PositiveInfinity, 2 }, { 1.5, 3, 4 } })
            .Scatter3D([0, 1, double.NegativeInfinity], [0, 1, 1], [1, 2, 3])
            .Quiver3D([0], [0], [1], [1], [1], [double.PositiveInfinity])).ToSvg();

        Assert.Empty(NonFiniteTokens(svg));
        Assert.Contains("<polygon", svg);
        Assert.Contains("<circle", svg);
    }

    /// <summary>A grid with nothing drawable adds nothing to the 3-D axes' range: the axes span the other grid, exactly as
    /// without it.</summary>
    [Fact]
    public void A3DAxes_LeavesAGridWithNothingDrawableOutOfItsRange()
    {
        static string Ticks(string svg) => string.Join("|", Regex.Matches(svg, ">([-0-9.]+)</text>").Select(m => m.Groups[1].Value));
        string withEmpty = Plt.Create().WithSize(500, 400).AddSubPlot(1, 1, 1, ax => ax
            .Surface([NaN, NaN], [NaN, NaN], new double[,] { { NaN, NaN }, { NaN, NaN } })
            .Surface([0, 1], [0, 1], new double[,] { { 1, 2 }, { 3, 4 } })).ToSvg();
        string alone = Plt.Create().WithSize(500, 400).AddSubPlot(1, 1, 1, ax => ax
            .Surface([0, 1], [0, 1], new double[,] { { 1, 2 }, { 3, 4 } })).ToSvg();

        Assert.Empty(NonFiniteTokens(withEmpty));
        Assert.NotEmpty(Ticks(alone));
        Assert.Equal(Ticks(alone), Ticks(withEmpty));
    }

    // ---- a whole made of parts (pie, donut, funnel), a radar's vertices, the polar grid, a series with no data ---------

    private static List<string> Texts(RecordingRenderContext ctx) =>
        [.. ctx.OfKind("DrawText").Select(c => (string)c.Args!.GetType().GetProperty("text")!.GetValue(c.Args)!)];

    private static Point[] PolygonPoints(CallRecord call) =>
        (Point[])call.Args!.GetType().GetProperty("points")!.GetValue(call.Args)!;

    /// <summary>A pie's total is the sum of the slices that are there. A missing slice (NaN, or an infinity, which no
    /// angle can hold) counts as 0 and draws nothing of its own: no wedge, no shadow, no percentage, no label. Before, the
    /// total was NaN and every wedge was drawn at NaN. matplotlib has no rule here (its total turns NaN as well); a slice
    /// that is not there takes no share of the circle, as a zero takes none. The other slices keep their own colour,
    /// explode and label, and their percentages are shares of the finite total.</summary>
    [Theory]
    [InlineData(NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void AMissingPieSlice_CountsAsZeroInTheTotal_AndDrawsNoWedge(double missing)
    {
        var withGap = Render(c => new PieSeriesRenderer(c).Render(new PieSeries([30, missing, 70])
        {
            Labels = ["a", "b", "c"], Colors = [Colors.Red, Colors.Green, Colors.Blue], Explode = [0.1, 0.2, 0],
            Shadow = true, AutoPct = "{0:F1}%",
        }));
        var without = Render(c => new PieSeriesRenderer(c).Render(new PieSeries([30, 70])
        {
            Labels = ["a", "c"], Colors = [Colors.Red, Colors.Blue], Explode = [0.1, 0], Shadow = true, AutoPct = "{0:F1}%",
        }));

        AssertNothingUndrawableReached(withGap);
        Assert.Equal(Signature(without), Signature(withGap));
        Assert.Equal(["30.0%", "70.0%"], Texts(withGap).Where(t => t.EndsWith('%')));
    }

    /// <summary>A donut follows the pie's rule: a missing slice counts as 0 in the total and draws no wedge, and the slices
    /// around it keep their own colour (the colour of a slice is the one at its own position, given or cycled).</summary>
    [Theory]
    [InlineData(NaN)]
    [InlineData(double.PositiveInfinity)]
    public void AMissingDonutSlice_CountsAsZeroInTheTotal_AndDrawsNoWedge(double missing)
    {
        var withGap = Render(c => new DonutSeriesRenderer(c).Render(new DonutSeries([30, missing, 70]) { Colors = [Colors.Red, Colors.Green, Colors.Blue], CenterText = "total" }));
        var without = Render(c => new DonutSeriesRenderer(c).Render(new DonutSeries([30, 70]) { Colors = [Colors.Red, Colors.Blue], CenterText = "total" }));
        var cycled = Render(c => new DonutSeriesRenderer(c).Render(new DonutSeries([30, missing, 70])));

        AssertNothingUndrawableReached(withGap);
        Assert.Equal(Signature(without), Signature(withGap));
        Assert.Equal([Theme.Default.CycleColors[0], Theme.Default.CycleColors[2]],
            cycled.OfKind("DrawPath").Select(c => (Color)c.Args!.GetType().GetProperty("fill")!.GetValue(c.Args)!));
    }

    /// <summary>A missing stage of a funnel is skipped with its label; the stages around it keep their rows and their
    /// widths, scaled to the widest stage that is there.</summary>
    [Theory]
    [InlineData(NaN)]
    [InlineData(double.PositiveInfinity)]
    public void AMissingFunnelStage_IsSkipped_ItsNeighboursStayWhereTheyAre(double missing)
    {
        var withGap = Render(c => new FunnelSeriesRenderer(c).Render(new FunnelSeries(["a", "b", "c"], [10, missing, 5])));
        var full = Render(c => new FunnelSeriesRenderer(c).Render(new FunnelSeries(["a", "b", "c"], [10, 7, 5])));

        AssertNothingUndrawableReached(withGap);
        var expected = Signature(full);
        expected.RemoveRange(2, 2);   // stage b: its bar and its label
        Assert.Equal(expected, Signature(withGap));
    }

    /// <summary>An empty funnel draws an empty plot, as a series of every other type does with no data: it scaled its
    /// stages by <c>Values.Max()</c> over nothing and threw InvalidOperationException.</summary>
    [Fact]
    public void AnEmptyFunnel_DrawsNothing()
    {
        Assert.Empty(Render(c => new FunnelSeriesRenderer(c).Render(new FunnelSeries([], []))).Calls);
        Assert.Contains("</svg>", Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Funnel([], [])).ToSvg());
    }

    /// <summary>Every registered type, read from JSON with no data at all, renders an empty plot. Measured before: the
    /// funnel was the one of 78 that threw. The pair grid is not here: its constructor refuses a grid with no variable
    /// (an invariant of the model, refused when the JSON is read).</summary>
    public static TheoryData<string> EveryTypeButThePairGrid { get; } = new(SeriesRegistry.Discriminators.Where(t => t != "pairgrid"));

    [Theory]
    [MemberData(nameof(EveryTypeButThePairGrid))]
    public void ASeriesWithNoData_RendersAnEmptyPlot(string type)
    {
        var figure = ChartSerializerDefault.Current.FromJson($$"""{"width":300,"height":200,"subPlots":[{"series":[{"type":"{{type}}"}]}]}""");

        Assert.Empty(NonFiniteTokens(figure.ToSvg()));
    }

    /// <summary>A radar value that is missing, or infinite (no ring holds it), is a skipped vertex: the polygon closes
    /// through the spokes on either side of it, and the web, the spokes and every category label stay. The scale spans the
    /// values that are there. matplotlib's radar example draws with <c>plot</c> and <c>fill</c>: its outline breaks at the
    /// spoke, and its fill, a closed path that NaN breaks, loses the region between the neighbours and the first vertex. One
    /// closed polygon here keeps the fill and its outline the same shape.</summary>
    [Theory]
    [InlineData(NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void AMissingRadarValue_IsASkippedVertex_ThePolygonClosesThroughItsNeighbours(double missing)
    {
        string[] spokes = ["a", "b", "c", "d", "e"];
        var withGap = Render(c => new RadarSeriesRenderer(c).Render(new RadarSeries(spokes, [3, missing, 2, 4, 5])));
        var full = Render(c => new RadarSeriesRenderer(c).Render(new RadarSeries(spokes, [3, 1, 2, 4, 5])));

        AssertNothingUndrawableReached(withGap);
        var web = Signature(full);
        web.RemoveAt(web.Count - 1);
        var gapWeb = Signature(withGap);
        gapWeb.RemoveAt(gapWeb.Count - 1);
        Assert.Equal(web, gapWeb);
        var vertices = PolygonPoints(full.Calls[^1]);
        Assert.Equal([vertices[0], vertices[2], vertices[3], vertices[4]], PolygonPoints(withGap.Calls[^1]));
    }

    /// <summary>The polar grid spans the radii that can be placed: one infinite radius made every ring infinite and every
    /// radial line NaN. It is now left out as a missing radius is.</summary>
    [Fact]
    public void ThePolarGrid_SpansTheRadiiThatCanBePlaced()
    {
        string Svg(double r) => Plt.Create().WithSize(400, 400).AddSubPlot(1, 1, 1, ax => ax.PolarScatter([1.0, 2, r], [0.0, 1, 2])).ToSvg();

        Assert.Empty(NonFiniteTokens(Svg(double.PositiveInfinity)));
        Assert.Equal(Svg(NaN), Svg(double.PositiveInfinity));
    }

    // ---- the whole figure ---------------------------------------------------------------------------------------------

    /// <summary>Measured before: pie 30 NaN tokens, polar bar 26, donut 24, polar scatter, radar, funnel and bubble 2 each.</summary>
    [Fact]
    public void AFigureOfCircularPolarAndBubbleChartsWithAMissingValue_WritesNoNaNIntoItsSvg()
    {
        string svg = Plt.Create().WithSize(1000, 500)
            .AddSubPlot(2, 4, 1, ax => ax.Pie([30.0, NaN, 70], ["a", "b", "c"], s => s.AutoPct = "{0:F0}%"))
            .AddSubPlot(2, 4, 2, ax => ax.Donut([30.0, NaN, 70], null, s => s.CenterText = "c"))
            .AddSubPlot(2, 4, 3, ax => ax.Funnel(["a", "b", "c"], [10.0, NaN, 5]))
            .AddSubPlot(2, 4, 4, ax => ax.Radar(["a", "b", "c", "d"], [1.0, NaN, 3, 2]))
            .AddSubPlot(2, 4, 5, ax => ax.PolarPlot([1.0, NaN, 2, 3], [0.0, 1, 2, 3]))
            .AddSubPlot(2, 4, 6, ax => ax.PolarScatter([1.0, NaN, 2], [0.0, 1, 2]))
            .AddSubPlot(2, 4, 7, ax => ax.PolarBar([5.0, NaN, 10], [0.0, 1, 2]))
            .AddSubPlot(2, 4, 8, ax => ax.Bubble([1.0, NaN, 3], [1.0, 2, 3], [10.0, 20, NaN]))
            .ToSvg();

        Assert.Empty(NonFiniteTokens(svg));
    }

    [Fact]
    public void AFigureWithAMissingValueInEveryKindOfMark_WritesNoNaNIntoItsSvg()
    {
        string svg = Plt.Create().WithSize(600, 400).AddSubPlot(1, 1, 1, ax => ax
            .Scatter([1.0, 2, 3], [1, NaN, 3], s => s.Marker = MarkerStyle.Diamond)
            .Plot([1.0, 2, 3], [2, NaN, 4], s => { s.Marker = MarkerStyle.Square; s.Label = "line"; })
            .Stem([1.0, 2, 3], [3, NaN, 1])
            .ErrorBar([1.0, 2, 3], [2, 3, NaN], [0.2, NaN, 0.2], [0.2, 0.2, 0.2])
            .AxHLine(NaN, r => r.Label = "h").AxVSpan(NaN, 2, s => s.Label = "v")
            .AddTrendline(0, 1, 3, NaN).AddLevel(NaN).AddFibonacci(NaN, 1).AddSignal(2, NaN, SignalDirection.Sell)
            .Annotate("peak", 2, NaN)
            .WithLegend().WithLegendValues()).ToSvg();

        Assert.Empty(NonFiniteTokens(svg));
    }
}
