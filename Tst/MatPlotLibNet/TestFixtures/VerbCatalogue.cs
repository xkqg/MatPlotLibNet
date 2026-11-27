// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Reflection;
using MatPlotLibNet.Builders;
using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Models.Tools;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Packaging;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests;

/// <summary>What a verb does with a missing callback.</summary>
public enum NoCallback
{
    /// <summary>The callback is optional: no callback draws what an empty one draws.</summary>
    Accepted,

    /// <summary>The callback is the verb's whole point (it fills an axis or a subplot): a missing one is refused by its name.</summary>
    Refused,

    /// <summary>The callback is the verb's whole point, but a missing one is not refused by its name: the call fails later, on a
    /// null reference. A known defect, register row R72; the row flips to <see cref="Refused"/> the day the verb refuses it.</summary>
    Unguarded,
}

/// <summary>One call a user writes to a builder verb that takes a callback, as the compiler bound it: the call the lambda hands the
/// callback to is the verb, so the overload a row stands for is exact, and a verb that moves from the class to an extension block
/// keeps its row.</summary>
/// <param name="Call">The call: a lambda of the builder and the callback that calls the verb with the callback (after whatever the
/// verb needs on the axes first, a price series for an indicator, an inset for a zoom).</param>
/// <param name="Series">How many series the call leaves on the figure; <see langword="null"/> where the count belongs to a composed
/// chart's own shape (<see cref="ComposedChartShapes"/>).</param>
/// <param name="Runs">How many times the callback runs: once, or once for each unit a composed chart draws.</param>
/// <param name="Holder">Where the things the callback is handed stand on the built figure, when they are not series (series are
/// found on the figure's axes); <see langword="null"/> when what the callback is handed is a builder, not a part of the figure.</param>
/// <param name="NoCallback">What the verb does without a callback.</param>
public readonly record struct VerbRow(LambdaExpression Call, int? Series = 1, int Runs = 1, Func<Figure, IEnumerable<object>>? Holder = null,
    NoCallback NoCallback = NoCallback.Accepted)
{
    /// <summary>The call the lambda hands the callback to.</summary>
    public MethodCallExpression Body => VerbCatalogue.CallTaking(Call.Body, Call.Parameters[1])
        ?? throw new ArgumentException("A verb row hands its callback to the verb.", nameof(Call));

    /// <summary>The overload the row calls; a generic verb as its definition.</summary>
    public MethodInfo Method => Body.Method.IsGenericMethod ? Body.Method.GetGenericMethodDefinition() : Body.Method;

    /// <summary>The builder the verb is called on.</summary>
    public Type Receiver => Call.Parameters[0].Type;

    /// <summary>What the callback is handed.</summary>
    public Type Configured => Call.Parameters[1].Type.GetGenericArguments()[0];

    /// <summary>The builder the verb belongs to: the one it is called on, which may be a builder the call reached first (the slot
    /// of an added axis, an inset).</summary>
    public Type Owner => Body.Object?.Type ?? Body.Arguments[0].Type;

    /// <summary>The overload as a Theory carries it: the builder, the verb and the types it takes.</summary>
    public string Name => VerbCatalogue.NameOf(Owner, Method);

    /// <summary>Calls the verb on <paramref name="builder"/>, handing it <paramref name="configure"/>; an exception the verb throws
    /// is thrown as it is.</summary>
    public object Invoke(object builder, Delegate? configure)
    {
        var target = Expression.Parameter(typeof(object));
        var callback = Expression.Parameter(typeof(Delegate));
        var call = Expression.Invoke(Call, Expression.Convert(target, Receiver), Expression.Convert(callback, Call.Parameters[1].Type));
        return Expression.Lambda<Func<object, Delegate?, object>>(Expression.Convert(call, typeof(object)), target, callback)
            .Compile()(builder, configure);
    }
}

/// <summary>Every builder verb that hands the caller a callback, one row per overload: the axes builder's verbs, the composed charts
/// (<see cref="ComposedChartShapes"/>, projected by <c>AsVerbRow</c>), the figure builder's own verbs, and each figure verb that
/// draws on the figure's one subplot, derived from the axes verb of the same name and parameters. The data is small and exact, so a
/// row draws in a few milliseconds.</summary>
public static class VerbCatalogue
{
    /// <summary>The calls a caller writes on the figure builder, the methods of the class and the extension members alike.</summary>
    private static readonly IReadOnlyList<BuilderCall> FigureCalls = typeof(FigureBuilder).Calls();

