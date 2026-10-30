// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>
/// The marks a legend keeps clear of are the marks that are drawn: for each series that draws a mark, every rectangle,
/// path vertex and point its renderer hands to the collection is one the same renderer draws when it draws for real,
/// and collecting draws nothing. A mark invented by the collector would move a legend away from nothing.
/// </summary>
public class MarksAsDrawnTests
{
    private static readonly Rect Plot = new(50, 40, 400, 300);
    private static DataTransform Transform() => new(0, 10, 0, 10, Plot);

    private static readonly double[] X = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
    private static readonly double[] Y = [3, 5, 4, 7, 6, 8, 5, 9, 7, 6, 8];

    private static readonly Dictionary<string, Func<ISeries>> Series = new()
    {
        ["line"] = () => new LineSeries(X, Y),
        ["stepped line"] = () => new LineSeries(X, Y) { DrawStyle = DrawStyle.StepsPost },
        ["line with a missing value"] = () => new LineSeries(X, [3, 5, double.NaN, 7, 6, 8, 5, 9, 7, 6, 8]),
        ["downsampled line"] = () => new LineSeries(X, Y) { MaxDisplayPoints = 5 },
        ["scatter"] = () => new ScatterSeries(X, Y),
        ["scatter with sizes"] = () => new ScatterSeries(X, Y) { Sizes = [10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110] },
        ["bar"] = () => new BarSeries(["a", "b", "c"], [3.0, 5.0, -2.0]),
        ["horizontal bar"] = () => new BarSeries(["a", "b", "c"], [3.0, 5.0, -2.0]) { Orientation = BarOrientation.Horizontal },
        ["bar with labels"] = () => new BarSeries(["a", "b", "c"], [3.0, 5.0, -2.0]) { ShowLabels = true },
        ["horizontal bar with labels"] = () => new BarSeries(["a", "b", "c"], [3.0, 5.0, -2.0]) { Orientation = BarOrientation.Horizontal, ShowLabels = true },
        ["step"] = () => new StepSeries(X, Y),
        ["ecdf"] = () => new EcdfSeries([3.0, 1.0, 2.0, 5.0, 4.0]),
        ["sparkline"] = () => new SparklineSeries([1.0, 4.0, 2.0, 5.0, 3.0]),
        ["signal"] = () => new SignalSeries([1.0, 4.0, 2.0, 5.0, 3.0], sampleRate: 1.0),
        ["signal xy"] = () => new SignalXYSeries(X, Y),
        ["bubble"] = () => new BubbleSeries(X, Y, [10.0, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110]),
        ["regression"] = () => new RegressionSeries(X, Y),
        ["regression with a band"] = () => new RegressionSeries(X, Y) { ShowConfidence = true },
        ["residual"] = () => new ResidualSeries(X, Y),
        ["error bar"] = () => new ErrorBarSeries(X, Y, [0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5], [0.4, 0.4, 0.4, 0.4, 0.4, 0.4, 0.4, 0.4, 0.4, 0.4, 0.4]),
        ["stacked area"] = () => new StackedAreaSeries(X, [Y, X]),
        ["stem"] = () => new StemSeries(X, Y),
        ["kde"] = () => new KdeSeries([1.0, 2.0, 2.5, 3.0, 3.1, 4.0, 5.0]) { Fill = true },
        ["area"] = () => new AreaSeries(X, Y),
        ["area with a lower edge"] = () => new AreaSeries(X, Y) { YData2 = [1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1] },
        ["area with a where"] = () => new AreaSeries(X, Y) { Where = (x, _) => x is > 2 and < 7 },
        ["histogram"] = () => new HistogramSeries([1.0, 2.0, 2.5, 3.0, 3.1, 4.0, 5.0, 5.5, 6.0]),
        ["stepped histogram"] = () => new HistogramSeries([1.0, 2.0, 2.5, 3.0, 3.1, 4.0, 5.0, 5.5, 6.0]) { HistType = HistType.Step },
        ["filled stepped histogram"] = () => new HistogramSeries([1.0, 2.0, 2.5, 3.0, 3.1, 4.0, 5.0, 5.5, 6.0]) { HistType = HistType.StepFilled },
        ["count"] = () => new CountSeries(["a", "b", "a", "c", "a", "b"]),
        ["waterfall"] = () => new WaterfallSeries(["a", "b", "c"], [10.0, -4.0, 6.0]),
        ["waterfall with targets"] = () => new WaterfallSeries(["a", "b", "c"], [10.0, -4.0, 6.0]) { Targets = [12.0, double.NaN, 5.0] },
        ["waterfall with a missing step"] = () => new WaterfallSeries(["a", "b", "c"], [10.0, double.NaN, 6.0]) { Targets = [9.0, 9.0, 9.0] },
        ["broken bar"] = () => new BrokenBarSeries([[new(1.0, 2.0), new(4.0, 1.0)], [new(2.0, 3.0)]]),
        ["gantt"] = () => new GanttSeries(["a", "b"], [0.0, 2.0], [3.0, 6.0]),
        ["funnel"] = () => new FunnelSeries(["Visited", "Cart", "Bought"], [100.0, 60.0, 20.0]),
        ["candlestick"] = () => new CandlestickSeries([10.0, 12, 11], [15.0, 16, 14], [8.0, 9, 8], [13.0, 11, 12]),
        ["ohlc bar"] = () => new OhlcBarSeries([10.0, 12, 11], [15.0, 16, 14], [8.0, 9, 8], [13.0, 11, 12]),
        ["point and figure"] = () => new PointAndFigureSeries([20.4, 22.3, 24.4, 21.0, 18.6, 21.9, 22.0, 25.8], 1),
        ["box"] = () => new BoxSeries([[1.0, 2, 3, 4, 5, 9], [2.0, 3, 3, 4, 6]]) { ShowMeans = true },
        ["horizontal box"] = () => new BoxSeries([[1.0, 2, 3, 4, 5, 9], [2.0, 3, 3, 4, 6]]) { Vert = false, ShowMeans = true },
        ["violin"] = () => new ViolinSeries([[1.0, 2, 3, 4, 5, 9], [2.0, 3, 3, 4, 6]]) { ShowExtrema = true, ShowMedians = true, ShowMeans = true },
        ["stripplot"] = () => new StripplotSeries([[1.0, 2, 3], [4.0, 5, 6]]),
        ["swarmplot"] = () => new SwarmplotSeries([[1.0, 2, 3], [4.0, 5, 6]]),
        ["rugplot"] = () => new RugplotSeries(new double[] { 1.0, 2.0, 3.0, 7.0 }),
        ["eventplot"] = () => new EventplotSeries([[1.0, 2.0], [3.0, 4.0, 8.0]]),
        ["pointplot"] = () => new PointplotSeries([[1.0, 2.0, 3.0], [4.0, 5.0, 6.0]]),
        ["pie"] = () => new PieSeries([30.0, 50.0, 20.0]) { AutoPct = "{0:F0}%", Labels = ["a", "b", "c"] },
        ["waffle"] = () => new WaffleSeries(["a", "b", "c"], [30.0, 50.0, 20.0]),
        ["donut"] = () => new DonutSeries([30.0, 50.0, 20.0]) { CenterText = "total" },
        ["radar"] = () => new RadarSeries(["A", "B", "C", "D"], [1.0, 2.0, 3.0, 2.0]),
        ["network graph"] = () => new NetworkGraphSeries(
            [new GraphNode("a", 2, 2), new GraphNode("b", 6, 3), new GraphNode("c", 4, 8)],
            [new GraphEdge("a", "b"), new GraphEdge("b", "c")]) { Layout = GraphLayout.Manual },
        ["relative rotation"] = () => new RelativeRotationSeries(
            [Enumerable.Range(0, 60).Select(i => 100.0 + i * 0.5 + (i % 7)).ToArray()],
            Enumerable.Range(0, 60).Select(i => 100.0 + i * 0.3).ToArray(), ["asset"]),
        ["dendrogram"] = () => new DendrogramSeries(new TreeNode { Label = "Root", Value = 1.0,
            Children = [new TreeNode { Label = "A", Value = 0.2 }, new TreeNode { Label = "B", Value = 0.4 }] }),
        ["polar line"] = () => new PolarLineSeries([1.0, 2.0, 3.0, 2.0], [0.0, 1.0, 2.0, 3.0]),
        ["polar scatter"] = () => new PolarScatterSeries([1.0, 2.0, 3.0], [0.5, 1.5, 2.5]),
        ["polar bar"] = () => new PolarBarSeries([5.0, 10.0, 7.0], [0.0, 1.57, 3.14]),
        ["line 3d"] = () => new Line3DSeries(new double[] { 1.0, 2.0, 3.0 }, new double[] { 1.0, 3.0, 2.0 }, new double[] { 3.0, 4.0, 2.0 }),
        ["stem 3d"] = () => new Stem3DSeries(new double[] { 1.0, 2.0 }, new double[] { 1.0, 2.0 }, new double[] { 3.0, 4.0 }),
        ["scatter 3d"] = () => new Scatter3DSeries(new double[] { 1.0, 2.0, 3.0 }, new double[] { 1.0, 3.0, 2.0 }, new double[] { 3.0, 4.0, 2.0 }),
        ["bar 3d"] = () => new Bar3DSeries(new double[] { 1.0, 2.0 }, new double[] { 1.0, 2.0 }, new double[] { 3.0, 4.0 }),
        ["planar bar 3d"] = () => new PlanarBar3DSeries(new double[] { 1.0, 2.0 }, new double[] { 1.0, 2.0 }, new double[] { 3.0, 4.0 }),
        ["text 3d"] = () => new Text3DSeries([new Text3DAnnotation(1, 2, 3, "A"), new Text3DAnnotation(4, 5, 6, "B")]),
        ["streaming line"] = () => Streamed(new StreamingLineSeries(capacity: 16), s => { foreach (var i in Enumerable.Range(0, 8)) s.AppendPoint(i, Y[i]); }),
        ["streaming scatter"] = () => Streamed(new StreamingScatterSeries(capacity: 16), s => { foreach (var i in Enumerable.Range(0, 8)) s.AppendPoint(i, Y[i]); }),
        ["streaming signal"] = () => Streamed(new StreamingSignalSeries(capacity: 16), s => { foreach (var i in Enumerable.Range(0, 8)) s.AppendSample(Y[i]); }),
        ["streaming candlestick"] = () => Streamed(new StreamingCandlestickSeries(capacity: 16), s => { s.AppendBar(10, 15, 8, 13); s.AppendBar(13, 18, 11, 16); }),
        ["sunburst"] = () => new SunburstSeries(new TreeNode { Label = "Root",
            Children = [new TreeNode { Label = "A", Value = 10 }, new TreeNode { Label = "B", Value = 20 }] }),
    };

