// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>Where a series sits on its axes: the primary list, or one of the two secondary axes, which the serializer
/// writes into lists of their own and reads back through factories of their own.</summary>
public enum SeriesSeat
{
    /// <summary>The axes' own series list.</summary>
    Primary,

    /// <summary>The secondary (right-hand) Y axis.</summary>
    SecondaryY,

    /// <summary>The secondary (top) X axis.</summary>
    SecondaryX,
}

/// <summary>The series types the JSON of a figure is guarded for, and every property of theirs that changes the
/// picture. The list of properties is REFLECTED off the types, never written by hand: a property added tomorrow joins
/// the contract on its own, and the JSON has to carry it or a test names it. A property that no JSON can carry is named
/// in <see cref="NotCarried"/> with the reason.
/// <para>The contract compares settable properties, not the data a series is built from. The chart types whose data still
/// comes back as a placeholder (the roadmap row "Every chart type back from JSON") are under it all the same: their
/// settings are what they draw on top of their data, and they travel already.</para>
/// <para>Public API only, so the Mcp test project links this very file and runs the same contract through its spec
/// reader.</para></summary>
public static class DrawnSeriesFixtures
{
    /// <summary>The number of points (and of datasets, and of stacked layers) every fixture series holds, so that every
    /// per-point array a property carries has the length the data has.</summary>
    public const int Points = 3;

    /// <summary>The series types under the contract: every series type the library ships except the ones
    /// <see cref="NotUnderContract"/> names with a reason. A Theory over the composed-chart fixture turns red on a shape
    /// built from a type missing here, and <see cref="SeriesDrawnPropertiesRoundTripTests"/> turns red on a shipped type
    /// that is neither listed here nor named there.</summary>
    public static IReadOnlyList<Type> DrawnTypes { get; } =
    [
        typeof(LineSeries), typeof(ScatterSeries), typeof(StemSeries), typeof(BarSeries), typeof(AreaSeries),
        typeof(ErrorBarSeries), typeof(HeatmapSeries), typeof(StepSeries), typeof(StackedAreaSeries),
        typeof(ViolinSeries), typeof(BoxSeries), typeof(StripplotSeries), typeof(KdeSeries), typeof(WaterfallSeries),
        typeof(ContourfSeries), typeof(HexbinSeries), typeof(Histogram2DSeries), typeof(ImageSeries),
        typeof(PcolormeshSeries), typeof(SpectrogramSeries), typeof(TricontourSeries), typeof(TripcolorSeries),
        typeof(ClustermapSeries), typeof(DendrogramSeries), typeof(SunburstSeries), typeof(TreemapSeries),
        typeof(PolarHeatmapSeries), typeof(Scatter3DSeries), typeof(SurfaceSeries), typeof(Trisurf3DSeries),
        typeof(Bar3DSeries), typeof(BarbsSeries), typeof(BrokenBarSeries), typeof(BubbleSeries), typeof(BulletGraphSeries),
        typeof(CandlestickSeries), typeof(Contour3DSeries), typeof(ContourSeries), typeof(CountSeries), typeof(DonutSeries),
        typeof(EcdfSeries), typeof(EventplotSeries), typeof(FunnelSeries), typeof(GanttSeries), typeof(GaugeSeries),
        typeof(HistogramSeries), typeof(Line3DSeries), typeof(NetworkGraphSeries), typeof(OhlcBarSeries), typeof(PairGridSeries),
        typeof(PieSeries), typeof(PointplotSeries), typeof(PolarBarSeries), typeof(PolarLineSeries), typeof(PolarScatterSeries),
        typeof(ProgressBarSeries), typeof(Quiver3DSeries), typeof(QuiverKeySeries), typeof(QuiverSeries), typeof(RadarSeries),
        typeof(RegressionSeries), typeof(RelativeRotationSeries), typeof(ResidualSeries), typeof(RugplotSeries),
        typeof(SankeySeries), typeof(SignalSeries), typeof(SignalXYSeries), typeof(SparklineSeries), typeof(StatTileSeries),
        typeof(StateTimelineSeries), typeof(Stem3DSeries), typeof(StreamplotSeries), typeof(SwarmplotSeries),
        typeof(TableSeries), typeof(Text3DSeries), typeof(TreeGridSeries), typeof(VoxelSeries), typeof(WireframeSeries),
    ];

