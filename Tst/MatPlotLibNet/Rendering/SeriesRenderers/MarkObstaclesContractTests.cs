// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Models.Series;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>
/// Every series type the library ships stands in exactly one list: it draws marks a legend keeps clear of (Cartesian, polar or
/// 3-D), or it fills its axes or is a panel of its own and has none. A new series type is in neither list and turns this red,
/// which is the moment to decide which it is. A type that draws marks has a row in <see cref="MarksAsDrawnTests"/> that proves
/// its renderer collects them; a type that has none collects nothing.
/// </summary>
public class MarkObstaclesContractTests
{
    /// <summary>The series that draw marks on Cartesian axes: paths (lines and outlines), points and boxes.</summary>
    private static readonly Type[] Cartesian =
    [
        // paths
        typeof(LineSeries), typeof(StepSeries), typeof(EcdfSeries), typeof(KdeSeries), typeof(RegressionSeries), typeof(SparklineSeries),
        typeof(SignalSeries), typeof(SignalXYSeries), typeof(StreamingLineSeries), typeof(StreamingSignalSeries), typeof(ErrorBarSeries),
        typeof(StemSeries), typeof(AreaSeries), typeof(StackedAreaSeries), typeof(BoxSeries), typeof(ViolinSeries), typeof(OhlcBarSeries),
        typeof(PointplotSeries), typeof(RugplotSeries), typeof(EventplotSeries), typeof(DendrogramSeries), typeof(NetworkGraphSeries),
        typeof(RelativeRotationSeries), typeof(RadarSeries), typeof(PieSeries), typeof(DonutSeries), typeof(SunburstSeries),
        // points
        typeof(ScatterSeries), typeof(BubbleSeries), typeof(StreamingScatterSeries), typeof(StripplotSeries), typeof(SwarmplotSeries),
        typeof(ResidualSeries),
        // boxes
        typeof(BarSeries), typeof(HistogramSeries), typeof(CountSeries), typeof(WaterfallSeries), typeof(BrokenBarSeries),
        typeof(GanttSeries), typeof(FunnelSeries), typeof(CandlestickSeries), typeof(StreamingCandlestickSeries),
    ];

    /// <summary>The series that draw marks on polar and 3-D axes.</summary>
    private static readonly Type[] PolarAndThreeD =
    [
        typeof(PolarLineSeries), typeof(PolarScatterSeries), typeof(PolarBarSeries),
        typeof(Line3DSeries), typeof(Stem3DSeries), typeof(Scatter3DSeries), typeof(Bar3DSeries), typeof(PlanarBar3DSeries), typeof(Text3DSeries),
    ];

    /// <summary>The series that fill their axes (a field of cells, an image, a surface), so every place a legend could stand
    /// is under them alike, and matplotlib counts no image; and the panels and tiles that fill their whole axes.</summary>
    private static readonly Type[] Dropped =
    [
        typeof(HeatmapSeries), typeof(ImageSeries), typeof(Histogram2DSeries), typeof(HexbinSeries), typeof(ContourSeries),
        typeof(ContourfSeries), typeof(TricontourSeries), typeof(TripcolorSeries), typeof(PcolormeshSeries), typeof(SpectrogramSeries),
        typeof(QuiverSeries), typeof(StreamplotSeries), typeof(BarbsSeries), typeof(PolarHeatmapSeries), typeof(SurfaceSeries),
        typeof(WireframeSeries), typeof(Trisurf3DSeries), typeof(Contour3DSeries), typeof(Quiver3DSeries), typeof(VoxelSeries),
        typeof(ClustermapSeries), typeof(StateTimelineSeries),
        typeof(TreemapSeries), typeof(TableSeries), typeof(TreeGridSeries), typeof(PairGridSeries), typeof(SankeySeries),
        typeof(StatTileSeries), typeof(GaugeSeries), typeof(ProgressBarSeries), typeof(BulletGraphSeries), typeof(QuiverKeySeries),
    ];

    private static IEnumerable<Type> Shipped() =>
        typeof(ISeries).Assembly.GetExportedTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(ISeries).IsAssignableFrom(t));

    [Fact]
    public void EverySeriesType_StandsInExactlyOneList()
    {
        var lists = new[] { Cartesian, PolarAndThreeD, Dropped };
        var unplaced = Shipped().Where(t => lists.All(list => !list.Contains(t))).Select(t => t.Name).Order(StringComparer.Ordinal).ToList();
        var twice = Shipped().Where(t => lists.Count(list => list.Contains(t)) > 1).Select(t => t.Name).Order(StringComparer.Ordinal).ToList();
        var unknown = lists.SelectMany(list => list).Where(t => !Shipped().Contains(t)).Select(t => t.Name).ToList();

        Assert.True(unplaced.Count == 0, $"{unplaced.Count} series type(s) are in no list: {string.Join(", ", unplaced)}. Decide whether each draws marks a legend keeps clear of.");
        Assert.True(twice.Count == 0, $"in more than one list: {string.Join(", ", twice)}");
        Assert.True(unknown.Count == 0, $"listed but not shipped: {string.Join(", ", unknown)}");
    }

    [Fact]
    public void EverySeriesTypeThatDrawsMarks_HasARowInTheMarksAsDrawnTests()
    {
        var covered = MarksAsDrawnTests.SeriesTypes.ToHashSet();

        var missing = Cartesian.Concat(PolarAndThreeD).Where(t => !covered.Contains(t)).Select(t => t.Name).ToList();

        Assert.True(missing.Count == 0, $"no marks-as-drawn row for: {string.Join(", ", missing)}");
    }

    public static TheoryData<ISeries, string> DroppedInstances =>
        [.. ((IEnumerable<ITheoryDataRow>)AllSeriesTests.AllSeriesInstances)
            .Select(row => row.GetData())
            .Where(data => Dropped.Contains(data[0]!.GetType()))
            .Select(data => (data[0]!, (string)data[1]!))
            .Select(pair => new TheoryDataRow<ISeries, string>((ISeries)pair.Item1, pair.Item2))];

    [Theory]
    [MemberData(nameof(DroppedInstances))]
    public void ASeriesThatFillsItsAxes_CollectsNoMarks(ISeries series, string name)
    {
        var ctx = new RecordingRenderContext();
        var builder = new MarkObstacles.Builder();
        var collector = new SvgSeriesRenderer(new DataTransform(0, 10, 0, 10, new Rect(0, 0, 400, 300)), ctx, Colors.Blue, Theme.Default,
            plotArea: new Rect(0, 0, 400, 300)) { Marks = builder };

        series.Accept(collector, new RenderArea(new Rect(0, 0, 400, 300), ctx));
        var marks = builder.Build();

        Assert.True(marks.Boxes.Count + marks.Paths.Count + marks.Points.Count == 0, $"{name} collected a mark");
        Assert.Empty(ctx.Calls);
    }
}