    public static TheoryData<string> Names => [.. Series.Keys];

    /// <summary>The series types the rows cover.</summary>
    internal static IEnumerable<Type> SeriesTypes => Series.Values.Select(make => make().GetType()).Distinct();

    private static T Streamed<T>(T series, Action<T> fill) where T : ISeries
    {
        fill(series);
        return series;
    }

    private static SvgSeriesRenderer Visitor(IRenderContext ctx, MarkObstacles.Builder? marks = null) =>
        new(Transform(), ctx, Colors.Blue, Theme.Default, plotArea: Plot) { Marks = marks };

    private static IEnumerable<Point> DrawnPoints(RecordingRenderContext ctx)
    {
        foreach (var call in ctx.Calls)
        {
            dynamic args = call.Args!;
            switch (call.Kind)
            {
                case "DrawLines" or "DrawPolygon":
                    foreach (var p in (Point[])args.points) yield return p;
                    break;
                case "DrawLine":
                    yield return (Point)args.p1;
                    yield return (Point)args.p2;
                    break;
                case "DrawCircle":
                    yield return (Point)args.center;
                    break;
                case "DrawPath":
                    foreach (var segment in (PathSegment[])args.segments)
                    {
                        if (segment is MoveToSegment move) yield return move.Point;
                        else if (segment is LineToSegment line) yield return line.Point;
                        else if (segment is BezierSegment bezier) yield return bezier.End;
                    }
                    break;
            }
        }
    }