    /// <summary>The series types the contract leaves out, each with the reason: types no JSON gives back as their own
    /// type, so there is no property of theirs to compare. A type is either in <see cref="DrawnTypes"/> or named here;
    /// <see cref="SeriesDrawnPropertiesRoundTripTests"/> turns red on a shipped series type that is neither, so the next
    /// series cannot slip past the contract unseen.</summary>
    public static IReadOnlyDictionary<Type, string> NotUnderContract { get; } = new Dictionary<Type, string>
    {
        [typeof(StreamingLineSeries)] = LiveSeries,
        [typeof(StreamingScatterSeries)] = LiveSeries,
        [typeof(StreamingSignalSeries)] = LiveSeries,
        [typeof(StreamingCandlestickSeries)] = LiveSeries,
        [typeof(PlanarBar3DSeries)] = "it writes the type 'planarbar3d' and the reader has no factory for it, so it does not come back as its "
            + "own type; the roadmap row 'Live series back from JSON' gives it and the streaming series their way back",
    };

    private const string LiveSeries =
        "a live series is written to JSON and dropped when the JSON is read, so it does not come back as its own type; "
        + "the roadmap row 'Live series back from JSON' gives it its way back";

    /// <summary>The settable properties no JSON carries, as <c>Type.Property</c>, each with the reason.</summary>
    public static IReadOnlyDictionary<string, string> NotCarried { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["AreaSeries.Where"] = "a delegate is code, and a JSON document carries data only",
        ["ClustermapSeries.RowTree"] = "a hierarchy is not written to JSON yet: the roadmap row 'Every chart type back from JSON' gives treemap, sunburst, dendrogram and clustermap their trees",
        ["ClustermapSeries.ColumnTree"] = "a hierarchy is not written to JSON yet: the roadmap row 'Every chart type back from JSON' gives treemap, sunburst, dendrogram and clustermap their trees",
    };

