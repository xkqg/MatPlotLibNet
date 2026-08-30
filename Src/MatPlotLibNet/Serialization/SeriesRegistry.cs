// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Serialization;

/// <summary>Registry mapping series type discriminators to factory functions for
/// deserialization.</summary>
/// <remarks>The factory table is **process-global** by design — every
/// <see cref="ChartSerializer"/> instance dispatches through the same dictionary so
/// downstream <c>FromJson</c> calls don't need a per-instance configuration step.
/// Because of this, registrations made by one test bleed into sibling tests sharing
/// the same process. Test code that registers a custom factory must call
/// <see cref="ResetForTestsInternal"/> (in-repo, via <c>InternalsVisibleTo</c>) or the
/// obsolete public <see cref="ResetForTests"/> shim (external consumers) in its tear-down
/// to roll the table back to the built-in defaults. Production callers that only consume
/// the built-ins are unaffected: idempotent re-registration of the same discriminator is a
/// safe last-writer-wins overwrite.</remarks>
public static class SeriesRegistry
{
    private static readonly ConcurrentDictionary<string, Func<Axes, SeriesDto, ISeries?>> Factories = new();

    /// <summary>Registers a factory for a series type discriminator.</summary>
    public static void Register(string typeDiscriminator, Func<Axes, SeriesDto, ISeries?> factory)
        => Factories[typeDiscriminator] = factory;

    /// <summary>Creates a series from a DTO using the registered factory.</summary>
    public static ISeries? Create(string typeDiscriminator, Axes axes, SeriesDto dto)
        => Factories.TryGetValue(typeDiscriminator, out var factory) ? factory(axes, dto) : null;

    /// <summary>Clears every registered factory and re-runs <see cref="RegisterDefaults"/>
    /// so the table returns to the exact set the static constructor builds. Intended
    /// for test infrastructure that registers a custom factory and needs to undo
    /// that mutation before the next test runs.</summary>
    /// <remarks>Public shim retained for one release for external consumers' test suites;
    /// in-repo tests must call <see cref="ResetForTestsInternal"/> directly (reachable via
    /// <c>InternalsVisibleTo</c>) so our own build stays warning-free.</remarks>
    [Obsolete("ResetForTests becomes internal in a future release; test projects should reference InternalsVisibleTo instead.")]
    public static void ResetForTests() => ResetForTestsInternal();

    /// <summary>Clears every registered factory and re-runs <see cref="RegisterDefaults"/>
    /// so the table returns to the exact set the static constructor builds. Internal entry
    /// point for in-repo test infrastructure (via <c>InternalsVisibleTo</c>); see
    /// <see cref="ResetForTests"/> for the obsolete public shim kept for external consumers.</summary>
    internal static void ResetForTestsInternal()
    {
        Factories.Clear();
        RegisterDefaults();
    }

    static SeriesRegistry() => RegisterDefaults();