    private static bool Same(Point a, Point b) => Math.Abs(a.X - b.X) < 1e-9 && Math.Abs(a.Y - b.Y) < 1e-9;

    [Theory]
    [MemberData(nameof(Names))]
    public void EveryMarkCollected_IsOneTheRendererDraws(string name)
    {
        var drawn = new RecordingRenderContext();
        var series = Series[name]();
        series.Accept(Visitor(drawn), new RenderArea(Plot, drawn));

        var collector = new RecordingRenderContext();
        var builder = new MarkObstacles.Builder();
        series.Accept(Visitor(collector, builder), new RenderArea(Plot, collector));
        var marks = builder.Build();

        Assert.Empty(collector.Calls);   // collecting draws nothing
        Assert.True(marks.Boxes.Count + marks.Paths.Count + marks.Points.Count > 0, $"{name}: collected no mark at all");

        var rects = drawn.OfKind("DrawRectangle").Select(c => (Rect)((dynamic)c.Args!).rect).ToList();
        // An ellipse is a box as well: the rectangle it is inscribed in.
        rects.AddRange(drawn.OfKind("DrawEllipse").Select(c => (Rect)((dynamic)c.Args!).bounds));
        // A text the series draws is a box as well: where it stands, measured as the legend's pass measures it.
        foreach (var text in drawn.OfKind("DrawText"))
        {
            dynamic args = text.Args!;
            var font = (Font)args.font;
            rects.Add(new TextBox((Point)args.position, drawn.MeasureText((string)args.text, font), font.Size,
                (TextAlignment)args.alignment, TextVerticalAlignment.Baseline, (double)args.rotation).Bounds());
        }
        foreach (var box in marks.Boxes)
            Assert.Contains(rects, r => Same(new Point(r.X, r.Y), new Point(box.X, box.Y)) && Math.Abs(r.Width - box.Width) < 1e-9 && Math.Abs(r.Height - box.Height) < 1e-9);

        var points = DrawnPoints(drawn).ToList();
        // An arc is drawn by its centre and radius; the points a collected path samples along it lie on that circle.
        var arcs = drawn.OfKind("DrawPath").SelectMany(c => (PathSegment[])((dynamic)c.Args!).segments).OfType<ArcSegment>().ToList();
        bool OnAnArc(Point p) => arcs.Any(a => Math.Abs(Math.Sqrt((p.X - a.Center.X) * (p.X - a.Center.X) + (p.Y - a.Center.Y) * (p.Y - a.Center.Y)) - a.RadiusX) < 1e-6);
        foreach (var path in marks.Paths)
        foreach (var vertex in path)
            Assert.True(points.Any(p => Same(p, vertex)) || OnAnArc(vertex), $"{name}: the vertex {vertex} is not one the renderer draws");
        foreach (var centre in marks.Points)
            Assert.Contains(points, p => Same(p, centre));
    }