    private static readonly double[] X = [1, 2, 3, 4, 5, 6];
    private static readonly double[] Y = [2, 4, 3, 5, 4, 6];
    private static readonly double[] Z = [1, 3, 2, 4, 3, 5];
    private static readonly double[] Ones = [1, 1, 1, 1, 1, 1];
    private static readonly double[] Fractions = [0.2, 0.5, 0.3];
    private static readonly string[] Labels = ["a", "b", "c", "d", "e", "f"];
    private static readonly string[] Pair = ["A", "B"];
    private static readonly string[] Three = ["A", "B", "C"];
    private static readonly double[] Shares = [37, 33, 30];
    private static readonly double[] Weights = [10, 4, 1];
    private static readonly double[] Edges3 = [0, 1, 2];
    private static readonly double[] TriangleX = [0, 1, 0.5];
    private static readonly double[] TriangleY = [0, 0, 1];
    private static readonly double[] TriangleZ = [1, 2, 3];
    private static readonly double[] Slope = [0, 1, 4];
    private static readonly DateTime[] Dates = [.. Enumerable.Range(0, 6).Select(day => new DateTime(2026, 1, 1).AddDays(day))];
    private static readonly double[] Axis = [0, 1];
    private static readonly double[,] Grid = { { 1, 2 }, { 3, 4 } };
    private static readonly double[,] Field = { { 0, 1 }, { 1, 0 } };
    private static readonly double[][] Samples = [[1, 2, 3, 4], [2, 3, 4, 5]];
    private static readonly double[][] Layers = [[1, 2, 3, 4, 5, 6], [2, 3, 4, 5, 6, 7]];
    private static readonly string[][] Cells = [["a", "b"], ["c", "d"]];
    private static readonly BarRange[][] Ranges = [[new BarRange(1, 2), new BarRange(4, 1)]];
    private static readonly bool[,,] Filled = { { { true, false }, { false, false } }, { { false, false }, { false, true } } };
    private static readonly TreeNode Tree = new() { Label = "Root", Children = [new() { Label = "A", Value = 1 }, new() { Label = "B", Value = 2 }] };
    private static readonly TreeNode Clustered = new() { Label = "Root", Value = 1, Children = [new() { Label = "A", Value = 0 }, new() { Label = "B", Value = 0 }] };
    private static readonly GraphNode[] Nodes = [new GraphNode("a", X: 1, Y: 2), new GraphNode("b", X: 3, Y: 4)];
    private static readonly GraphEdge[] Edges = [new GraphEdge("a", "b")];
    private static readonly SankeyNode[] Stages = [new SankeyNode("A"), new SankeyNode("B")];
    private static readonly SankeyLink[] Flows = [new SankeyLink(0, 1, 10)];
    private static readonly StateSegment[] Segments = [new StateSegment(0, 1, "A", Colors.Red), new StateSegment(1, 2, "B", Colors.Blue)];
    private static readonly WaterSample[] Water = [new WaterSample { Ca = 60, Mg = 10, Na = 8, K = 2, HCO3 = 200, SO4 = 20, Cl = 10 }];
    private static readonly TreeGridRow[] Rows3 = [new TreeGridRow("A", ["1", "2"]), new TreeGridRow("B", ["3", "4"])];
    private static readonly BarGroup[] Groups = [new BarGroup("one", [1, 2, 3, 4, 5, 6]), new BarGroup("two", [2, 3, 4, 5, 6, 7])];
    private static readonly double[][] Closes = [[100, 101, 102], [100, 99, 98]];
    private static readonly double[] Benchmark = [100, 100, 100];
    private static readonly string[] Assets = ["ETH", "BNB"];
    private static readonly InsetBounds Inset = new(0.6, 0.6, 0.3, 0.3);
    private static readonly GridPosition Whole = new(0, 1, 0, 1);

    /// <summary>120 closes that rise with a deterministic wobble (integer arithmetic, so every platform has the same bits).</summary>
    private static readonly double[] Prices = [.. Enumerable.Range(0, 120).Select(i => 100 + (i * 0.1) + (i * 37 % 23 * 0.25))];
    private static readonly double[] Bars = [.. Enumerable.Range(0, 120).Select(i => (double)i)];
    private static readonly double[] Wobble = [.. Enumerable.Range(0, 120).Select(i => (i * 53 % 17) / 8.0)];
    private static readonly OhlcvBars Ohlcv = new(
        [.. Prices.Select(p => p + 0.5)], [.. Prices.Select(p => p - 0.5)], Prices, [.. Prices.Select((p, i) => p - (i % 2 * 0.2))],
        [.. Enumerable.Range(0, 120).Select(i => 1000.0 + (i * 13 % 7 * 100))]);
    private static readonly double[][] Features = [.. Enumerable.Range(0, 30).Select(i => new[] { Prices[i], Prices[i + 1] })];
    private static readonly double[][] Signals = [[0.2, 0.3, 0.5], [0.8, 0.5, 0.1]];
    private static readonly TransferEntropyOptions Windowed = new(Neighbours: 3, Window: 40, Stride: 5);

    private static LineSeries ALine() => new((Vec)X, (Vec)Y);

    private static readonly Action<AxesBuilder> Nothing = _ => { };