    /// <summary>The public settable properties of <paramref name="type"/> that the JSON must carry, by name.
    /// <para>The rule: a property is drawn when a user can set it, that is every public instance property with a public
    /// setter and no index, declared on the type or inherited (label, visibility and z-order included). A public
    /// setting nothing draws would be decoration, which a series does not carry, so none is presumed harmless; the one
    /// way out is a row in <see cref="NotCarried"/> that says why no JSON can carry it.</para></summary>
    public static IReadOnlyList<PropertyInfo> DrawnProperties(Type type) =>
        [.. type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.SetMethod is { IsPublic: true } && p.GetIndexParameters().Length == 0)
            .Where(p => !NotCarried.ContainsKey($"{type.Name}.{p.Name}"))
            .OrderBy(p => p.Name, StringComparer.Ordinal)];

    /// <summary>One row per series type under the contract and drawn property: the type's name and the property's name.</summary>
    public static TheoryData<string, string> Rows { get; } =
        new(DrawnTypes.SelectMany(t => DrawnProperties(t).Select(p => (t.Name, p.Name))));

    /// <summary>The rows of the two series types a secondary axis holds, once per secondary axis.</summary>
    public static TheoryData<string, string, SeriesSeat> SecondaryRows { get; } =
        new(from type in new[] { typeof(LineSeries), typeof(ScatterSeries) }
            from seat in new[] { SeriesSeat.SecondaryY, SeriesSeat.SecondaryX }
            from property in DrawnProperties(type)
            select (type.Name, property.Name, seat));

    /// <summary>A figure that holds one series of the type named <paramref name="typeName"/> at <paramref name="seat"/>:
    /// at rest, or with every drawn property set to a value that is not its default.</summary>
    /// <exception cref="ArgumentException">No series type under the contract has that name, or that type cannot sit on a secondary axis.</exception>
    public static Figure Build(string typeName, bool everyPropertySet, SeriesSeat seat = SeriesSeat.Primary)
    {
        double[] x = [1, 2, 3], y = [2, 4, 3], low = [0.5, 0.5, 0.5];
        double[][] sets = [[1, 2, 3], [2, 3, 4], [3, 4, 5]];
        double[,] grid = { { 1, 2, 3 }, { 4, 5, 6 }, { 7, 8, 9 } };
        double[] edges = [1, 2, 3, 4];   // a mesh has one edge more than it has cells along each axis
        string[] categories = ["A", "B", "C"];
        var tree = new TreeNode { Label = "Root" };
        void Set<T>(T series) where T : ISeries
        {
            if (everyPropertySet)
            {
                SetEveryDrawnProperty(series);
            }
        }

        return Plt.Create().AddSubPlot(1, 1, 1, ax => _ = (typeName, seat) switch
        {
            (nameof(LineSeries), SeriesSeat.SecondaryY) => ax.WithAddRightYAxis(y2 => y2.Plot(x, y, Set)),
            (nameof(ScatterSeries), SeriesSeat.SecondaryY) => ax.WithAddRightYAxis(y2 => y2.Scatter(x, y, Set)),
            (nameof(LineSeries), SeriesSeat.SecondaryX) => ax.WithSecondaryXAxis(x2 => x2.PlotXSecondary(x, y, Set)),
            (nameof(ScatterSeries), SeriesSeat.SecondaryX) => ax.WithSecondaryXAxis(x2 => x2.ScatterXSecondary(x, y, Set)),
            (_, not SeriesSeat.Primary) => throw new ArgumentException($"A {typeName} has no seat on a secondary axis.", nameof(seat)),
            (nameof(LineSeries), _) => ax.Plot(x, y, Set),
            (nameof(ScatterSeries), _) => ax.Scatter(x, y, Set),
            (nameof(StemSeries), _) => ax.Stem(x, y, Set),
            (nameof(BarSeries), _) => ax.Bar(categories, y, Set),
            (nameof(AreaSeries), _) => ax.FillBetween(x, y, null, Set),
            (nameof(ErrorBarSeries), _) => ax.ErrorBar(x, y, low, low, Set),
            (nameof(HeatmapSeries), _) => ax.Heatmap(new double[,] { { 1, 2, 3 }, { 4, 5, 6 }, { 7, 8, 9 } }, Set),
            (nameof(StepSeries), _) => ax.Step(x, y, Set),
            (nameof(StackedAreaSeries), _) => ax.StackPlot(x, sets, Set),
            (nameof(ViolinSeries), _) => ax.Violin(sets, Set),
            (nameof(BoxSeries), _) => ax.BoxPlot(sets, Set),
            (nameof(StripplotSeries), _) => ax.Stripplot(sets, Set),
            (nameof(KdeSeries), _) => ax.Kde(y, Set),
            (nameof(WaterfallSeries), _) => ax.Waterfall(categories, y, Set),
            (nameof(ContourfSeries), _) => ax.Contourf(x, x, grid, Set),
            (nameof(HexbinSeries), _) => ax.Hexbin(x, y, Set),
            (nameof(Histogram2DSeries), _) => ax.Histogram2D(x, y, 3, Set),
            (nameof(ImageSeries), _) => ax.Image(grid, Set),
            (nameof(PcolormeshSeries), _) => ax.Pcolormesh(edges, edges, grid, Set),
            (nameof(SpectrogramSeries), _) => ax.Spectrogram(y, 1, Set),
            (nameof(TricontourSeries), _) => ax.Tricontour(x, y, low, Set),
            (nameof(TripcolorSeries), _) => ax.Tripcolor(x, y, low, Set),
            (nameof(ClustermapSeries), _) => ax.Clustermap(grid, Set),
            (nameof(DendrogramSeries), _) => ax.Dendrogram(tree, Set),
            (nameof(SunburstSeries), _) => ax.Sunburst(tree, Set),
            (nameof(TreemapSeries), _) => ax.Treemap(tree, Set),
            (nameof(PolarHeatmapSeries), _) => ax.PolarHeatmap(grid, 3, 3, Set),
            (nameof(Scatter3DSeries), _) => ax.Scatter3D(x, y, low, Set),
            (nameof(SurfaceSeries), _) => ax.Surface(x, x, grid, Set),
            (nameof(Trisurf3DSeries), _) => ax.Trisurf(x, y, low, Set),
            (nameof(Bar3DSeries), _) => ax.AddSeries(new Bar3DSeries(x, y, low), Set),
            (nameof(BarbsSeries), _) => ax.AddSeries(new BarbsSeries(x, y, new double[] { 10.0, 20.0, 30.0 }, new double[] { 45.0, 90.0, 135.0 }), Set),
            (nameof(BrokenBarSeries), _) => ax.AddSeries(new BrokenBarSeries([[new(1.0, 2.0), new(4.0, 1.0)], [new(2.0, 1.0)], [new(0.0, 3.0)]]), Set),
            (nameof(BubbleSeries), _) => ax.AddSeries(new BubbleSeries(x, y, [10.0, 20.0, 30.0]), Set),
            (nameof(BulletGraphSeries), _) => ax.AddSeries(new BulletGraphSeries(50), Set),
            (nameof(CandlestickSeries), _) => ax.AddSeries(new CandlestickSeries([10.0, 11, 12], [15.0, 16, 17], [8.0, 9, 10], [13.0, 12, 14]), Set),
            (nameof(Contour3DSeries), _) => ax.AddSeries(new Contour3DSeries(x, x, grid), Set),
            (nameof(ContourSeries), _) => ax.AddSeries(new ContourSeries(x, x, grid), Set),
            (nameof(CountSeries), _) => ax.AddSeries(new CountSeries(["a", "b", "a"]), Set),
            (nameof(DonutSeries), _) => ax.AddSeries(new DonutSeries([30.0, 50.0, 20.0]), Set),
            (nameof(EcdfSeries), _) => ax.AddSeries(new EcdfSeries(y), Set),
            (nameof(EventplotSeries), _) => ax.AddSeries(new EventplotSeries([[1.0, 2.0], [3.0], [4.0, 5.0]]), Set),
            (nameof(FunnelSeries), _) => ax.AddSeries(new FunnelSeries(categories, [30.0, 20.0, 10.0]), Set),
            (nameof(GanttSeries), _) => ax.AddSeries(new GanttSeries(categories, [0.0, 1.0, 2.0], [1.0, 3.0, 4.0]), Set),
            (nameof(GaugeSeries), _) => ax.AddSeries(new GaugeSeries(50), Set),
            (nameof(HistogramSeries), _) => ax.AddSeries(new HistogramSeries(y), Set),
            (nameof(Line3DSeries), _) => ax.AddSeries(new Line3DSeries(x, y, low), Set),
            (nameof(NetworkGraphSeries), _) => ax.AddSeries(new NetworkGraphSeries([new GraphNode("a"), new GraphNode("b")], [new GraphEdge("a", "b")]), Set),
            (nameof(OhlcBarSeries), _) => ax.AddSeries(new OhlcBarSeries([10.0, 11, 12], [15.0, 16, 17], [8.0, 9, 10], [13.0, 12, 14]), Set),
            (nameof(PairGridSeries), _) => ax.AddSeries(new PairGridSeries(sets), Set),
            (nameof(PieSeries), _) => ax.AddSeries(new PieSeries([30.0, 50.0, 20.0]), Set),
            (nameof(PointplotSeries), _) => ax.AddSeries(new PointplotSeries(sets), Set),
            (nameof(PolarBarSeries), _) => ax.AddSeries(new PolarBarSeries([5.0, 10.0, 7.0], [0.0, 1.0, 2.0]), Set),
            (nameof(PolarLineSeries), _) => ax.AddSeries(new PolarLineSeries([1.0, 2.0, 3.0], [0.0, 1.0, 2.0]), Set),
            (nameof(PolarScatterSeries), _) => ax.AddSeries(new PolarScatterSeries([1.0, 2.0, 3.0], [0.0, 1.0, 2.0]), Set),
            (nameof(ProgressBarSeries), _) => ax.AddSeries(new ProgressBarSeries(0.5), Set),
            (nameof(Quiver3DSeries), _) => ax.AddSeries(new Quiver3DSeries(x, y, low, low, low, low), Set),
            (nameof(QuiverKeySeries), _) => ax.AddSeries(new QuiverKeySeries(0.5, 0.9, 1.0, "1 m/s"), Set),
            (nameof(QuiverSeries), _) => ax.AddSeries(new QuiverSeries(x, y, low, low), Set),
            (nameof(RadarSeries), _) => ax.AddSeries(new RadarSeries(categories, y), Set),
            (nameof(RegressionSeries), _) => ax.AddSeries(new RegressionSeries(x, y), Set),
            (nameof(RelativeRotationSeries), _) => ax.AddSeries(new RelativeRotationSeries(sets, x, categories), Set),
            (nameof(ResidualSeries), _) => ax.AddSeries(new ResidualSeries(x, y), Set),
            (nameof(RugplotSeries), _) => ax.AddSeries(new RugplotSeries(y), Set),
            (nameof(SankeySeries), _) => ax.AddSeries(new SankeySeries([new SankeyNode("A"), new SankeyNode("B")], [new SankeyLink(0, 1, 10)]), Set),
            (nameof(SignalSeries), _) => ax.AddSeries(new SignalSeries(y, 100.0), Set),
            (nameof(SignalXYSeries), _) => ax.AddSeries(new SignalXYSeries(x, y), Set),
            (nameof(SparklineSeries), _) => ax.AddSeries(new SparklineSeries(y), Set),
            (nameof(StatTileSeries), _) => ax.AddSeries(new StatTileSeries(42), Set),
            (nameof(StateTimelineSeries), _) => ax.AddSeries(new StateTimelineSeries([new StateSegment(0, 1, "Up", Colors.Tab10Green)]), Set),
            (nameof(Stem3DSeries), _) => ax.AddSeries(new Stem3DSeries(x, y, low), Set),
            (nameof(StreamplotSeries), _) => ax.AddSeries(new StreamplotSeries([0.0, 1.0], [0.0, 1.0], new double[,] { { 1, 0 }, { 0, 1 } }, new double[,] { { 0, 1 }, { 1, 0 } }), Set),
            (nameof(SwarmplotSeries), _) => ax.AddSeries(new SwarmplotSeries(sets), Set),
            (nameof(TableSeries), _) => ax.AddSeries(new TableSeries([["a", "b"], ["c", "d"]]), Set),
            (nameof(Text3DSeries), _) => ax.AddSeries(new Text3DSeries([new Text3DAnnotation(1, 2, 3, "A"), new Text3DAnnotation(4, 5, 6, "B")]), Set),
            (nameof(TreeGridSeries), _) => ax.AddSeries(new TreeGridSeries([new TreeGridRow("Root", ["1", "2"])]), Set),
            (nameof(VoxelSeries), _) => ax.AddSeries(new VoxelSeries(new bool[2, 2, 2] { { { true, false }, { false, true } }, { { false, true }, { true, false } } }), Set),
            (nameof(WireframeSeries), _) => ax.AddSeries(new WireframeSeries(x, x, grid), Set),
            _ => throw new ArgumentException($"'{typeName}' is not a series type under the contract.", nameof(typeName)),
        }).Build();
    }

    /// <summary>The one series a <see cref="Build"/> figure holds, wherever it sits.</summary>
    public static ISeries TheSeries(Figure figure, SeriesSeat seat = SeriesSeat.Primary)
    {
        var axes = figure.SubPlots[0];
        return seat switch
        {
            SeriesSeat.SecondaryY => axes.YAxes.Right[0].Series.Single(),
            SeriesSeat.SecondaryX => axes.XSecondarySeries.Single(),
            _ => axes.Series.Single(),
        };
    }

    /// <summary>The value of the property named <paramref name="property"/> on <paramref name="series"/>.</summary>
    public static object? ValueOf(ISeries series, string property) =>
        (series.GetType().GetProperty(property) ?? throw new ArgumentException($"{series.GetType().Name} has no property '{property}'.", nameof(property)))
            .GetValue(series);

    /// <summary>Whether two property values draw the same: arrays element by element, and a colour map, a tick locator,
    /// a tick formatter or a normaliser by what it does (the same name colouring the same values and drawing the same
    /// under, over and bad colours; the same type placing the same ticks, writing the same labels, mapping the same
    /// values), since what they draw is all a reader of the picture can see of them.</summary>
    public static bool SameValue(object? written, object? read) => (written, read) switch
    {
        (IColorMap a, IColorMap b) => a.Name == b.Name && ColoursOf(a).SequenceEqual(ColoursOf(b)),
        (PropCycler a, PropCycler b) => CycleOf(a).SequenceEqual(CycleOf(b)),
        (ITickLocator a, ITickLocator b) => a.GetType() == b.GetType() && TicksOf(a).SequenceEqual(TicksOf(b)),
        (ITickFormatter a, ITickFormatter b) => a.GetType() == b.GetType() && LabelsOf(a).SequenceEqual(LabelsOf(b)),
        (INormalizer a, INormalizer b) => a.GetType() == b.GetType() && NormalisedBy(a).SequenceEqual(NormalisedBy(b)),
        (string a, string b) => a == b,
        (IEnumerable a, IEnumerable b) => a.Cast<object?>().SequenceEqual(b.Cast<object?>()),
        _ => Equals(written, read),
    };

    /// <summary>A range a locator is asked for ticks over.</summary>
    private readonly record struct TickRange(double Min, double Max);

    /// <summary>A value a normaliser maps within the range it is given.</summary>
    private readonly record struct NormaliseCase(double Value, double Min, double Max);

    /// <summary>The ranges a locator is asked for ticks over: a plain one, one with a negative end, one over six decades
    /// and a stretch of dates.</summary>
    private static readonly TickRange[] TickRanges = [new(0, 10), new(-3.7, 42.1), new(0.001, 1000), new(45000, 45400)];

    /// <summary>The values a formatter is asked to write: negative, zero, fractions, a date and a million.</summary>
    private static readonly double[] LabelValues = [-1234.5, 0, 0.5, 1, 3, 42.25, 45000.5, 1e6];

    /// <summary>The values a normaliser is asked to map.</summary>
    private static readonly NormaliseCase[] NormaliseCases = [new(0.3, 0, 1), new(5, 0, 10), new(-2, -5, 5), new(7, 1, 100), new(50, 0, 100)];

    /// <summary>The values a colour map is asked to colour: both ends, values between its stops, values past either end
    /// and a missing value.</summary>
    private static readonly double[] ColourMapValues = [0, 0.1, 0.25, 1.0 / 3, 0.5, 0.7, 0.9, 1, -0.5, 1.5, double.NaN];

    /// <summary>Every colour <paramref name="map"/> draws for the test values, then its under, over and bad colours: what
    /// a reader of the picture can see of a colour map.</summary>
    public static IReadOnlyList<Color?> ColoursOf(IColorMap map) =>
        [.. ColourMapValues.Select(value => (Color?)map.GetColor(value)), map.GetUnderColor(), map.GetOverColor(), map.GetBadColor()];

    /// <summary>Every tick <paramref name="locator"/> places over the test ranges, one list.</summary>
    public static IReadOnlyList<double> TicksOf(ITickLocator locator) =>
        [.. TickRanges.SelectMany(range => locator.Locate(range.Min, range.Max))];

    /// <summary>Every set of properties <paramref name="cycler"/> hands out over one whole cycle, in order: what a reader
    /// of the picture can see of a cycler.</summary>
    public static IReadOnlyList<CycledProperties> CycleOf(PropCycler cycler) => [.. Enumerable.Range(0, cycler.Length).Select(i => cycler[i])];

    /// <summary>Every label <paramref name="formatter"/> writes for the test values.</summary>
    public static IReadOnlyList<string> LabelsOf(ITickFormatter formatter) => [.. LabelValues.Select(formatter.Format)];

    /// <summary>Every value <paramref name="normalizer"/> maps the test triples to.</summary>
    public static IReadOnlyList<double> NormalisedBy(INormalizer normalizer) =>
        [.. NormaliseCases.Select(c => normalizer.Normalize(c.Value, c.Min, c.Max))];

    /// <summary>A value as a test message shows it.</summary>
    public static string Show(object? value) => value switch
    {
        null => "null",
        IColorMap map => $"colour map '{map.Name}' [{string.Join(", ", ColoursOf(map).Select(c => c is { } colour ? $"{colour.ToHex()}{colour.A:X2}" : "none"))}]",
        PropCycler cycler => $"PropCycler [{string.Join(", ", CycleOf(cycler))}]",
        ITickLocator locator => $"{locator.GetType().Name} [{string.Join(", ", locator.Locate(0, 10).Select(t => Show(t)))}]",
        ITickFormatter formatter => $"{formatter.GetType().Name} [{string.Join(", ", LabelsOf(formatter))}]",
        INormalizer normalizer => $"{normalizer.GetType().Name} [{string.Join(", ", NormalisedBy(normalizer).Select(v => Show(v)))}]",
        string text => $"\"{text}\"",
        IEnumerable items => "[" + string.Join(", ", items.Cast<object?>().Select(Show)) + "]",
        IFormattable number => number.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "?",
    };

    /// <summary>Sets every drawn property of <paramref name="series"/> to a value that is not its default, a different
    /// value per property so that two fields swapped on the way through JSON cannot pass for each other.</summary>
    public static void SetEveryDrawnProperty(ISeries series)
    {
        var properties = DrawnProperties(series.GetType());
        for (int k = 0; k < properties.Count; k++)
        {
            var property = properties[k];
            property.SetValue(series, NotTheDefault(property, property.GetValue(series), k));
        }
    }

    /// <summary>A value for <paramref name="property"/> that is not <paramref name="current"/>, different for every
    /// <paramref name="k"/>. The one generator both drawn-property contracts (the series' and the axes') set their values
    /// with.</summary>
    /// <exception cref="InvalidOperationException">The property's type is one the generator does not know.</exception>
    public static object NotTheDefault(PropertyInfo property, object? current, int k)
    {
        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        if (type == typeof(double) && property.DeclaringType == typeof(Figure)
            && property.Name is nameof(Figure.Width) or nameof(Figure.Height) or nameof(Figure.Dpi))
        {
            // A figure's size and resolution are read as such: the spec reader refuses one below a pixel.
            return 300.0 + k;
        }

        if (type == typeof(double))
        {
            double value = 0.25 + k / 64.0;
            return current is double d && d == value ? value + 1 / 128.0 : value;
        }

        if (type == typeof(int))
        {
            return (current is int i ? i : 0) + 2 + k;
        }

        if (type == typeof(bool))
        {
            return current is not true;
        }

        if (type.IsEnum)
        {
            return Enum.GetValues(type).Cast<object>().Last(member => !member.Equals(current));
        }

        if (type == typeof(Color))
        {
            return Colour(k, 0);
        }

        if (type == typeof(Color[]))
        {
            return Enumerable.Range(0, Points).Select(i => Colour(k, i)).ToArray();
        }

        if (type == typeof(double[]))
        {
            return Enumerable.Range(0, Points).Select(i => k + i + 0.5).ToArray();
        }

        if (type == typeof(int[]))
        {
            // Indices into the Points vertices, a different turn of them per property.
            return Enumerable.Range(0, Points).Select(i => (i + k) % Points).ToArray();
        }

        if (type == typeof(HatchPattern[]))
        {
            return Enumerable.Range(0, Points).Select(i => (HatchPattern)(1 + (i + k) % 3)).ToArray();
        }

        if (type == typeof(string))
        {
            // A format string is read as one: an arbitrary text would be a format that writes nonsense.
            return property.Name switch
            {
                nameof(BarSeries.LabelFormat) => "F1",
                nameof(TickConfig.Format) => "F2",
                _ => $"text {k}",
            };
        }

        if (type == typeof(string[]))
        {
            return Enumerable.Range(0, Points).Select(i => $"layer {k}.{i}").ToArray();
        }

        if (type == typeof(IColorMap))
        {
            // A map built in code, which no other reader knows by a name: it travels as what it is made of. Its bad
            // colour is translucent, so a colour's alpha is held too.
            return new LinearColorMap($"map {k}", [Colour(k, 0), Colour(k, 1), Colour(k, 2)])
            {
                UnderColor = Colour(k, 3),
                OverColor = Colour(k, 4),
                BadColor = Colour(k, 5).WithAlpha(128),
            };
        }

        if (type == typeof(INormalizer))
        {
            return new PowerNormNormalizer(0.5 + k / 64.0);
        }

        if (type == typeof(ITickLocator))
        {
            return new MultipleLocator(0.5 + k / 64.0, 0.25);
        }

        if (type == typeof(ITickFormatter))
        {
            return new PercentFormatter(50 + k);
        }

        if (type == typeof(GaugeBand[]) || type == typeof(IReadOnlyList<GaugeBand>))
        {
            // Ascending thresholds with a colour each, the order a gauge and a bullet graph read their bands in.
            return Enumerable.Range(0, Points).Select(i => new GaugeBand(10 * (i + 1) + k, Colour(k, i))).ToArray();
        }

        if (type == typeof(IReadOnlyList<double>))
        {
            return Enumerable.Range(0, Points).Select(i => k + i + 0.5).ToArray();
        }

        if (type == typeof(OpsCondition))
        {
            var rest = current is OpsCondition condition ? condition : OpsCondition.Resting;
            return new OpsCondition(
                Enum.GetValues<OpsSeverity>().Last(severity => severity != rest.Severity),
                Enum.GetValues<OpsVisibility>().Last(visibility => visibility != rest.Visibility));
        }

        if (type == typeof(PropCycler))
        {
            // Arrays of different lengths, so the cycle is longer than any one of them (2 x 3 = 6).
            return new PropCyclerBuilder()
                .WithColors(Colour(k, 0), Colour(k, 1))
                .WithLineStyles(LineStyle.Dashed, LineStyle.Dotted, LineStyle.DashDot)
                .WithMarkerStyles(MarkerStyle.Square)
                .WithLineWidths(1 + k / 64.0)
                .Build();
        }

        throw new InvalidOperationException(
            $"The fixture cannot set {property.DeclaringType?.Name}.{property.Name} of type {property.PropertyType.Name}: teach "
            + $"{nameof(NotTheDefault)} that type, or name the property in {nameof(NotCarried)} with the reason no JSON can carry it.");
    }

    private static Color Colour(int k, int i) => new((byte)(20 + 7 * k), (byte)(200 - 5 * k - 30 * i), (byte)(60 + 3 * k + 40 * i));
}