    [Fact]
    public void ALineWithAMissingValue_IsTwoPaths_ThatMeetNoPointTheAxesCannotPlace()
    {
        var builder = new MarkObstacles.Builder();
        Series["line with a missing value"]().Accept(Visitor(new RecordingRenderContext(), builder), new RenderArea(Plot, new RecordingRenderContext()));

        var marks = builder.Build();

        Assert.Equal(2, marks.Paths.Count);
        Assert.All(marks.Paths, path => Assert.All(path, p => Assert.True(p.IsDrawable())));
    }

    [Fact]
    public void ADownsampledLine_CollectsTheSamplesItKeeps_NotTheWholeSeries()
    {
        var builder = new MarkObstacles.Builder();
        Series["downsampled line"]().Accept(Visitor(new RecordingRenderContext(), builder), new RenderArea(Plot, new RecordingRenderContext()));

        Assert.Equal(5, Assert.Single(builder.Build().Paths).Count);
    }

    // ---- the marks of a series with less than the rows give it --------------------------------------------------------

    private static MarkObstacles Collected(ISeries series)
    {
        var builder = new MarkObstacles.Builder();
        series.Accept(Visitor(new RecordingRenderContext(), builder), new RenderArea(Plot, new RecordingRenderContext()));
        return builder.Build();
    }