    private static VerbRow OnAxes<T>(Expression<Func<AxesBuilder, Action<T>?, object>> call, int? series = 1,
        Func<Figure, IEnumerable<object>>? holder = null, NoCallback noCallback = NoCallback.Accepted, int runs = 1) =>
        new(call, series, runs, holder, noCallback);

    private static VerbRow OnTheModel<T>(Expression<Func<Axes, Action<T>?, object>> call) => new(call);

    private static VerbRow OnFigure<T>(Expression<Func<FigureBuilder, Action<T>?, object>> call, int? series,
        Func<Figure, IEnumerable<object>>? holder = null, NoCallback noCallback = NoCallback.Accepted) =>
        new(call, series, 1, holder, noCallback);

    private static IEnumerable<object> Overlays(Figure figure, Func<Axes, IEnumerable<object>> list) => figure.SubPlots.SelectMany(list);

    /// <summary>The series of the first right Y axis of the one subplot: what a slot's verb draws on, and nothing else.</summary>
    private static IEnumerable<object> OnTheRightSlot(Figure figure) => figure.SubPlots[0].YAxes.Right[0].Series;

    /// <summary>The series of the top X axis of the one subplot.</summary>
    private static IEnumerable<object> OnTheTopAxis(Figure figure) => figure.SubPlots[0].XSecondarySeries;

