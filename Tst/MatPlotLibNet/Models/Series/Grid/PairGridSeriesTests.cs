// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>v1.10 Phase 4 — Verifies <see cref="PairGridSeries"/> construction, validation,
/// default properties, and clamping. Render + serialization behaviour live in
/// <c>PairGridRenderTests</c> and <c>PairGridSerializationTests</c>.</summary>
public class PairGridSeriesTests
{
    private static double[][] SampleVars => new[]
    {
        new[] { 1.0, 2.0, 3.0, 4.0 },
        new[] { 0.5, 1.5, 2.5, 3.5 },
        new[] { 9.0, 8.0, 7.0, 6.0 },
    };

    // ── Construction ─────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_StoresVariables()
    {
        var v = SampleVars;
        var s = new PairGridSeries(v);
        Assert.Same(v, s.Variables);
    }

    [Fact]
    public void Constructor_EmptyVariables_Throws()
    {
        Assert.Throws<ArgumentException>(() => new PairGridSeries(Array.Empty<double[]>()));
    }

    [Fact]
    public void Constructor_JaggedVariables_Throws()
    {
        var v = new[]
        {
            new[] { 1.0, 2.0, 3.0 },
            new[] { 1.0, 2.0 }, // shorter
        };
        Assert.Throws<ArgumentException>(() => new PairGridSeries(v));
    }

    [Fact]
    public void Constructor_SingleVariable_Accepted()
    {
        var v = new[] { new[] { 1.0, 2.0, 3.0 } };
        var s = new PairGridSeries(v);
        Assert.Single(s.Variables);
    }

    // ── Default properties ────────────────────────────────────────────────────

    [Fact]
    public void DiagonalKind_DefaultsToHistogram()
    {
        var s = new PairGridSeries(SampleVars);
        Assert.Equal(PairGridDiagonalKind.Histogram, s.DiagonalKind);
    }

    [Fact]
    public void OffDiagonalKind_DefaultsToScatter()
    {
        var s = new PairGridSeries(SampleVars);
        Assert.Equal(PairGridOffDiagonalKind.Scatter, s.OffDiagonalKind);
    }

    [Fact]
    public void Triangular_DefaultsToBoth()
    {
        var s = new PairGridSeries(SampleVars);
        Assert.Equal(PairGridTriangle.Both, s.Triangular);
    }

    [Fact]
    public void DiagonalBins_DefaultsTo20()
    {
        var s = new PairGridSeries(SampleVars);
        Assert.Equal(20, s.DiagonalBins);
    }

    [Fact]
    public void MarkerSize_DefaultsTo3()
    {
        var s = new PairGridSeries(SampleVars);
        Assert.Equal(3.0, s.MarkerSize);
    }

    [Fact]
    public void CellSpacing_DefaultsTo002()
    {
        var s = new PairGridSeries(SampleVars);
        Assert.Equal(0.02, s.CellSpacing, 10);
    }

    [Fact]
    public void Labels_DefaultsToNull()
    {
        var s = new PairGridSeries(SampleVars);
        Assert.Null(s.Labels);
    }

    [Fact]
    public void HueGroups_DefaultsToNull()
    {
        var s = new PairGridSeries(SampleVars);
        Assert.Null(s.HueGroups);
    }

    [Fact]
    public void HueLabels_DefaultsToNull()
    {
        var s = new PairGridSeries(SampleVars);
        Assert.Null(s.HueLabels);
    }

    [Fact]
    public void HuePalette_DefaultsToNull()
    {
        var s = new PairGridSeries(SampleVars);
        Assert.Null(s.HuePalette);
    }

    // ── CellSpacing clamping [0, 0.2] ─────────────────────────────────────────

    [Fact]
    public void CellSpacing_NegativeValue_ClampsToZero()
    {
        var s = new PairGridSeries(SampleVars) { CellSpacing = -0.5 };
        Assert.Equal(0.0, s.CellSpacing, 10);
    }

    [Fact]
    public void CellSpacing_AboveMax_ClampsToTwoTenths()
    {
        var s = new PairGridSeries(SampleVars) { CellSpacing = 0.9 };
        Assert.Equal(0.2, s.CellSpacing, 10);
    }

    [Fact]
    public void CellSpacing_InRange_StoredVerbatim()
    {
        var s = new PairGridSeries(SampleVars) { CellSpacing = 0.07 };
        Assert.Equal(0.07, s.CellSpacing, 10);
    }

    [Fact]
    public void CellSpacing_ExactlyZero_Allowed()
    {
        var s = new PairGridSeries(SampleVars) { CellSpacing = 0.0 };
        Assert.Equal(0.0, s.CellSpacing, 10);
    }

    [Fact]
    public void CellSpacing_ExactlyTwoTenths_Allowed()
    {
        var s = new PairGridSeries(SampleVars) { CellSpacing = 0.2 };
        Assert.Equal(0.2, s.CellSpacing, 10);
    }

    // ── Init properties accepted ──────────────────────────────────────────────