    private static void RegisterDefaults()
    {
        Register("line", LineSeries.FromSeriesDto);
        Register("scatter", ScatterSeries.FromSeriesDto);
        Register("bar", BarSeries.FromSeriesDto);
        Register("histogram", HistogramSeries.FromSeriesDto);
        Register("pie", PieSeries.FromSeriesDto);
        Register("box", BoxSeries.FromSeriesDto);
        Register("violin", ViolinSeries.FromSeriesDto);
        Register("hexbin", HexbinSeries.FromSeriesDto);
        Register("regression", RegressionSeries.FromSeriesDto);
        Register("kde", KdeSeries.FromSeriesDto);
        Register("heatmap", HeatmapSeries.FromSeriesDto);
        Register("image", ImageSeries.FromSeriesDto);
        Register("histogram2d", Histogram2DSeries.FromSeriesDto);
        Register("stem", StemSeries.FromSeriesDto);
        Register("contour", ContourSeries.FromSeriesDto);
        Register("contourf", ContourfSeries.FromSeriesDto);
        Register("area", AreaSeries.FromSeriesDto);
        Register("step", StepSeries.FromSeriesDto);
        Register("ecdf", EcdfSeries.FromSeriesDto);
        Register("stackedarea", StackedAreaSeries.FromSeriesDto);
        Register("errorbar", ErrorBarSeries.FromSeriesDto);
        Register("candlestick", CandlestickSeries.FromSeriesDto);
        Register("quiver", QuiverSeries.FromSeriesDto);
        Register("streamplot", StreamplotSeries.FromSeriesDto);
        Register("radar", RadarSeries.FromSeriesDto);
        Register("donut", DonutSeries.FromSeriesDto);
        Register("bubble", BubbleSeries.FromSeriesDto);
        Register("ohlcbar", OhlcBarSeries.FromSeriesDto);
        Register("waterfall", WaterfallSeries.FromSeriesDto);
        Register("funnel", FunnelSeries.FromSeriesDto);
        Register("gantt", GanttSeries.FromSeriesDto);
        Register("gauge", GaugeSeries.FromSeriesDto);
        Register("progressbar", ProgressBarSeries.FromSeriesDto);
        Register("stattile", StatTileSeries.FromSeriesDto);
        Register("statetimeline", StateTimelineSeries.FromSeriesDto);
        Register("bulletgraph", BulletGraphSeries.FromSeriesDto);
        Register("sparkline", SparklineSeries.FromSeriesDto);
        Register("treemap", TreemapSeries.FromSeriesDto);
        Register("sunburst", SunburstSeries.FromSeriesDto);
        Register("dendrogram", DendrogramSeries.FromSeriesDto);
        Register("clustermap", ClustermapSeries.FromSeriesDto);
        Register("pairgrid", PairGridSeries.FromSeriesDto);
        Register("networkgraph", NetworkGraphSeries.FromSeriesDto);
        Register("relativerotation", RelativeRotationSeries.FromSeriesDto);
        Register("sankey", SankeySeries.FromSeriesDto);
        Register("polarline", PolarLineSeries.FromSeriesDto);
        Register("polarscatter", PolarScatterSeries.FromSeriesDto);
        Register("polarbar", PolarBarSeries.FromSeriesDto);
        Register("surface", SurfaceSeries.FromSeriesDto);
        Register("wireframe", WireframeSeries.FromSeriesDto);
        Register("scatter3d", Scatter3DSeries.FromSeriesDto);

        // v0.8.0
        Register("rugplot", RugplotSeries.FromSeriesDto);
        Register("stripplot", StripplotSeries.FromSeriesDto);
        Register("eventplot", EventplotSeries.FromSeriesDto);
        Register("brokenbar", BrokenBarSeries.FromSeriesDto);
        Register("count", CountSeries.FromSeriesDto);
        Register("pcolormesh", PcolormeshSeries.FromSeriesDto);
        Register("residual", ResidualSeries.FromSeriesDto);

        // v0.8.0 Phase B
        Register("pointplot", PointplotSeries.FromSeriesDto);
        Register("swarmplot", SwarmplotSeries.FromSeriesDto);
        Register("spectrogram", SpectrogramSeries.FromSeriesDto);
        Register("table", TableSeries.FromSeriesDto);
        Register("treegrid", TreeGridSeries.FromSeriesDto);

        // v0.8.0 Phase C
        Register("tricontour", TricontourSeries.FromSeriesDto);
        Register("tripcolor", TripcolorSeries.FromSeriesDto);
        Register("quiverkey", QuiverKeySeries.FromSeriesDto);
        Register("barbs", BarbsSeries.FromSeriesDto);

        // v0.8.0 Phase D
        Register("stem3d", Stem3DSeries.FromSeriesDto);
        Register("bar3d", Bar3DSeries.FromSeriesDto);

        // v1.0 Signal series
        Register("signal-xy", SignalXYSeries.FromSeriesDto);
        Register("signal", SignalSeries.FromSeriesDto);

        // v1.1.1 PolarHeatmapSeries
        Register("polarheatmap", PolarHeatmapSeries.FromSeriesDto);

        // v1.3.0 ThreeD series
        Register("line3d", Line3DSeries.FromSeriesDto);
        Register("trisurf", Trisurf3DSeries.FromSeriesDto);
        Register("contour3d", Contour3DSeries.FromSeriesDto);
        Register("quiver3d", Quiver3DSeries.FromSeriesDto);
        Register("voxels", VoxelSeries.FromSeriesDto);
        Register("text3d", Text3DSeries.FromSeriesDto);

    }
}