    /// <summary>The axes builder's own verbs, in the order of its pinned surface.</summary>
    private static readonly VerbRow[] AxesVerbs =
    [
        OnAxes<AdaptiveStochastic>((ax, c) => ax.AdaptiveStochastic(Ohlcv, 3, c)),
        OnAxes<global::MatPlotLibNet.Models.Tools.FibonacciRetracement>((ax, c) => ax.AddFibonacci(110, 100, c), 0, f => Overlays(f, a => a.FibonacciRetracements)),
        OnAxes<AxesBuilder>((ax, c) => ax.AddInset(Inset, c!), 0, noCallback: NoCallback.Refused),
        OnAxes<AxesBuilder>((ax, c) => ax.AddInset(0.6, 0.6, 0.3, 0.3, c!), 0, noCallback: NoCallback.Refused),
        OnAxes<HorizontalLevel>((ax, c) => ax.AddLevel(105, c), 0, f => Overlays(f, a => a.HorizontalLevels)),
        OnAxes<LineSeries>((ax, c) => ax.AddSeries(ALine(), c)),
        OnAxes<SignalMarker>((ax, c) => ax.AddSignal(1, 2, SignalDirection.Buy, c), 0, f => Overlays(f, a => a.Signals)),
        OnAxes<Trendline>((ax, c) => ax.AddTrendline(0, 1, 5, 6, c), 0, f => Overlays(f, a => a.Trendlines)),
        OnAxes<AmihudIlliquidity>((ax, c) => ax.AmihudIlliquidity(Ohlcv, 5, c)),
        OnAxes<Annotation>((ax, c) => ax.Annotate("note", 2, 3, c), 0, f => Overlays(f, a => a.Annotations)),
        OnAxes<Annotation>((ax, c) => ax.Annotate("note", 2, 3, 4, 5, c), 0, f => Overlays(f, a => a.Annotations)),
        OnAxes<AroonOscillator>((ax, c) => ax.AroonOscillator(Ohlcv, 10, c)),
        OnAxes<ReferenceLine>((ax, c) => ax.AxHLine(3.5, c), 0, f => Overlays(f, a => a.ReferenceLines)),
        OnAxes<SpanRegion>((ax, c) => ax.AxHSpan(2, 4, c), 0, f => Overlays(f, a => a.Spans)),
        OnAxes<ReferenceLine>((ax, c) => ax.AxVLine(1.5, c), 0, f => Overlays(f, a => a.ReferenceLines)),
        OnAxes<SpanRegion>((ax, c) => ax.AxVSpan(1, 2, c), 0, f => Overlays(f, a => a.Spans)),
        OnAxes<BarSeries>((ax, c) => ax.Bar(Labels, X, c)),
        OnAxes<Bar3DSeries>((ax, c) => ax.Bar3D(Axis, Axis, Axis, c)),
        OnAxes<BarbsSeries>((ax, c) => ax.Barbs(X, Y, X, Y, c)),
        OnAxes<Bocpd>((ax, c) => ax.Bocpd(Prices, 0.01, 1, 50, c)),
        OnAxes<BollingerBands>((ax, c) => ax.Plot(Bars, Prices, null).BollingerBands(5, 2, c), 3),
        OnAxes<BoxSeries>((ax, c) => ax.BoxPlot(Samples, c)),
        OnAxes<BrokenBarSeries>((ax, c) => ax.BrokenBarH(Ranges, c)),
        OnAxes<BubbleSeries>((ax, c) => ax.Bubble(X, Y, Z, c)),
        OnAxes<BulletGraphSeries>((ax, c) => ax.Bullet(2412, c)),
        OnAxes<SignalMarker>((ax, c) => ax.BuyAt(1, 2, c), 0, f => Overlays(f, a => a.Signals)),
        OnAxes<CandlestickSeries>((ax, c) => ax.Candlestick(Y, Z, X, Y, null, c)),
        OnAxes<Cci>((ax, c) => ax.Cci(Ohlcv, 5, c)),
        OnAxes<CgOscillator>((ax, c) => ax.CgOscillator(Prices, 10, c)),
        OnAxes<ChordSeries>((ax, c) => ax.Chord(Pair, Grid, c)),
        OnAxes<ClustermapSeries>((ax, c) => ax.Clustermap(Grid, c)),
        OnAxes<ContourSeries>((ax, c) => ax.Contour(Axis, Axis, Grid, c)),
        OnAxes<Contour3DSeries>((ax, c) => ax.Contour3D(Axis, Axis, Field, c)),
        OnAxes<ContourfSeries>((ax, c) => ax.Contourf(Axis, Axis, Grid, c)),
        OnAxes<CorwinSchultz>((ax, c) => ax.CorwinSchultz(Ohlcv, 5, c)),
        OnAxes<CountSeries>((ax, c) => ax.Countplot(Labels, c)),
        OnAxes<Cusum>((ax, c) => ax.Cusum(Prices, 0.02, 0, c)),
        OnAxes<CyberCycle>((ax, c) => ax.CyberCycle(Prices, 0.07, c)),
        OnAxes<Decycler>((ax, c) => ax.Decycler(Prices, 20, c)),
        OnAxes<DendrogramSeries>((ax, c) => ax.Dendrogram(Clustered, c)),
        OnAxes<ScatterSeries>((ax, c) => ax.DensityScatter(X, Y, c, 0)),
        OnAxes<DispersionIndex>((ax, c) => ax.DispersionIndex(Signals, c)),
        OnAxes<DonutSeries>((ax, c) => ax.Donut(X, null, c)),
        OnAxes<EaseOfMovement>((ax, c) => ax.EaseOfMovement(Ohlcv, 5, 1e5, c)),
        OnAxes<EcdfSeries>((ax, c) => ax.Ecdf(X, c)),
        OnAxes<EhlersITrend>((ax, c) => ax.EhlersITrend(Prices, c)),
        OnAxes<EhlersSineWave>((ax, c) => ax.EhlersSineWave(Prices, c), 2),
        OnAxes<EhlersSuperSmoother>((ax, c) => ax.EhlersSuperSmoother(Prices, 10, c)),
        OnAxes<Ema>((ax, c) => ax.Plot(Bars, Prices, null).Ema(5, c), 2),
        OnAxes<ErrorBarSeries>((ax, c) => ax.ErrorBar(X, Y, Ones, Ones, c)),
        OnAxes<EventplotSeries>((ax, c) => ax.Eventplot(Samples, c)),
        OnAxes<FractionalDifferentiation>((ax, c) => ax.Ffd(Prices, 0.4, 1e-2, c)),
        OnAxes<AreaSeries>((ax, c) => ax.FillBetween(X, Y, null, c)),
        OnAxes<AreaSeries>((ax, c) => ax.FillBetweenX(Y, X, null, c)),
        OnAxes<ForceIndex>((ax, c) => ax.ForceIndex(Ohlcv, 5, c)),
        OnAxes<FunnelSeries>((ax, c) => ax.Funnel(Labels, X, c)),
        OnAxes<GanttSeries>((ax, c) => ax.Gantt(Labels, X, Z, c)),
        OnAxes<GarmanKlass>((ax, c) => ax.GarmanKlass(Ohlcv, 5, c)),
        OnAxes<GaugeSeries>((ax, c) => ax.Gauge(0.7, c)),
        OnAxes<BarSeries>((ax, c) => ax.GroupedBar(Labels, Groups, c), 2, runs: 2),
        OnAxes<HeatmapSeries>((ax, c) => ax.Heatmap(Grid, c)),
        OnAxes<HexbinSeries>((ax, c) => ax.Hexbin(X, Y, c)),
        OnAxes<HistogramSeries>((ax, c) => ax.Hist(X, 10, c)),
        OnAxes<Histogram2DSeries>((ax, c) => ax.Histogram2D(X, Y, 20, c)),
        OnAxes<IcicleSeries>((ax, c) => ax.Icicle(Tree, c)),
        OnAxes<ImageSeries>((ax, c) => ax.Image(Grid, c)),
        OnAxes<ZoomIndicator>((ax, c) => ax.AddInset(Inset, inset => inset.IndicateZoom(c)), 0,
            f => f.SubPlots.SelectMany(a => a.Insets).Select(inset => inset.ZoomIndicator).OfType<object>()),
        OnAxes<InverseFisherTransform>((ax, c) => ax.InverseFisherTransform(Wobble, 1, c)),
        OnAxes<KaufmanEfficiencyRatio>((ax, c) => ax.KaufmanEfficiencyRatio(Prices, 10, c)),
        OnAxes<KdeSeries>((ax, c) => ax.Kde(X, c)),
        OnAxes<KlingerVolumeOscillator>((ax, c) => ax.KlingerVolumeOscillator(Ohlcv, 3, 5, 2, c), 2),
        OnAxes<LaguerreRsi>((ax, c) => ax.LaguerreRsi(Prices, 0.2, c)),
        OnAxes<MamaFama>((ax, c) => ax.MamaFama(Prices, 0.5, 0.05, c), 2),
        OnAxes<SunburstSeries>((ax, c) => ax.NestedPie(Tree, c)),
        OnAxes<NetworkGraphSeries>((ax, c) => ax.NetworkGraph(Nodes, Edges, c)),
        OnAxes<Obv>((ax, c) => ax.Obv(Prices, Ohlcv.Volume, c)),
        OnAxes<OhlcBarSeries>((ax, c) => ax.OhlcBar(Y, Z, X, Y, null, c)),
        OnAxes<PairGridSeries>((ax, c) => ax.PairGrid(Samples, c)),
        OnAxes<ParabolicSar>((ax, c) => ax.ParabolicSar(Ohlcv.High, Ohlcv.Low, 0.02, 0.2, c), 2),
        OnAxes<PcolormeshSeries>((ax, c) => ax.Pcolormesh(Edges3, Edges3, Grid, c)),
        OnAxes<PermutationEntropy>((ax, c) => ax.PermutationEntropy(Prices, 3, 20, c)),
        OnAxes<PieSeries>((ax, c) => ax.Pie(X, null, c)),
        OnAxes<PiperSeries>((ax, c) => ax.Piper(Water, c)),
        OnAxes<PlanarBar3DSeries>((ax, c) => ax.PlanarBar3D(Axis, Axis, Axis, c)),
        OnAxes<LineSeries>((ax, c) => ax.Plot(Dates, Y, c)),
        OnAxes<LineSeries>((ax, c) => ax.Plot(X, Y, c)),
        OnAxes<Line3DSeries>((ax, c) => ax.Plot3D(X, Y, Z, c)),
        OnAxes<PointAndFigureSeries>((ax, c) => ax.PointAndFigure(Prices, 1, 3, c)),
        OnAxes<PointplotSeries>((ax, c) => ax.Pointplot(Samples, c)),
        OnAxes<PolarBarSeries>((ax, c) => ax.PolarBar(X, Y, c)),
        OnAxes<PolarHeatmapSeries>((ax, c) => ax.PolarHeatmap(Grid, 4, 2, c)),
        OnAxes<PolarLineSeries>((ax, c) => ax.PolarPlot(X, Y, c)),
        OnAxes<PolarScatterSeries>((ax, c) => ax.PolarScatter(X, Y, c)),
        OnAxes<ProgressBarSeries>((ax, c) => ax.ProgressBar(0.5, c)),
        OnAxes<QuiverSeries>((ax, c) => ax.Quiver(X, Y, Ones, Ones, c)),
        OnAxes<Quiver3DSeries>((ax, c) => ax.Quiver3D(X, Y, Z, Ones, Ones, Ones, c)),
        OnAxes<QuiverKeySeries>((ax, c) => ax.QuiverKey(0.5, 0.9, 1, "key", c)),
        OnAxes<RadarSeries>((ax, c) => ax.Radar(Labels, X, c)),
        OnAxes<RegressionSeries>((ax, c) => ax.Regression(X, Y, c)),
        OnAxes<RelativeRotationSeries>((ax, c) => ax.RelativeRotation(Closes, Benchmark, Assets, c)),
        OnAxes<RelativeVigorIndex>((ax, c) => ax.RelativeVigorIndex(Ohlcv, 5, c), 2),
        OnAxes<ResidualSeries>((ax, c) => ax.Residplot(X, Y, c)),
        OnAxes<RollSpread>((ax, c) => ax.RollSpread(Prices, 10, c)),
        OnAxes<RoofingFilter>((ax, c) => ax.RoofingFilter(Prices, 48, 10, c)),
        OnAxes<Rsi>((ax, c) => ax.Rsi(Prices, 14, c)),
        OnAxes<RugplotSeries>((ax, c) => ax.Rugplot(X, c)),
        OnAxes<SankeySeries>((ax, c) => ax.Sankey(Stages, Flows, c)),
        OnAxes<ScatterSeries>((ax, c) => ax.Scatter(Dates, Y, c)),
        OnAxes<ScatterSeries>((ax, c) => ax.Scatter(X, Y, c)),
        OnAxes<Scatter3DSeries>((ax, c) => ax.Scatter3D(X, Y, Z, c)),
        OnAxes<SignalMarker>((ax, c) => ax.SellAt(1, 2, c), 0, f => Overlays(f, a => a.Signals)),
        OnAxes<SignalSeries>((ax, c) => ax.Signal(Y, 1, 0, c)),
        OnAxes<SignalXYSeries>((ax, c) => ax.SignalXY(X, Y, c)),
        OnAxes<Sma>((ax, c) => ax.Plot(Bars, Prices, null).Sma(5, c), 2),
        OnAxes<SparklineSeries>((ax, c) => ax.Sparkline(X, c)),
        OnAxes<SpectrogramSeries>((ax, c) => ax.Spectrogram(Wobble, 1000, c)),
        OnAxes<SqueezeMomentum>((ax, c) => ax.SqueezeMomentum(Ohlcv, 5, 2, 1.5, c)),
        OnAxes<StackedAreaSeries>((ax, c) => ax.StackPlot(X, Layers, c)),
        OnAxes<StateTimelineSeries>((ax, c) => ax.StateTimeline(Segments, c)),
        OnAxes<StatTileSeries>((ax, c) => ax.StatTile(42, c)),
        OnAxes<StemSeries>((ax, c) => ax.Stem(X, Y, c)),
        OnAxes<Stem3DSeries>((ax, c) => ax.Stem3D(X, Y, Z, c)),
        OnAxes<StepSeries>((ax, c) => ax.Step(X, Y, c)),
        OnAxes<StreamingCandlestickSeries>((ax, c) => ax.StreamingCandlestick(8, c)),
        OnAxes<StreamingLineSeries>((ax, c) => ax.StreamingPlot(8, c)),
        OnAxes<StreamingScatterSeries>((ax, c) => ax.StreamingScatter(8, c)),
        OnAxes<StreamingSignalSeries>((ax, c) => ax.StreamingSignal(8, 1, c)),
        OnAxes<StreamplotSeries>((ax, c) => ax.Streamplot(Axis, Axis, Field, Field, c)),
        OnAxes<StripplotSeries>((ax, c) => ax.Stripplot(Samples, c)),
        OnAxes<SunburstSeries>((ax, c) => ax.Sunburst(Tree, c)),
        OnAxes<Supertrend>((ax, c) => ax.Supertrend(Ohlcv, 10, 3, c)),
        OnAxes<SurfaceSeries>((ax, c) => ax.Surface(Axis, Axis, Field, c)),
        OnAxes<SwarmplotSeries>((ax, c) => ax.Swarmplot(Samples, c)),
        OnAxes<TableSeries>((ax, c) => ax.Table(Cells, c)),
        OnAxes<TernaryLineSeries>((ax, c) => ax.TernaryPlot(Fractions, Fractions, Fractions, c)),
        OnAxes<TernaryScatterSeries>((ax, c) => ax.TernaryScatter(Fractions, Fractions, Fractions, c)),
        OnAxes<Text3DSeries>((ax, c) => ax.Text3D(1, 2, 3, "hello", c)),
        OnAxes<TransferEntropy>((ax, c) => ax.TransferEntropy(Wobble, Prices, 4, 2, c), 0),
        OnAxes<TransferEntropy>((ax, c) => ax.TransferEntropy(Wobble, Prices, Windowed, c)),
        OnAxes<TreeGridSeries>((ax, c) => ax.TreeGrid(Rows3, c)),
        OnAxes<TreemapSeries>((ax, c) => ax.Treemap(Tree, c)),
        OnAxes<TricontourSeries>((ax, c) => ax.Tricontour(TriangleX, TriangleY, TriangleZ, c)),
        OnAxes<TripcolorSeries>((ax, c) => ax.Tripcolor(TriangleX, TriangleY, TriangleZ, c)),
        OnAxes<Trisurf3DSeries>((ax, c) => ax.Trisurf(Edges3, Edges3, Slope, c)),
        OnAxes<TurbulenceIndex>((ax, c) => ax.TurbulenceIndex(Features, 3, 1e-6, c)),
        OnAxes<TwiggsMoneyFlow>((ax, c) => ax.TwiggsMoneyFlow(Ohlcv, 5, c)),
        OnAxes<ViolinSeries>((ax, c) => ax.Violin(Samples, c)),
        OnAxes<VolumeProfile>((ax, c) => ax.VolumeProfile(Ohlcv, 12, c)),
        OnAxes<VoxelSeries>((ax, c) => ax.Voxels(Filled, c)),
        OnAxes<Vpin>((ax, c) => ax.Vpin(Ohlcv, 5, 5, c)),
        OnAxes<Vwap>((ax, c) => ax.Vwap(Ohlcv, 0, c)),
        OnAxes<VwapZScore>((ax, c) => ax.VwapZScore(Ohlcv, 5, c)),
        OnAxes<WaffleSeries>((ax, c) => ax.Waffle(Three, Shares, c)),
        OnAxes<WaterfallSeries>((ax, c) => ax.Waterfall(Labels, X, c)),
        OnAxes<WaveletEnergyRatio>((ax, c) => ax.WaveletEnergyRatio(Prices, 16, 0, c)),
        OnAxes<WaveletEntropy>((ax, c) => ax.WaveletEntropy(Prices, 16, c)),
        OnAxes<WilliamsR>((ax, c) => ax.WilliamsR(Ohlcv, 5, c)),
        OnAxes<WireframeSeries>((ax, c) => ax.Wireframe(Axis, Axis, Field, c)),
        OnAxes<XAxisSlotBuilder>((ax, c) => ax.WithAddBottomXAxis(c!), 0, noCallback: NoCallback.Refused),
        OnAxes<YAxisSlotBuilder>((ax, c) => ax.WithAddLeftYAxis(c!), 0, noCallback: NoCallback.Refused),
        OnAxes<YAxisSlotBuilder>((ax, c) => ax.WithAddRightYAxis(c!), 0, noCallback: NoCallback.Refused),
        OnAxes<SecondaryXAxisBuilder>((ax, c) => ax.WithSecondaryXAxis(c!), 0, noCallback: NoCallback.Unguarded),
        OnAxes<WordCloudSeries>((ax, c) => ax.WordCloud(Three, Weights, c)),
        OnAxes<LineSeries>((ax, c) => ax.WithAddRightYAxis(slot => slot.Plot(X, Y, c)), holder: OnTheRightSlot),
        OnAxes<ScatterSeries>((ax, c) => ax.WithAddRightYAxis(slot => slot.Scatter(X, Y, c)), holder: OnTheRightSlot),
        OnAxes<StreamingLineSeries>((ax, c) => ax.WithAddRightYAxis(slot => slot.StreamingPlot(8, c)), holder: OnTheRightSlot),
        OnAxes<LineSeries>((ax, c) => ax.WithSecondaryXAxis(top => top.PlotXSecondary(X, Y, c)), holder: OnTheTopAxis),
        OnAxes<ScatterSeries>((ax, c) => ax.WithSecondaryXAxis(top => top.ScatterXSecondary(X, Y, c)), holder: OnTheTopAxis),
        OnAxes<YangZhang>((ax, c) => ax.YangZhang(Ohlcv, 5, c)),
        OnAxes<YangZhangVolRatio>((ax, c) => ax.YangZhangVolRatio(Ohlcv, 5, 10, c)),
    ];

