// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>v1.10 Phase 4 — verifies JSON round-trip for <see cref="PairGridSeries"/>.
/// <see cref="PairGridSeries.HuePalette"/> is intentionally not serialised; same project
/// convention as <c>HeatmapSeries.Normalizer</c> and <c>ClustermapSeries.RowTree</c>.</summary>
public class PairGridSerializationTests
{
    private static double[][] SampleVars => new[]
    {
        new[] { 1.0, 2.0, 3.0, 4.0 },
        new[] { 0.5, 1.5, 2.5, 3.5 },
        new[] { 9.0, 8.0, 7.0, 6.0 },
    };

    private static PairGridSeries Roundtrip(Action<PairGridSeries>? configure = null)
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.PairGrid(SampleVars, configure))
            .Build();
        var json = new ChartSerializer().ToJson(figure);
        var restored = new ChartSerializer().FromJson(json);
        return restored.SubPlots[0].Series.OfType<PairGridSeries>().First();
    }

    // ── Type tag ──────────────────────────────────────────────────────────────

    [Fact]
    public void RoundTrip_TypeTagIsPairGrid()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.PairGrid(SampleVars))
            .Build();
        var json = new ChartSerializer().ToJson(figure);
        Assert.Contains("\"type\":\"pairgrid\"", json);
    }

    // ── Default properties: not emitted to JSON ───────────────────────────────

    [Fact]
    public void RoundTrip_DefaultProperties_NotEmittedToJson()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.PairGrid(SampleVars))
            .Build();
        var json = new ChartSerializer().ToJson(figure);
        Assert.DoesNotContain("\"pairGridLabels\"", json);
        Assert.DoesNotContain("\"pairGridHueGroups\"", json);
        Assert.DoesNotContain("\"pairGridHueLabels\"", json);
        Assert.DoesNotContain("\"pairGridDiagonal\"", json);
        Assert.DoesNotContain("\"pairGridOffDiagonal\"", json);
        Assert.DoesNotContain("\"pairGridTriangular\"", json);
        Assert.DoesNotContain("\"pairGridDiagonalBins\"", json);
        Assert.DoesNotContain("\"pairGridMarkerSize\"", json);
        Assert.DoesNotContain("\"pairGridCellSpacing\"", json);
    }

    // ── Variables round-trip ──────────────────────────────────────────────────

    [Fact]
    public void RoundTrip_PreservesVariableShape()
    {
        var s = Roundtrip();
        Assert.Equal(SampleVars.Length, s.Variables.Length);
        for (int i = 0; i < SampleVars.Length; i++)
            Assert.Equal(SampleVars[i].Length, s.Variables[i].Length);
    }

    [Fact]
    public void RoundTrip_PreservesVariableValues()
    {
        var s = Roundtrip();
        for (int i = 0; i < SampleVars.Length; i++)
        for (int k = 0; k < SampleVars[i].Length; k++)
            Assert.Equal(SampleVars[i][k], s.Variables[i][k], precision: 10);
    }

    // ── DiagonalKind round-trip ───────────────────────────────────────────────

    [Fact]
    public void RoundTrip_PreservesDiagonalKind_Kde()
    {
        var s = Roundtrip(s => s.DiagonalKind = PairGridDiagonalKind.Kde);
        Assert.Equal(PairGridDiagonalKind.Kde, s.DiagonalKind);
    }

    // ── OffDiagonalKind round-trip ────────────────────────────────────────────

    [Fact]
    public void RoundTrip_PreservesOffDiagonalKind_None()
    {
        var s = Roundtrip(s => s.OffDiagonalKind = PairGridOffDiagonalKind.None);
        Assert.Equal(PairGridOffDiagonalKind.None, s.OffDiagonalKind);
    }

    // ── Triangular round-trip ─────────────────────────────────────────────────

    [Fact]
    public void RoundTrip_PreservesTriangular_LowerOnly()
    {
        var s = Roundtrip(s => s.Triangular = PairGridTriangle.LowerOnly);
        Assert.Equal(PairGridTriangle.LowerOnly, s.Triangular);
    }

    [Fact]
    public void RoundTrip_PreservesTriangular_UpperOnly()
    {
        var s = Roundtrip(s => s.Triangular = PairGridTriangle.UpperOnly);
        Assert.Equal(PairGridTriangle.UpperOnly, s.Triangular);
    }

    // ── Numeric properties round-trip ─────────────────────────────────────────

    [Fact]
    public void RoundTrip_PreservesDiagonalBins()
    {
        var s = Roundtrip(s => s.DiagonalBins = 50);
        Assert.Equal(50, s.DiagonalBins);
    }

    [Fact]
    public void RoundTrip_PreservesMarkerSize()
    {
        var s = Roundtrip(s => s.MarkerSize = 6.5);
        Assert.Equal(6.5, s.MarkerSize, precision: 10);
    }

    [Fact]
    public void RoundTrip_PreservesCellSpacing()
    {
        var s = Roundtrip(s => s.CellSpacing = 0.07);
        Assert.Equal(0.07, s.CellSpacing, precision: 10);
    }

    // ── Labels / Hue round-trip ───────────────────────────────────────────────

    [Fact]
    public void RoundTrip_PreservesLabels()
    {
        var s = Roundtrip(s => s.Labels = ["A", "B", "C"]);
        Assert.NotNull(s.Labels);
        Assert.Equal(["A", "B", "C"], s.Labels);
    }

    [Fact]
    public void RoundTrip_PreservesHueGroups()
    {
        var s = Roundtrip(s => s.HueGroups = [0, 1, 0, 1]);
        Assert.NotNull(s.HueGroups);
        Assert.Equal([0, 1, 0, 1], s.HueGroups);
    }

    [Fact]
    public void RoundTrip_PreservesHueLabels()
    {
        var s = Roundtrip(s => s.HueLabels = ["Setosa", "Versicolor"]);
        Assert.NotNull(s.HueLabels);
        Assert.Equal(["Setosa", "Versicolor"], s.HueLabels);
    }

    // ── HuePalette intentionally NOT serialised ───────────────────────────────

    [Fact]
    public void RoundTrip_HuePaletteIsNullAfterRoundTrip()
    {
        var s = Roundtrip(s => s.HuePalette = [new Color(255, 0, 0), new Color(0, 255, 0)]);
        Assert.Null(s.HuePalette);
    }

    // ── Empty-Variables DTO must throw (matches model invariant) ──────────────

    [Fact]
    public void Deserialize_EmptyVariablesDto_Throws()
    {
        // Hand-crafted JSON with a "pairgrid" type but null/missing variables must
        // be rejected — the constructor invariant requires ≥1 variable.
        var axes = new MatPlotLibNet.Models.Axes();
        var dto = new SeriesDto { Type = "pairgrid", Variables = null };
        var ex = Assert.Throws<InvalidOperationException>(
            () => MatPlotLibNet.Serialization.SeriesRegistry.Create("pairgrid", axes, dto));
        Assert.Contains("Variables", ex.Message);
    }

    // ── DiagonalKind.None round-trips ─────────────────────────────────────────

    [Fact]
    public void RoundTrip_PreservesDiagonalKind_None()
    {
        var s = Roundtrip(s => s.DiagonalKind = PairGridDiagonalKind.None);
        Assert.Equal(PairGridDiagonalKind.None, s.DiagonalKind);
    }

    // ── v1.10 — Hexbin round-trip ─────────────────────────────────────────────

    [Fact]
    public void RoundTrip_PreservesOffDiagonalKind_Hexbin()
    {
        var s = Roundtrip(s => s.OffDiagonalKind = PairGridOffDiagonalKind.Hexbin);
        Assert.Equal(PairGridOffDiagonalKind.Hexbin, s.OffDiagonalKind);
    }

    [Fact]
    public void RoundTrip_PreservesHexbinGridSize()
    {
        var s = Roundtrip(s => s.HexbinGridSize = 25);
        Assert.Equal(25, s.HexbinGridSize);
    }

    [Fact]
    public void RoundTrip_PreservesOffDiagonalColorMap()
    {
        var s = Roundtrip(s => s.OffDiagonalColorMap = MatPlotLibNet.Styling.ColorMaps.ColorMaps.Plasma);
        Assert.NotNull(s.OffDiagonalColorMap);
        Assert.Equal("plasma", s.OffDiagonalColorMap!.Name);
    }

    // ── Combined non-defaults ─────────────────────────────────────────────────

    [Fact]
    public void RoundTrip_AllNonDefaults_Combined()
    {
        var s = Roundtrip(s =>
        {
            s.DiagonalKind    = PairGridDiagonalKind.Kde;
            s.OffDiagonalKind = PairGridOffDiagonalKind.None;
            s.Triangular      = PairGridTriangle.LowerOnly;
            s.DiagonalBins    = 30;
            s.MarkerSize      = 5.0;
            s.CellSpacing     = 0.05;
            s.Labels          = ["v0", "v1", "v2"];
            s.HueGroups       = [0, 1, 0, 1];
            s.HueLabels       = ["A", "B"];
        });
        Assert.Equal(PairGridDiagonalKind.Kde,        s.DiagonalKind);
        Assert.Equal(PairGridOffDiagonalKind.None,    s.OffDiagonalKind);
        Assert.Equal(PairGridTriangle.LowerOnly,      s.Triangular);
        Assert.Equal(30,    s.DiagonalBins);
        Assert.Equal(5.0,   s.MarkerSize, precision: 10);
        Assert.Equal(0.05,  s.CellSpacing, precision: 10);
        Assert.NotNull(s.Labels);
        Assert.NotNull(s.HueGroups);
        Assert.NotNull(s.HueLabels);
        Assert.Equal(["v0", "v1", "v2"], s.Labels);
        Assert.Equal([0, 1, 0, 1],       s.HueGroups);
        Assert.Equal(["A", "B"],         s.HueLabels);
    }
}
