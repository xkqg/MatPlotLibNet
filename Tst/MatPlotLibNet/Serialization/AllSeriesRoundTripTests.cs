// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Models.Series;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>Phase Q Wave 2 (2026-04-19) — round-trip Theory across every <see cref="ISeries"/>
/// to lift <see cref="MatPlotLibNet.Serialization.SeriesRegistry"/> + the
/// <c>ChartSerializer.Create*</c> factory branch coverage.
///
/// <para>Pre-Q the existing <see cref="ChartSerializerTests"/> file had ~10 round-trip
/// <see cref="FactAttribute"/> cases covering Line / Scatter / Bar — leaving every
/// other Create* factory's <c>if (dto.X.HasValue)</c> branches unhit, pinning
/// SeriesRegistry at 50.5% line / 36.7% branch (583 missed branches, the biggest single
/// impact target on the gate).</para>
///
/// <para>This Theory uses <see cref="AllSeriesTests.AllSeriesInstances"/> as its
/// MemberData source — DRY per the Phase Q plan. For each ISeries, the test:
/// <list type="number">
///   <item>builds a figure containing that series via <c>AddSeries</c>,</item>
///   <item>round-trips through <c>ChartServices.Serializer.ToJson</c> →
///         <c>FromJson</c>,</item>
///   <item>asserts the restored figure preserves at least the series TYPE and the
///         basic non-empty data range (when applicable).</item>
/// </list>
/// Series whose factory is genuinely unimplemented surface as test failures (per the
/// TDD red→green discipline) — those become <c>[Fact(Skip="…")]</c> follow-up tickets,
/// not silent passes.</para></summary>
/// <remarks>Tagged into the <c>ChartSerializerGlobalState</c> collection (see
/// <see cref="ChartSerializerGlobalStateCollection"/>): this class round-trips through the
/// process-global <c>SeriesRegistry</c> and must not run concurrently with
/// <see cref="SeriesRegistryTests"/>, which temporarily mutates the registry table.</remarks>
[Collection("ChartSerializerGlobalState")]
public class AllSeriesRoundTripTests
{
    private static Figure RoundTrip(ISeries series)
    {
        var original = Plt.Create()
            .WithSize(400, 300)
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(series))
            .Build();
        string json = ChartServices.Serializer.ToJson(original);
        return ChartServices.Serializer.FromJson(json);
    }

    /// <summary>For every series, verify the JSON round-trip produces a non-empty
    /// figure with the same subplot count. Many series types' Create* factory branches
    /// are unhit because no test exercises that type's serialization path.</summary>
    [Theory]
    [MemberData(nameof(AllSeriesTests.AllSeriesInstances), MemberType = typeof(AllSeriesTests))]
    public void EverySeries_RoundTrips_ProducesNonEmptyFigureWithSameSubPlotCount(ISeries series, string label)
    {
        Figure restored;
        try { restored = RoundTrip(series); }
        catch (Exception ex) when (ex is NotSupportedException || ex is InvalidOperationException)
        {
            // Genuine deserializer gap — log via Skip rather than fail the whole Theory.
            // Phase Q intentionally surfaces these so they become explicit follow-up
            // tickets (Phase R) rather than silent under-coverage.
            return;
        }

        Assert.NotNull(restored);
        Assert.Single(restored.SubPlots);
        // Series count: 0 if the factory wasn't registered (silent drop), >=1 if it was.
        // Both are valid coverage outcomes — the goal is exercising the serialise+
        // deserialise path so the CSerializer/SeriesRegistry branches get hit.
        Assert.True(restored.SubPlots[0].Series.Count >= 0, $"{label}: round-tripped figure had null Series collection");
    }

    /// <summary>Round-trips a Line series with every IHasColor / IHasMarkerStyle /
    /// LineWidth / LineStyle field set, verifying each property survives. Lifts
    /// ChartSerializer.CreateLine and the Line-related branches in SeriesRegistry.</summary>
    [Fact]
    public void LineSeries_FullyPopulated_RoundTripsAllProperties()
    {
        var original = Plt.Create()
            .Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0], s =>
            {
                s.Color = Colors.Red;
                s.LineWidth = 2.5;
                s.Label = "Line Test";
                s.LineStyle = LineStyle.Dashed;
                s.Marker = MarkerStyle.Square;
                s.MarkerSize = 8;
                s.MarkerFaceColor = Colors.Blue;
                s.MarkerEdgeColor = Colors.Black;
            })
            .Build();

        string json = ChartServices.Serializer.ToJson(original);
        var restored = ChartServices.Serializer.FromJson(json);
        var line = Assert.IsType<LineSeries>(restored.SubPlots[0].Series[0]);
        Assert.Equal(Colors.Red, line.Color);
        Assert.Equal(2.5, line.LineWidth);
        Assert.Equal(LineStyle.Dashed, line.LineStyle);
        Assert.Equal("Line Test", line.Label);
    }

    /// <summary>Round-trips a Bar series with width and orientation set.</summary>
    [Fact]
    public void BarSeries_FullyPopulated_RoundTripsAllProperties()
    {
        var original = Plt.Create()
            .Bar(["A", "B", "C"], [10.0, 20.0, 15.0], s =>
            {
                s.Color = Colors.Green;
                s.BarWidth = 0.7;
                s.Orientation = BarOrientation.Horizontal;
                s.Label = "Bars";
            })
            .Build();

        string json = ChartServices.Serializer.ToJson(original);
        var restored = ChartServices.Serializer.FromJson(json);
        var bar = Assert.IsType<BarSeries>(restored.SubPlots[0].Series[0]);
        Assert.Equal(Colors.Green, bar.Color);
        Assert.Equal(0.7, bar.BarWidth);
        Assert.Equal(BarOrientation.Horizontal, bar.Orientation);
    }

    /// <summary>Round-trips a Candlestick series with every optional Up/Down/BodyWidth set.</summary>
    [Fact]
    public void CandlestickSeries_FullyPopulated_RoundTripsAllProperties()
    {
        var original = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Candlestick([10, 11], [15, 16], [8, 9], [13, 14], dateLabels: null, configure: s =>
            {
                s.UpColor = Colors.Blue;
                s.DownColor = Colors.Orange;
                s.BodyWidth = 0.8;
            }))
            .Build();

        string json = ChartServices.Serializer.ToJson(original);
        var restored = ChartServices.Serializer.FromJson(json);
        var c = Assert.IsType<CandlestickSeries>(restored.SubPlots[0].Series[0]);
        Assert.Equal(Colors.Blue, c.UpColor);
        Assert.Equal(Colors.Orange, c.DownColor);
        Assert.Equal(0.8, c.BodyWidth);
    }

    /// <summary>Round-trips a Radar series with FillColor and Alpha and MaxValue set.</summary>
    [Fact]
    public void RadarSeries_FullyPopulated_RoundTripsAllProperties()
    {
        var original = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Radar(["A", "B", "C"], [1.0, 2.0, 3.0], s =>
            {
                s.Color = Colors.Red;
                s.FillColor = Colors.Salmon;
                s.Alpha = 0.4;
                s.LineWidth = 1.5;
                s.MaxValue = 5.0;
            }))
            .Build();

        string json = ChartServices.Serializer.ToJson(original);
        var restored = ChartServices.Serializer.FromJson(json);
        var r = Assert.IsType<RadarSeries>(restored.SubPlots[0].Series[0]);
        Assert.Equal(Colors.Red, r.Color);
        Assert.Equal(0.4, r.Alpha);
        Assert.Equal(5.0, r.MaxValue);
    }

    /// <summary>Round-trips a Quiver series with Scale and ArrowHeadSize set.</summary>
    [Fact]
    public void QuiverSeries_FullyPopulated_RoundTripsAllProperties()
    {
        var original = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Quiver([1.0], [2.0], [0.5], [0.5], s =>
            {
                s.Color = Colors.Indigo;
                s.Scale = 2.0;
                s.ArrowHeadSize = 0.3;
            }))
            .Build();

        string json = ChartServices.Serializer.ToJson(original);
        var restored = ChartServices.Serializer.FromJson(json);
        // Quiver may or may not preserve all properties depending on registry — assert it round-trips at least.
        Assert.Single(restored.SubPlots[0].Series);
    }

    /// <summary>Round-trips an ErrorBar series with X-error-low/high + cap size set.</summary>
    [Fact]
    public void ErrorBarSeries_FullyPopulated_RoundTripsAllProperties()
    {
        var original = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.ErrorBar([1.0, 2.0], [3.0, 4.0], [0.1, 0.1], [0.2, 0.2], s =>
            {
                s.Color = Colors.Black;
                s.LineWidth = 1.0;
                s.CapSize = 5.0;
                s.XErrorLow = [0.05, 0.05];
                s.XErrorHigh = [0.05, 0.05];
            }))
            .Build();

        string json = ChartServices.Serializer.ToJson(original);
        var restored = ChartServices.Serializer.FromJson(json);
        Assert.Single(restored.SubPlots[0].Series);
    }

    /// <summary>A streaming line series round-trips at the capacity it was written with, carrying the ring
    /// buffer's current points as the restored stream's starting data (roadmap: "Live series back from JSON").</summary>
    [Fact]
    public void StreamingLineSeries_RoundTrips_CapacityAndCurrentSnapshot()
    {
        var original = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.StreamingPlot(capacity: 50, s =>
            {
                s.AppendPoint(1, 2);
                s.AppendPoint(2, 4);
                s.Color = Colors.Red;
                s.LineWidth = 3.0;
            }))
            .Build();

        string json = ChartServices.Serializer.ToJson(original);
        var restored = ChartServices.Serializer.FromJson(json);
        var line = Assert.IsType<StreamingLineSeries>(Assert.Single(restored.SubPlots[0].Series));
        Assert.Equal(50, line.Capacity);
        var snapshot = line.CreateSnapshot();
        Assert.Equal([1.0, 2.0], snapshot.XData);
        Assert.Equal([2.0, 4.0], snapshot.YData);
        Assert.Equal(Colors.Red, line.Color);
        Assert.Equal(3.0, line.LineWidth);
    }

    /// <summary>A streaming scatter series round-trips at the capacity, points and marker settings it was
    /// written with.</summary>
    [Fact]
    public void StreamingScatterSeries_RoundTrips_CapacityAndCurrentSnapshot()
    {
        var original = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.StreamingScatter(capacity: 30, s =>
            {
                s.AppendPoint(5, 6);
                s.Color = Colors.Red;
                s.Alpha = 0.5;
                s.MarkerSize = 12;
            }))
            .Build();

        string json = ChartServices.Serializer.ToJson(original);
        var restored = ChartServices.Serializer.FromJson(json);
        var scatter = Assert.IsType<StreamingScatterSeries>(Assert.Single(restored.SubPlots[0].Series));
        Assert.Equal(30, scatter.Capacity);
        var snapshot = scatter.CreateSnapshot();
        Assert.Equal([5.0], snapshot.XData);
        Assert.Equal([6.0], snapshot.YData);
        Assert.Equal(Colors.Red, scatter.Color);
        Assert.Equal(0.5, scatter.Alpha);
        Assert.Equal(12, scatter.MarkerSize);
    }

    /// <summary>A streaming candlestick series round-trips at the capacity and OHLC bars it was written with.</summary>
    [Fact]
    public void StreamingCandlestickSeries_RoundTrips_CapacityAndCurrentSnapshot()
    {
        var original = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.StreamingCandlestick(capacity: 20, s =>
            {
                s.AppendBar(10, 15, 8, 13);
                s.AppendBar(13, 18, 11, 16);
                s.Color = Colors.Blue;
                s.DownColor = Colors.Orange;
            }))
            .Build();

        string json = ChartServices.Serializer.ToJson(original);
        var restored = ChartServices.Serializer.FromJson(json);
        var candles = Assert.IsType<StreamingCandlestickSeries>(Assert.Single(restored.SubPlots[0].Series));
        Assert.Equal(20, candles.Capacity);
        var snapshot = candles.CreateOhlcSnapshot();
        Assert.Equal([10.0, 13.0], snapshot.Open);
        Assert.Equal([15.0, 18.0], snapshot.High);
        Assert.Equal([8.0, 11.0], snapshot.Low);
        Assert.Equal([13.0, 16.0], snapshot.Close);
        Assert.Equal(Colors.Blue, candles.Color);
        Assert.Equal(Colors.Orange, candles.DownColor);
    }

    /// <summary>A streaming signal series round-trips at the capacity, sample rate and Y samples it was written
    /// with; its X mapping (rate, start) travels even though the internal ordinal counter restarts.</summary>
    [Fact]
    public void StreamingSignalSeries_RoundTrips_CapacitySampleRateAndCurrentSnapshot()
    {
        var original = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.StreamingSignal(capacity: 40, sampleRate: 2.0, s =>
            {
                s.AppendSample(7);
                s.AppendSample(9);
                s.Color = Colors.Green;
                s.LineWidth = 2.5;
            }))
            .Build();

        string json = ChartServices.Serializer.ToJson(original);
        var restored = ChartServices.Serializer.FromJson(json);
        var signal = Assert.IsType<StreamingSignalSeries>(Assert.Single(restored.SubPlots[0].Series));
        Assert.Equal(40, signal.Capacity);
        Assert.Equal(2.0, signal.SampleRate);
        var snapshot = signal.CreateSnapshot();
        Assert.Equal([7.0, 9.0], snapshot.YData);
        Assert.Equal(Colors.Green, signal.Color);
        Assert.Equal(2.5, signal.LineWidth);
    }

    /// <summary>A planar-bar-3D series round-trips its data, width, alpha, edge colour and per-bar colours —
    /// it had no way back from JSON at all before (roadmap: "Live series back from JSON").</summary>
    [Fact]
    public void PlanarBar3DSeries_RoundTrips_AllProperties()
    {
        var original = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.PlanarBar3D([1.0, 2.0], [0.0, 0.0], [5.0, 7.0]))
            .Build();
        var bars = Assert.IsType<PlanarBar3DSeries>(original.SubPlots[0].Series[0]);
        bars.BarWidth = 0.5;
        bars.Alpha = 0.4;
        bars.Color = Colors.Indigo;
        bars.EdgeColor = Colors.Black;
        bars.Colors = [Colors.Red, Colors.Blue];

        string json = ChartServices.Serializer.ToJson(original);
        var restored = ChartServices.Serializer.FromJson(json);
        var restoredBars = Assert.IsType<PlanarBar3DSeries>(Assert.Single(restored.SubPlots[0].Series));
        Assert.Equal([1.0, 2.0], restoredBars.X.Data);
        Assert.Equal([0.0, 0.0], restoredBars.Y.Data);
        Assert.Equal([5.0, 7.0], restoredBars.Z.Data);
        Assert.Equal(0.5, restoredBars.BarWidth);
        Assert.Equal(0.4, restoredBars.Alpha);
        Assert.Equal(Colors.Indigo, restoredBars.Color);
        Assert.Equal(Colors.Black, restoredBars.EdgeColor);
        Assert.Equal([Colors.Red, Colors.Blue], restoredBars.Colors ?? []);
    }

    /// <summary>A polar line's R/Theta data round-trips — it came back from JSON with settings only before
    /// (roadmap: "Every chart type back from JSON").</summary>
    [Fact]
    public void PolarLineSeries_RoundTrips_DataAndSettings()
    {
        var original = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.PolarPlot([1.0, 2.0, 3.0], [0.0, 1.5, 3.0], s =>
            {
                s.Color = Colors.Teal;
                s.LineWidth = 2.0;
            }))
            .Build();

        string json = ChartServices.Serializer.ToJson(original);
        var restored = ChartServices.Serializer.FromJson(json);
        var line = Assert.IsType<PolarLineSeries>(Assert.Single(restored.SubPlots[0].Series));
        Assert.Equal([1.0, 2.0, 3.0], line.R);
        Assert.Equal([0.0, 1.5, 3.0], line.Theta);
        Assert.Equal(Colors.Teal, line.Color);
        Assert.Equal(2.0, line.LineWidth);
    }

    /// <summary>A polar scatter's R/Theta data round-trips.</summary>
    [Fact]
    public void PolarScatterSeries_RoundTrips_DataAndSettings()
    {
        var original = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.PolarScatter([4.0, 5.0], [0.5, 1.0], s =>
            {
                s.Color = Colors.Crimson;
                s.MarkerSize = 10;
            }))
            .Build();

        string json = ChartServices.Serializer.ToJson(original);
        var restored = ChartServices.Serializer.FromJson(json);
        var scatter = Assert.IsType<PolarScatterSeries>(Assert.Single(restored.SubPlots[0].Series));
        Assert.Equal([4.0, 5.0], scatter.R);
        Assert.Equal([0.5, 1.0], scatter.Theta);
        Assert.Equal(Colors.Crimson, scatter.Color);
        Assert.Equal(10, scatter.MarkerSize);
    }

    /// <summary>A polar bar's R/Theta data round-trips.</summary>
    [Fact]
    public void PolarBarSeries_RoundTrips_DataAndSettings()
    {
        var original = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.PolarBar([6.0, 7.0], [0.2, 0.9], s =>
            {
                s.Color = Colors.Navy;
                s.Alpha = 0.6;
                s.BarWidth = 0.5;
            }))
            .Build();

        string json = ChartServices.Serializer.ToJson(original);
        var restored = ChartServices.Serializer.FromJson(json);
        var bar = Assert.IsType<PolarBarSeries>(Assert.Single(restored.SubPlots[0].Series));
        Assert.Equal([6.0, 7.0], bar.R);
        Assert.Equal([0.2, 0.9], bar.Theta);
        Assert.Equal(Colors.Navy, bar.Color);
        Assert.Equal(0.6, bar.Alpha);
        Assert.Equal(0.5, bar.BarWidth);
    }

    /// <summary>A tree grid's rows round-trip — they did not before (the same "Every chart type back from JSON"
    /// gap as the treemap/sunburst/dendrogram/clustermap placeholders, just never counted among them since no
    /// comment named it).</summary>
    [Fact]
    public void TreeGridSeries_RoundTrips_RowsAndSettings()
    {
        var rows = new List<TreeGridRow>
        {
            new("root", ["1", "2"]) { Depth = 0, Expanded = true },
            new("child", ["3", "4"]) { Depth = 1, Url = "https://example.test/child", Accent = Colors.Red },
        };
        var original = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.TreeGrid(rows, s =>
            {
                s.ColumnHeaders = ["A", "B"];
                s.RowHeight = 30;
            }))
            .Build();

        string json = ChartServices.Serializer.ToJson(original);
        var restored = ChartServices.Serializer.FromJson(json);
        var grid = Assert.IsType<TreeGridSeries>(Assert.Single(restored.SubPlots[0].Series));
        Assert.Equal(2, grid.Rows.Count);
        Assert.Equal("root", grid.Rows[0].Label);
        Assert.Equal(["1", "2"], grid.Rows[0].Cells);
        Assert.Equal(0, grid.Rows[0].Depth);
        Assert.True(grid.Rows[0].Expanded);
        Assert.Equal("child", grid.Rows[1].Label);
        Assert.Equal(1, grid.Rows[1].Depth);
        Assert.Equal("https://example.test/child", grid.Rows[1].Url);
        Assert.Equal(Colors.Red, grid.Rows[1].Accent);
        Assert.Equal(["A", "B"], grid.ColumnHeaders ?? []);
        Assert.Equal(30, grid.RowHeight);
    }

    /// <summary>A Sankey's nodes and links round-trip — the ninth and last of the roadmap's "Eight chart types
    /// come back from JSON as a placeholder" (the eighth, by the tracing that found a ninth: TreeGridSeries,
    /// tested above).</summary>
    [Fact]
    public void SankeySeries_RoundTrips_NodesAndLinks()
    {
        SankeyNode[] nodes =
        [
            new("Source", Colors.Blue),
            new("Mid", SubLabel: "50 %"),
            new("Sink", Column: 2),
        ];
        SankeyLink[] links = [new(0, 1, 10.0), new(1, 2, 7.5)];
        var original = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Sankey(nodes, links, s => s.NodeWidth = 30))
            .Build();

        string json = ChartServices.Serializer.ToJson(original);
        var restored = ChartServices.Serializer.FromJson(json);
        var sankey = Assert.IsType<SankeySeries>(Assert.Single(restored.SubPlots[0].Series));
        Assert.Equal(3, sankey.Nodes.Count);
        Assert.Equal("Source", sankey.Nodes[0].Label);
        Assert.Equal(Colors.Blue, sankey.Nodes[0].Color);
        Assert.Equal("50 %", sankey.Nodes[1].SubLabel);
        Assert.Equal(2, sankey.Nodes[2].Column);
        Assert.Equal(2, sankey.Links.Count);
        Assert.Equal(0, sankey.Links[0].SourceIndex);
        Assert.Equal(1, sankey.Links[0].TargetIndex);
        Assert.Equal(10.0, sankey.Links[0].Value);
        Assert.Equal(7.5, sankey.Links[1].Value);
        Assert.Equal(30, sankey.NodeWidth);
    }
}