    /// <summary>The figure builder's verbs that are its own, not a forward to the axes verb of the same name.</summary>
    private static readonly VerbRow[] FigureOwnVerbs =
    [
        OnFigure<Axes>((fig, c) => fig.AddSubPlot(1, 1, 1, Nothing, null).AddInset(0, Inset, c), 0, f => f.SubPlots.SelectMany(a => a.Insets)),
        OnFigure<Axes>((fig, c) => fig.AddSubPlot(1, 1, 1, Nothing, null).AddInset(0, 0.6, 0.6, 0.3, 0.3, c), 0, f => f.SubPlots.SelectMany(a => a.Insets)),
        OnFigure<AxesBuilder>((fig, c) => fig.AddSubPlot(1, 1, 1, c!, null), 0, noCallback: NoCallback.Unguarded),
        OnFigure<AxesBuilder>((fig, c) => fig.AddSubPlot(Whole, c!, null), 0, noCallback: NoCallback.Unguarded),
        OnFigure<FigureImage>((fig, c) => fig.FigImage(Grid, 0, 0, c), 0, f => f.Images),
        OnFigure<FigureText>((fig, c) => fig.FigText("note", 0.5, 0.5, c), 0, f => f.Texts),
        OnFigure<ServerInteractionBuilder>((fig, c) => fig.WithServerInteraction("chart", c!), 0, noCallback: NoCallback.Unguarded),
    ];

