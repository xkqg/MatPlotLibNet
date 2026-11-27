// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Serialization;

/// <summary>Registry mapping series type discriminators to factory functions for
/// deserialization.</summary>
/// <remarks>The factory table is **process-global** by design — every
/// <see cref="ChartSerializer"/> instance dispatches through the same table so
/// downstream <c>FromJson</c> calls don't need a per-instance configuration step.
/// Because of this, registrations made by one test bleed into sibling tests sharing
/// the same process. A test of this repository that registers a custom factory calls
/// an internal reset (via <c>InternalsVisibleTo</c>) in its tear-down
/// to roll the table back to the built-in defaults. Production callers that only consume
/// the built-ins are unaffected: idempotent re-registration of the same discriminator is a
/// safe last-writer-wins overwrite.</remarks>
public static class SeriesRegistry
{
    private static readonly NamedTable<string, Func<Axes, SeriesDto, SeriesRead>> Factories = new();

    /// <summary>Registers a factory for a series type discriminator.</summary>
    /// <param name="typeDiscriminator">The discriminator the DTO carries.</param>
    /// <param name="factory">The reader that makes the series from the axes and the DTO; <see langword="null"/> from it means
    /// no series was made.</param>
    public static void Register(string typeDiscriminator, Func<Axes, SeriesDto, ISeries?> factory)
        => Factories.Set(typeDiscriminator, (axes, dto) => SeriesRead.Of(factory(axes, dto)));

    /// <summary>Registers the reader of a kind of series of the library or of one of its packages under the name the kind carries
    /// (<see cref="ISeriesKind{TSelf}.Kind"/>), so the name is written once, in the class that is the kind.</summary>
    /// <typeparam name="TSeries">The series class the reader makes; it holds the name.</typeparam>
    /// <param name="reader">The series class's own <c>FromSeriesDto</c>, the one place its arguments and result are said: it takes the
    /// axes the series is added to and the record that carries the series' persisted properties, adds the series it makes to the axes and
    /// returns it; <see langword="null"/> from it means no series was made.</param>
    internal static void Register<TSeries>(Func<Axes, SeriesDto, TSeries?> reader) where TSeries : class, ISeries, ISeriesKind<TSeries> =>
        RegisterReader<TSeries>((axes, dto) => SeriesRead.Of(reader(axes, dto)));

    /// <summary>Registers a reader that says why it made no series of the DTO it cannot read, under the name the kind carries
    /// (<see cref="ISeriesKind{TSelf}.Kind"/>).</summary>
    /// <typeparam name="TSeries">The series class the reader makes; it holds the name.</typeparam>
    /// <param name="reader">The reader: the series made on the axes, or why the data is unreadable.</param>
    internal static void RegisterReader<TSeries>(Func<Axes, SeriesDto, SeriesRead> reader) where TSeries : ISeriesKind<TSeries> =>
        RegisterReader(TSeries.Kind, reader);

    /// <summary>Registers a reader that says why it made no series of a DTO it cannot read (a map whose projection this build
    /// does not know), for a kind of the library or of one of its packages.</summary>
    /// <param name="typeDiscriminator">The discriminator the kind is written under.</param>
    /// <param name="reader">The reader: the series made on the axes, or why the data is unreadable.</param>
    internal static void RegisterReader(string typeDiscriminator, Func<Axes, SeriesDto, SeriesRead> reader)
        => Factories.Set(typeDiscriminator, reader);

    /// <summary>The discriminators the registry can dispatch, sorted ordinally. A consumer that must SHOW the
    /// chart types (the MCP server's <c>list_chart_types</c>) reads this instead of keeping a second list that
    /// drifts from <see cref="Create"/>. Sorted on purpose: a hash collection has no order to lean on.</summary>
    public static IReadOnlyList<string> Discriminators => Factories.Keys.Order(StringComparer.Ordinal).ToArray();

    /// <summary>Builds the series a DTO describes, through the factory registered under its discriminator.</summary>
    /// <param name="typeDiscriminator">The discriminator the series type was registered under, such as <c>"line"</c>.</param>
    /// <param name="axes">The axes the new series is built for.</param>
    /// <param name="dto">The serialised series to read.</param>
    /// <returns>The new series, or <see langword="null"/> when no factory is registered under the discriminator or the factory returns none.</returns>
    public static ISeries? Create(string typeDiscriminator, Axes axes, SeriesDto dto)
        => Read(typeDiscriminator, axes, dto)?.Series;

    /// <summary>What the reader registered under <paramref name="typeDiscriminator"/> made of <paramref name="dto"/>: the series, or
    /// why its data is unreadable.</summary>
    /// <param name="typeDiscriminator">The discriminator the DTO carries.</param>
    /// <param name="axes">The axes the series is added to.</param>
    /// <param name="dto">The record of the series.</param>
    /// <returns><see langword="null"/> when no reader is registered under the discriminator.</returns>
    internal static SeriesRead? Read(string typeDiscriminator, Axes axes, SeriesDto dto)
        => Factories.Get(typeDiscriminator) is { } reader ? reader(axes, dto) : null;

    /// <summary>Clears every registered factory and re-runs <see cref="RegisterDefaults"/>
    /// so the table returns to the exact set the static constructor builds. Internal entry
    /// point for in-repo test infrastructure (via <c>InternalsVisibleTo</c>).</summary>
    internal static void ResetForTestsInternal()
    {
        Factories.Clear();
        RegisterDefaults();
    }

    static SeriesRegistry() => RegisterDefaults();