    [Fact]
    public void Labels_SetViaInit_Stored()
    {
        var labs = new[] { "A", "B", "C" };
        var s = new PairGridSeries(SampleVars) { Labels = labs };
        Assert.Same(labs, s.Labels);
    }

    [Fact]
    public void HueGroups_SetViaInit_Stored()
    {
        var hue = new[] { 0, 1, 0, 1 };
        var s = new PairGridSeries(SampleVars) { HueGroups = hue };
        Assert.Same(hue, s.HueGroups);
    }

    [Fact]
    public void HueLabels_SetViaInit_Stored()
    {
        var labs = new[] { "Setosa", "Versicolor" };
        var s = new PairGridSeries(SampleVars) { HueLabels = labs };
        Assert.Same(labs, s.HueLabels);
    }

    // ── ToSeriesDto ───────────────────────────────────────────────────────────

    [Fact]
    public void ToSeriesDto_TypeIsPairGrid()
    {
        var s = new PairGridSeries(SampleVars);
        Assert.Equal("pairgrid", s.ToSeriesDto().Type);
    }

    [Fact]
    public void ToSeriesDto_DefaultProperties_NotEmittedAsNonNull()
    {
        var s = new PairGridSeries(SampleVars);
        var dto = s.ToSeriesDto();
        // Type is the only required tag; everything else stays null on a fresh series.
        Assert.Null(dto.PairGridLabels);
        Assert.Null(dto.PairGridHueGroups);
        Assert.Null(dto.PairGridHueLabels);
        Assert.Null(dto.PairGridDiagonal);     // default Histogram → null
        Assert.Null(dto.PairGridOffDiagonal);  // default Scatter   → null
        Assert.Null(dto.PairGridTriangular);   // default Both      → null
        Assert.Null(dto.PairGridDiagonalBins); // default 20        → null
        Assert.Null(dto.PairGridMarkerSize);   // default 3.0       → null
        Assert.Null(dto.PairGridCellSpacing);  // default 0.02      → null
    }

    [Fact]
    public void ToSeriesDto_NonDefaultDiagonalKind_EmittedAsString()
    {
        var s = new PairGridSeries(SampleVars) { DiagonalKind = PairGridDiagonalKind.Kde };
        var dto = s.ToSeriesDto();
        Assert.Equal("Kde", dto.PairGridDiagonal);
    }

    [Fact]
    public void ToSeriesDto_NonDefaultTriangular_EmittedAsString()
    {
        var s = new PairGridSeries(SampleVars) { Triangular = PairGridTriangle.LowerOnly };
        var dto = s.ToSeriesDto();
        Assert.Equal("LowerOnly", dto.PairGridTriangular);
    }

    [Fact]
    public void ToSeriesDto_NonDefaultOffDiagonalKind_EmittedAsString()
    {
        var s = new PairGridSeries(SampleVars) { OffDiagonalKind = PairGridOffDiagonalKind.None };
        var dto = s.ToSeriesDto();
        Assert.Equal("None", dto.PairGridOffDiagonal);
    }

    [Fact]
    public void ToSeriesDto_NonDefaultBinsAndMarker_Emitted()
    {
        var s = new PairGridSeries(SampleVars)
        {
            DiagonalBins = 50,
            MarkerSize = 5.0,
            CellSpacing = 0.05,
        };
        var dto = s.ToSeriesDto();
        Assert.Equal(50,   dto.PairGridDiagonalBins);
        Assert.Equal(5.0,  dto.PairGridMarkerSize);
        Assert.Equal(0.05, dto.PairGridCellSpacing!.Value, 10);
    }

    [Fact]
    public void ToSeriesDto_VariablesRoundTripShape()
    {
        var s = new PairGridSeries(SampleVars);
        var dto = s.ToSeriesDto();
        Assert.NotNull(dto.Variables);
        Assert.Equal(3, dto.Variables!.Count);
        Assert.Equal(4, dto.Variables[0].Count);
    }

    [Fact]
    public void ToSeriesDto_LabelsHueRoundTripShape()
    {
        var labs = new[] { "A", "B", "C" };
        var hue = new[] { 0, 1, 0, 1 };
        var hLabs = new[] { "G0", "G1" };
        var s = new PairGridSeries(SampleVars)
        {
            Labels = labs,
            HueGroups = hue,
            HueLabels = hLabs,
        };
        var dto = s.ToSeriesDto();
        Assert.Equal(labs,  dto.PairGridLabels);
        Assert.Equal(hue,   dto.PairGridHueGroups);
        Assert.Equal(hLabs, dto.PairGridHueLabels);
    }

    // ── ComputeDataRange ──────────────────────────────────────────────────────

    [Fact]
    public void ComputeDataRange_ReturnsZeroToN()
    {
        var s = new PairGridSeries(SampleVars);
        var range = s.ComputeDataRange(null!);
        // 3 variables → outer axes span [0, 3] in both dimensions
        Assert.Equal(0.0, range.XMin);
        Assert.Equal(3.0, range.XMax);
        Assert.Equal(0.0, range.YMin);
        Assert.Equal(3.0, range.YMax);
    }
}