    /// <summary>The verbs of the axes model that take a callback of their own (the builder's verb of the same name configures the
    /// series it is handed back instead).</summary>
    private static readonly VerbRow[] ModelVerbs =
    [
        OnTheModel<ClustermapSeries>((axes, c) => axes.Clustermap(Grid, c)),
        OnTheModel<NetworkGraphSeries>((axes, c) => axes.NetworkGraph(Nodes, Edges, c)),
        OnTheModel<PairGridSeries>((axes, c) => axes.PairGrid(Samples, c)),
        OnTheModel<RelativeRotationSeries>((axes, c) => axes.RelativeRotation(Closes, Benchmark, Assets, c)),
    ];

    /// <summary>Every row: the axes builder's verbs, the composed charts, the axes model's verbs, the figure builder's own verbs, and
    /// the figure verbs that draw on the one subplot, each derived from its axes verb.</summary>
    public static IReadOnlyList<VerbRow> Rows { get; } =
    [
        .. AxesVerbs,
        .. ComposedChartShapes.All.Where(shape => shape.WithCallback is not null).Select(shape => shape.AsVerbRow()),
        .. ModelVerbs,
        .. FigureOwnVerbs,
        .. AxesVerbs.Select(row => row.OnTheFigure()).OfType<VerbRow>(),
    ];