    private static int MarkCount(MarkObstacles marks) => marks.Boxes.Count + marks.Paths.Count + marks.Points.Count;

    private static double[] Fill(double value) => [.. X.Select(_ => value)];

    [Fact]
    public void AWaterfallsTargets_AreCollectedAsOnePathEach_ThoseTheAxesCannotPlaceAreNot()
    {
        var marks = Collected(new WaterfallSeries(["a", "b", "c"], [10.0, -4.0, 6.0]) { Targets = [12.0, double.NaN, 5.0] });

        Assert.Equal(3, marks.Boxes.Count);
        Assert.Equal(2, marks.Paths.Count);
        Assert.All(marks.Paths, path => Assert.Equal(2, path.Count));
    }

    [Fact]
    public void AWaterfallStepTheAxesCannotPlace_CollectsNoBoxForItsBar()
    {
        var marks = Collected(new WaterfallSeries(["a", "b", "c"], [10.0, double.NaN, 6.0]));

        Assert.Single(marks.Boxes);
    }

    /// <summary>A grid of cells needs room to stand in: a plot area with none draws no cell and collects no mark, rather than
    /// cells of no size or a negative one.</summary>
    [Fact]
    public void AWaffleInAPlotWithNoRoom_DrawsNothing_AndCollectsNothing()
    {
        var empty = new Rect(10, 10, 0, 0);
        var drawn = new RecordingRenderContext();
        var builder = new MarkObstacles.Builder();
        var transform = new DataTransform(0, 10, 0, 10, empty);

        Series["waffle"]().Accept(new SvgSeriesRenderer(transform, drawn, Colors.Blue, Theme.Default, plotArea: empty), new RenderArea(empty, drawn));
        Series["waffle"]().Accept(new SvgSeriesRenderer(transform, new RecordingRenderContext(), Colors.Blue, Theme.Default, plotArea: empty) { Marks = builder },
            new RenderArea(empty, new RecordingRenderContext()));

        Assert.Empty(drawn.OfKind("DrawRectangle"));
        Assert.Equal(0, MarkCount(builder.Build()));
    }

    [Fact]
    public void AKdeOfNoSample_CollectsNoMark() =>
        Assert.Equal(0, MarkCount(Collected(new KdeSeries([]))));

    [Fact]
    public void ARegressionWithTooFewPairsToFit_CollectsNoMark() =>
        Assert.Equal(0, MarkCount(Collected(new RegressionSeries([1.0], [2.0]))));

    [Fact]
    public void AResidualPlotOfNoPairs_CollectsNoMark() =>
        Assert.Equal(0, MarkCount(Collected(new ResidualSeries(Array.Empty<double>(), Array.Empty<double>()))));

    [Fact]
    public void ErrorBars_EveryThirdPoint_AreMarkedAtEveryThirdPointOnly_ThePointsAreAllMarked()
    {
        var marks = Collected(new ErrorBarSeries(X, Y, Fill(0.5), Fill(0.4)) { ErrorEvery = 3 });

        Assert.Equal(X.Length, marks.Points.Count);
        Assert.Equal(4, marks.Paths.Count);   // the bars at the points 0, 3, 6 and 9 of 11
    }

    [Fact]
    public void ErrorBars_WithErrorsAlongX_HaveAHorizontalBarBesideEachVerticalOne()
    {
        var marks = Collected(new ErrorBarSeries(X, Y, Fill(0.5), Fill(0.4)) { XErrorLow = Fill(0.3), XErrorHigh = Fill(0.2) });

        Assert.Equal(2 * X.Length, marks.Paths.Count);
    }

    [Fact]
    public void ErrorBars_WithAnXErrorThatCannotBePlaced_LeaveThatHorizontalBarOut()
    {
        double[] low = Fill(0.3);
        low[3] = double.NaN;

        var marks = Collected(new ErrorBarSeries(X, Y, Fill(0.5), Fill(0.4)) { XErrorLow = low, XErrorHigh = Fill(0.2) });

        Assert.Equal(2 * X.Length - 1, marks.Paths.Count);
    }
}