    private static void RegisterDefaults()
    {
        Register(LineSeries.FromSeriesDto);
        Register(ScatterSeries.FromSeriesDto);
        Register(BarSeries.FromSeriesDto);
        Register(HistogramSeries.FromSeriesDto);
        Register(PieSeries.FromSeriesDto);
        Register(BoxSeries.FromSeriesDto);
        Register(ViolinSeries.FromSeriesDto);
        Register(HexbinSeries.FromSeriesDto);
        Register(RegressionSeries.FromSeriesDto);
        Register(KdeSeries.FromSeriesDto);
        Register(HeatmapSeries.FromSeriesDto);
        Register(ImageSeries.FromSeriesDto);
        Register(Histogram2DSeries.FromSeriesDto);
        Register(StemSeries.FromSeriesDto);
        Register(ContourSeries.FromSeriesDto);
        Register(ContourfSeries.FromSeriesDto);
        Register(AreaSeries.FromSeriesDto);
        Register(StepSeries.FromSeriesDto);
        Register(EcdfSeries.FromSeriesDto);
        Register(StackedAreaSeries.FromSeriesDto);
        Register(ErrorBarSeries.FromSeriesDto);
        Register(CandlestickSeries.FromSeriesDto);
        Register(QuiverSeries.FromSeriesDto);
        Register(StreamplotSeries.FromSeriesDto);
        Register(RadarSeries.FromSeriesDto);
        Register(DonutSeries.FromSeriesDto);
        Register(BubbleSeries.FromSeriesDto);
        Register(OhlcBarSeries.FromSeriesDto);
        Register(WaterfallSeries.FromSeriesDto);
        Register(FunnelSeries.FromSeriesDto);
        Register(GanttSeries.FromSeriesDto);
        Register(GaugeSeries.FromSeriesDto);
        Register(ProgressBarSeries.FromSeriesDto);
        Register(StatTileSeries.FromSeriesDto);
        Register(StateTimelineSeries.FromSeriesDto);
        Register(BulletGraphSeries.FromSeriesDto);
        Register(SparklineSeries.FromSeriesDto);
        Register(TreemapSeries.FromSeriesDto);
        Register(SunburstSeries.FromSeriesDto);
        Register(DendrogramSeries.FromSeriesDto);
        Register(ClustermapSeries.FromSeriesDto);
        Register(PairGridSeries.FromSeriesDto);
        Register(NetworkGraphSeries.FromSeriesDto);
        Register(RelativeRotationSeries.FromSeriesDto);
        Register(SankeySeries.FromSeriesDto);
        Register(ChordSeries.FromSeriesDto);
        Register(WordCloudSeries.FromSeriesDto);
        Register(PolarLineSeries.FromSeriesDto);
        Register(PolarScatterSeries.FromSeriesDto);
        Register(PolarBarSeries.FromSeriesDto);
        Register(TernaryScatterSeries.FromSeriesDto);
        Register(TernaryLineSeries.FromSeriesDto);
        Register(PiperSeries.FromSeriesDto);
        Register(SurfaceSeries.FromSeriesDto);
        Register(WireframeSeries.FromSeriesDto);
        Register(Scatter3DSeries.FromSeriesDto);
        Register(PlanarBar3DSeries.FromSeriesDto);

        // Streaming series (JSON round-trip): each kind writes and reads its own discriminator now — the shared
        // base class' "streaming" (StreamingSeries.ToSeriesDto) is no longer written by any concrete series, so
        // nothing registers it.
        Register(Models.Series.Streaming.StreamingLineSeries.FromSeriesDto);
        Register(Models.Series.Streaming.StreamingScatterSeries.FromSeriesDto);
        Register(Models.Series.Streaming.StreamingCandlestickSeries.FromSeriesDto);
        Register(Models.Series.Streaming.StreamingSignalSeries.FromSeriesDto);

        // v0.8.0
        Register(RugplotSeries.FromSeriesDto);
        Register(StripplotSeries.FromSeriesDto);
        Register(EventplotSeries.FromSeriesDto);
        Register(BrokenBarSeries.FromSeriesDto);
        Register(CountSeries.FromSeriesDto);
        Register(PcolormeshSeries.FromSeriesDto);
        Register(ResidualSeries.FromSeriesDto);

        // v0.8.0 Phase B
        Register(PointplotSeries.FromSeriesDto);
        Register(SwarmplotSeries.FromSeriesDto);
        Register(SpectrogramSeries.FromSeriesDto);
        Register(TableSeries.FromSeriesDto);
        Register(TreeGridSeries.FromSeriesDto);
        Register(WaffleSeries.FromSeriesDto);
        Register(PointAndFigureSeries.FromSeriesDto);
        Register(IcicleSeries.FromSeriesDto);

        // v0.8.0 Phase C
        Register(TricontourSeries.FromSeriesDto);
        Register(TripcolorSeries.FromSeriesDto);
        Register(QuiverKeySeries.FromSeriesDto);
        Register(BarbsSeries.FromSeriesDto);

        // v0.8.0 Phase D
        Register(Stem3DSeries.FromSeriesDto);
        Register(Bar3DSeries.FromSeriesDto);

        // v1.0 Signal series
        Register(SignalXYSeries.FromSeriesDto);
        Register(SignalSeries.FromSeriesDto);

        // v1.1.1 PolarHeatmapSeries
        Register(PolarHeatmapSeries.FromSeriesDto);

        // v1.3.0 ThreeD series
        Register(Line3DSeries.FromSeriesDto);
        Register(Trisurf3DSeries.FromSeriesDto);
        Register(Contour3DSeries.FromSeriesDto);
        Register(Quiver3DSeries.FromSeriesDto);
        Register(VoxelSeries.FromSeriesDto);
        Register(Text3DSeries.FromSeriesDto);

    }
}