    /// <summary>The figure verbs that draw on the figure's one subplot, as a Theory carries them.</summary>
    public static TheoryData<string> Forwards { get; } =
        new(AxesVerbs.Select(row => row.OnTheFigure()).OfType<VerbRow>().Select(row => row.Name));

    /// <summary>The indicator verbs of the axes builder, as a Theory carries them.</summary>
    public static TheoryData<string> Indicators { get; } =
        new(AxesVerbs.Where(row => typeof(Indicator).IsAssignableFrom(row.Configured)).Select(row => row.Name));

    /// <summary>The composed-chart verbs of the axes builder (<see cref="Packaging.VerbFamilies"/>), as a Theory carries them: they
    /// check what they are handed, the builder they are called on first, before they touch the axes. A verb that forwards to one axes
    /// verb reads its builder at once and meets a missing one as the method of the class it was did.</summary>
    public static TheoryData<string> Extensions { get; } =
        new(Rows.Where(row => row.Method.IsStatic && row.Owner == typeof(AxesBuilder) && Packaging.VerbFamilies.All.Contains(row.Method.DeclaringType!))
            .Select(row => row.Name));

    /// <summary>The row called <paramref name="name"/>.</summary>
    /// <exception cref="ArgumentException">No row has that name.</exception>
    public static VerbRow Named(string name) =>
        Rows.FirstOrDefault(row => row.Name == name) is { Call: not null } row
            ? row
            : throw new ArgumentException($"No verb row is called '{name}'.", nameof(name));

