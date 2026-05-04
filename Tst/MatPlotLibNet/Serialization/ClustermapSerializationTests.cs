// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>v1.10 — verifies JSON round-trip for <see cref="ClustermapSeries"/>.
/// The trees are not serialised; the registry rebuilds the series with a placeholder
/// <c>new double[1,1]</c> data matrix, mirroring the treemap / sunburst pattern.</summary>
public class ClustermapSerializationTests
{
    private static double[,] SampleData => new double[,]
    {
        { 1.0, 2.0 },
        { 3.0, 4.0 },
    };

    private static ClustermapSeries Roundtrip(Action<ClustermapSeries>? configure = null)
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Clustermap(SampleData, configure))
            .Build();
        var json = new ChartSerializer().ToJson(figure);
        var restored = new ChartSerializer().FromJson(json);
        return restored.SubPlots[0].Series.OfType<ClustermapSeries>().First();
    }

    // ── Type tag ──────────────────────────────────────────────────────────────

    [Fact]
    public void RoundTrip_TypeTagIsClustermap()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Clustermap(SampleData))
            .Build();
        var json = new ChartSerializer().ToJson(figure);
        Assert.Contains("\"type\":\"clustermap\"", json);
    }

    // ── Default properties: not emitted to JSON ───────────────────────────────

    [Fact]
    public void RoundTrip_DefaultProperties_NotEmittedToJson()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Clustermap(SampleData))
            .Build();
        var json = new ChartSerializer().ToJson(figure);
        Assert.DoesNotContain("\"colorMapName\"", json);
        Assert.DoesNotContain("\"showLabels\"", json);
        Assert.DoesNotContain("\"labelFormat\"", json);
        Assert.DoesNotContain("\"rowDendrogramWidth\"", json);
        Assert.DoesNotContain("\"columnDendrogramHeight\"", json);
    }

    // ── ColorMap round-trip ───────────────────────────────────────────────────

    [Fact]
    public void RoundTrip_PreservesColorMap()
    {
        var s = Roundtrip(s => s.ColorMap = ColorMaps.Inferno);
        Assert.NotNull(s.ColorMap);
        Assert.Equal("inferno", s.ColorMap!.Name);
    }

    // ── ShowLabels round-trip ─────────────────────────────────────────────────

    [Fact]
    public void RoundTrip_PreservesShowLabels_True()
    {
        var s = Roundtrip(s => s.ShowLabels = true);
        Assert.True(s.ShowLabels);
    }

    // ── LabelFormat round-trip ────────────────────────────────────────────────

    [Fact]
    public void RoundTrip_PreservesLabelFormat()
    {
        var s = Roundtrip(s => s.LabelFormat = "P1");
        Assert.Equal("P1", s.LabelFormat);
    }

    // ── Panel ratios round-trip ───────────────────────────────────────────────

    [Fact]
    public void RoundTrip_PreservesRowDendrogramWidth()
    {
        var s = Roundtrip(s => s.RowDendrogramWidth = 0.25);
        Assert.Equal(0.25, s.RowDendrogramWidth, precision: 10);
    }

    [Fact]
    public void RoundTrip_PreservesColumnDendrogramHeight()
    {
        var s = Roundtrip(s => s.ColumnDendrogramHeight = 0.20);
        Assert.Equal(0.20, s.ColumnDendrogramHeight, precision: 10);
    }

    // ── Normalizer (not serialised — project-wide design choice) ─────────────

    [Fact]
    public void RoundTrip_Normalizer_IsNullAfterRoundTrip()
    {
        // Normalizer is intentionally not emitted to the DTO — the same design choice
        // used by HeatmapSeries and all other INormalizable series in this library.
        // This test documents the expected behaviour so a future accidental serialisation
        // attempt is caught immediately.
        var s = Roundtrip(s => s.Normalizer = MatPlotLibNet.Styling.ColorMaps.LinearNormalizer.Instance);
        Assert.Null(s.Normalizer);
    }

    // ── Combined non-defaults ─────────────────────────────────────────────────

    [Fact]
    public void RoundTrip_AllNonDefaults_Combined()
    {
        var s = Roundtrip(s =>
        {
            s.ColorMap = ColorMaps.Plasma;
            s.ShowLabels = true;
            s.LabelFormat = "F1";
            s.RowDendrogramWidth = 0.30;
            s.ColumnDendrogramHeight = 0.25;
        });
        Assert.Equal("plasma", s.ColorMap!.Name);
        Assert.True(s.ShowLabels);
        Assert.Equal("F1", s.LabelFormat);
        Assert.Equal(0.30, s.RowDendrogramWidth, precision: 10);
        Assert.Equal(0.25, s.ColumnDendrogramHeight, precision: 10);
    }
}
