// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>What a series holds that the drawn-property contract does not set comes back from JSON: a live series' capacity and the
/// points it holds now, and the fields of a tree grid's rows and of a Sankey's nodes and links. Every settable property of every kind
/// is held once, over the one roster, by <see cref="SeriesDrawnPropertiesRoundTripTests"/>, and the data a kind is made from by the
/// GivenSeries pins.</summary>
/// <remarks>Tagged into the <c>ChartSerializerGlobalState</c> collection (see
/// <see cref="ChartSerializerGlobalStateCollection"/>): this class round-trips through the
/// process-global <c>SeriesRegistry</c> and must not run concurrently with
/// <see cref="SeriesRegistryTests"/>, which temporarily mutates the registry table.</remarks>
[Collection("ChartSerializerGlobalState")]
public class AllSeriesRoundTripTests
{
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
            }))
            .Build();

        var restored = original.RoundTripped();
        var line = Assert.IsType<StreamingLineSeries>(Assert.Single(restored.SubPlots[0].Series));
        Assert.Equal(50, line.Capacity);
        var snapshot = line.CreateSnapshot();
        Assert.Equal([1.0, 2.0], snapshot.XData);
        Assert.Equal([2.0, 4.0], snapshot.YData);
    }

    /// <summary>A streaming scatter series round-trips at the capacity and points it was written with.</summary>
    [Fact]
    public void StreamingScatterSeries_RoundTrips_CapacityAndCurrentSnapshot()
    {
        var original = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.StreamingScatter(capacity: 30, s =>
            {
                s.AppendPoint(5, 6);
            }))
            .Build();

        var restored = original.RoundTripped();
        var scatter = Assert.IsType<StreamingScatterSeries>(Assert.Single(restored.SubPlots[0].Series));
        Assert.Equal(30, scatter.Capacity);
        var snapshot = scatter.CreateSnapshot();
        Assert.Equal([5.0], snapshot.XData);
        Assert.Equal([6.0], snapshot.YData);
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
            }))
            .Build();

        var restored = original.RoundTripped();
        var candles = Assert.IsType<StreamingCandlestickSeries>(Assert.Single(restored.SubPlots[0].Series));
        Assert.Equal(20, candles.Capacity);
        var snapshot = candles.CreateOhlcSnapshot();
        Assert.Equal([10.0, 13.0], snapshot.Open);
        Assert.Equal([15.0, 18.0], snapshot.High);
        Assert.Equal([8.0, 11.0], snapshot.Low);
        Assert.Equal([13.0, 16.0], snapshot.Close);
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
            }))
            .Build();

        var restored = original.RoundTripped();
        var signal = Assert.IsType<StreamingSignalSeries>(Assert.Single(restored.SubPlots[0].Series));
        Assert.Equal(40, signal.Capacity);
        Assert.Equal(2.0, signal.SampleRate);
        var snapshot = signal.CreateSnapshot();
        Assert.Equal([7.0, 9.0], snapshot.YData);
    }

    /// <summary>A tree grid's rows round-trip — they did not before (the same "Every chart type back from JSON"
    /// gap as the treemap/sunburst/dendrogram/clustermap placeholders, just never counted among them since no
    /// comment named it).</summary>
    [Fact]
    public void TreeGridSeries_RoundTrips_ItsRows()
    {
        var rows = new List<TreeGridRow>
        {
            new("root", ["1", "2"]) { Depth = 0, Expanded = true },
            new("child", ["3", "4"]) { Depth = 1, Url = "https://example.test/child", Accent = Colors.Red },
        };
        var original = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.TreeGrid(rows))
            .Build();

        var restored = original.RoundTripped();
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
            .AddSubPlot(1, 1, 1, ax => ax.Sankey(nodes, links))
            .Build();

        var restored = original.RoundTripped();
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
    }
}