    /// <summary>An overload as a row names it: the builder it is called on, the verb, and the types it takes after the builder.</summary>
    /// <param name="receiver">The builder.</param>
    /// <param name="method">The overload.</param>
    public static string NameOf(Type receiver, MethodInfo method) =>
        $"{receiver.Name}.{method.Name}({string.Join(", ", method.GetParameters().Skip(method.IsStatic ? 1 : 0).Select(p => TypeName(p.ParameterType)))})";

    private static string TypeName(Type type) => type.IsGenericType
        ? $"{type.Name[..type.Name.IndexOf('`')]}<{string.Join(", ", type.GetGenericArguments().Select(TypeName))}>"
        : type.Name;

    /// <summary>The call in <paramref name="body"/> that is handed <paramref name="callback"/> itself.</summary>
    internal static MethodCallExpression? CallTaking(Expression body, ParameterExpression callback)
    {
        var finder = new CallFinder(callback);
        finder.Visit(body);
        return finder.Found;
    }

    private sealed class CallFinder(ParameterExpression callback) : ExpressionVisitor
    {
        public MethodCallExpression? Found { get; private set; }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Arguments.Contains(callback)) Found = node;
            return base.VisitMethodCall(node);
        }
    }

    extension(VerbRow row)
    {
        /// <summary>The figure builder's verb of the same name and parameters, called with the same arguments on the figure's one
        /// subplot; <see langword="null"/> when the figure builder has no such verb.</summary>
        public VerbRow? OnTheFigure()
        {
            // The builder the verb is called on: the object of a method of the class, the first argument of an extension member.
            int skip = row.Method.IsStatic ? 1 : 0;
            var target = skip == 1 ? row.Body.Arguments[0] : row.Body.Object;
            if (row.Receiver != typeof(AxesBuilder) || target != row.Call.Parameters[0]) return null;

            var parameters = row.Method.GetParameters().Skip(skip).Select(p => p.ParameterType);
            if (FigureCalls.FirstOrDefault(c => c.Method.Name == row.Method.Name && c.Parameters.Select(p => p.ParameterType).SequenceEqual(parameters))
                is not { Method: not null } forward) return null;

            var figure = Expression.Parameter(typeof(FigureBuilder), "fig");
            var arguments = row.Body.Arguments.Skip(skip);
            var verb = forward.IsExtension ? Expression.Call(forward.Method, [figure, .. arguments]) : Expression.Call(figure, forward.Method, arguments);
            return row with { Call = Expression.Lambda(Expression.Convert(verb, typeof(object)), figure, row.Call.Parameters[1]) };
        }
    }

    extension(ComposedShape shape)
    {
        /// <summary>The composed chart's verb as a row of the catalogue: its call with the caller's callback, and how often the
        /// callback runs; how many series it draws is the shape's own business.</summary>
        public VerbRow AsVerbRow() => new(shape.WithCallback!, Series: null, Runs: shape.CallbackRuns);
    }
}
