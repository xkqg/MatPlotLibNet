// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>v1.11.0 — JSON round-trip coverage for <see cref="RelativeRotationSeries"/>.</summary>
public class RelativeRotationSerializationTests
{
    private static readonly double[][] SampleAssets = [
        [100.0, 101.0, 102.0, 103.0, 104.0],
        [100.0,  99.0,  98.0,  97.0,  96.0],
    ];
    private static readonly double[] SampleBench = [100.0, 100.0, 100.0, 100.0, 100.0];
    private static readonly string[] SampleLabels = ["ETH", "BNB"];

    private static RelativeRotationSeries Roundtrip(Action<RelativeRotationSeries>? configure = null)
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.RelativeRotation(SampleAssets, SampleBench, SampleLabels, configure))
            .Build();
        var json     = new ChartSerializer().ToJson(figure);
        var restored = new ChartSerializer().FromJson(json);
        return restored.SubPlots[0].Series.OfType<RelativeRotationSeries>().First();
    }

    // ── Type tag ──────────────────────────────────────────────────────────────

    [Fact]
    public void RoundTrip_TypeTagIsRelativeRotation()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.RelativeRotation(SampleAssets, SampleBench, SampleLabels))
            .Build();
        var json = new ChartSerializer().ToJson(figure);
        Assert.Contains("\"type\":\"relativerotation\"", json);
    }

    // ── Defaults not emitted ──────────────────────────────────────────────────

    [Fact]
    public void RoundTrip_DefaultProperties_NotEmittedToJson()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.RelativeRotation(SampleAssets, SampleBench, SampleLabels))
            .Build();
        var json = new ChartSerializer().ToJson(figure);
        Assert.DoesNotContain("\"rrgFormula\"",          json);
        Assert.DoesNotContain("\"rrgShortPeriod\"",      json);
        Assert.DoesNotContain("\"rrgLongPeriod\"",       json);
        Assert.DoesNotContain("\"rrgMomentumLookback\"", json);
        Assert.DoesNotContain("\"rrgTailLength\"",       json);
        Assert.DoesNotContain("\"rrgShowQuadrantGrid\"", json);
        Assert.DoesNotContain("\"colorMapName\"",        json);
    }

    // ── Asset data ────────────────────────────────────────────────────────────

    [Fact]
    public void RoundTrip_PreservesAssetAndBenchmarkData()
    {
        var s = Roundtrip();
        Assert.Equal(2, s.AssetCloses.Count);
        Assert.Equal(5, s.BenchmarkCloses.Count);
        Assert.Equal(100.0, s.AssetCloses[0][0]);
        Assert.Equal(96.0,  s.AssetCloses[1][4]);
        Assert.Equal(100.0, s.BenchmarkCloses[0]);
    }

    [Fact]
    public void RoundTrip_PreservesAssetLabels()
    {
        var s = Roundtrip();
        Assert.Equal(2, s.AssetLabels.Count);
        Assert.Equal("ETH", s.AssetLabels[0]);
        Assert.Equal("BNB", s.AssetLabels[1]);
    }

    // ── Property round-trips ──────────────────────────────────────────────────

    [Fact]
    public void RoundTrip_PreservesFormula_ZScore()
    {
        var s = Roundtrip(s => s.Formula = RrgFormula.ZScore);
        Assert.Equal(RrgFormula.ZScore, s.Formula);
    }

    [Fact]
    public void RoundTrip_PreservesFormula_LogReturn()
    {
        var s = Roundtrip(s => s.Formula = RrgFormula.LogReturn);
        Assert.Equal(RrgFormula.LogReturn, s.Formula);
    }

    [Fact]
    public void RoundTrip_PreservesShortPeriod()
    {
        var s = Roundtrip(s => s.ShortPeriod = 5);
        Assert.Equal(5, s.ShortPeriod);
    }

    [Fact]
    public void RoundTrip_PreservesLongPeriod()
    {
        var s = Roundtrip(s => s.LongPeriod = 52);
        Assert.Equal(52, s.LongPeriod);
    }

    [Fact]
    public void RoundTrip_PreservesMomentumLookback()
    {
        var s = Roundtrip(s => s.MomentumLookback = 20);
        Assert.Equal(20, s.MomentumLookback);
    }

    [Fact]
    public void RoundTrip_PreservesTailLength()
    {
        var s = Roundtrip(s => s.TailLength = 12);
        Assert.Equal(12, s.TailLength);
    }

    [Fact]
    public void RoundTrip_PreservesShowQuadrantGrid_False()
    {
        var s = Roundtrip(s => s.ShowQuadrantGrid = false);
        Assert.False(s.ShowQuadrantGrid);
    }

    [Fact]
    public void RoundTrip_PreservesColorMap()
    {
        var s = Roundtrip(s => s.ColorMap = ColorMaps.Plasma);
        Assert.NotNull(s.ColorMap);
        Assert.Equal("plasma", s.ColorMap!.Name);
    }

    // ── Overlay arrays round-trip ─────────────────────────────────────────────

    [Fact]
    public void RoundTrip_PreservesAbsorptionRatioPerBar()
    {
        double[] absorption = [0.3, 0.45, 0.6, 0.55, 0.4];
        var s = Roundtrip(s => s.AbsorptionRatioPerBar = absorption);
        Assert.NotNull(s.AbsorptionRatioPerBar);
        Assert.Equal(5, s.AbsorptionRatioPerBar!.Length);
        Assert.Equal(0.3,  s.AbsorptionRatioPerBar[0], precision: 10);
        Assert.Equal(0.45, s.AbsorptionRatioPerBar[1], precision: 10);
    }

    [Fact]
    public void RoundTrip_PreservesEnbPerBar()
    {
        double[] enb = [4.2, 3.8, 3.1, 2.5, 1.9];
        var s = Roundtrip(s => s.EnbPerBar = enb);
        Assert.NotNull(s.EnbPerBar);
        Assert.Equal(5, s.EnbPerBar!.Length);
        Assert.Equal(4.2, s.EnbPerBar[0], precision: 10);
        Assert.Equal(1.9, s.EnbPerBar[4], precision: 10);
    }

    [Fact]
    public void RoundTrip_NullOverlays_NotEmittedToJson()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.RelativeRotation(SampleAssets, SampleBench, SampleLabels))
            .Build();
        var json = new ChartSerializer().ToJson(figure);
        Assert.DoesNotContain("\"rrgAbsorptionPerBar\"", json);
        Assert.DoesNotContain("\"rrgEnbPerBar\"",        json);
    }

    // ── Empty assets round-trip ───────────────────────────────────────────────

    [Fact]
    public void RoundTrip_EmptyAssets_PreservesShape()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.RelativeRotation([], [], []))
            .Build();
        var json     = new ChartSerializer().ToJson(figure);
        var restored = new ChartSerializer().FromJson(json);
        var s = restored.SubPlots[0].Series.OfType<RelativeRotationSeries>().First();
        Assert.Empty(s.AssetCloses);
    }

    // ── Null-field fallback branches ──────────────────────────────────────────

    [Fact]
    public void FromJson_MissingAssetFields_FallsBackToEmptyCollections()
    {
        // JSON with type=relativerotation but no rrgAssetCloses / rrgBenchmarkCloses /
        // rrgAssetLabels → exercises the ?? [] null branches in CreateRelativeRotation.
        const string json = """{"subPlots":[{"series":[{"type":"relativerotation"}]}]}""";
        var figure = new ChartSerializer().FromJson(json);
        var s = figure.SubPlots[0].Series.OfType<RelativeRotationSeries>().First();
        Assert.Empty(s.AssetCloses);
        Assert.Empty(s.BenchmarkCloses);
        Assert.Empty(s.AssetLabels);
    }
}
