// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>v1.10 — direct branch coverage for the pair-grid geometry helpers
/// (<see cref="PairGridGeometry.MapPoint"/> and <see cref="PairGridGeometry.ComputeAxisSpan"/>).
/// Indirect coverage via the render tests leaves a few branches dark — these direct
/// tests close those gaps.</summary>
public class PairGridGeometryTests
{
    private static readonly Rect UnitCell = new(0, 0, 100, 100);

    // ── ComputeAxisSpan ──────────────────────────────────────────────────────

    [Fact]
    public void ComputeAxisSpan_AscendingValues_FindsMinAndMax()
    {
        var data = new[] { 1.0, 2.0, 3.0, 4.0 };
        PairGridGeometry.ComputeAxisSpan(data, data.Length, out double min, out double span);
        Assert.Equal(1.0, min);
        Assert.Equal(3.0, span);
    }

    [Fact]
    public void ComputeAxisSpan_UnorderedValues_NewMinFoundLater()
    {
        // First sample is 5; subsequent 1 must trigger the v<mn branch.
        var data = new[] { 5.0, 1.0, 3.0 };
        PairGridGeometry.ComputeAxisSpan(data, data.Length, out double min, out double span);
        Assert.Equal(1.0, min);
        Assert.Equal(4.0, span);
    }

    [Fact]
    public void ComputeAxisSpan_AllEqualValues_DegenerateGuardKicksIn()
    {
        // All-equal samples → mn == mx → degenerate-guard sets mx = mn + 1.
        var data = new[] { 7.0, 7.0, 7.0 };
        PairGridGeometry.ComputeAxisSpan(data, data.Length, out double min, out double span);
        Assert.Equal(7.0, min);
        Assert.Equal(1.0, span);
    }

    [Fact]
    public void ComputeAxisSpan_AllNonFinite_FallsBackToZeroOne()
    {
        // No finite samples → fallback (min=0, span=1) so MapPoint stays finite.
        var data = new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity };
        PairGridGeometry.ComputeAxisSpan(data, data.Length, out double min, out double span);
        Assert.Equal(0.0, min);
        Assert.Equal(1.0, span);
    }

    [Fact]
    public void ComputeAxisSpan_MixedFiniteAndNaN_SkipsNaN()
    {
        var data = new[] { 2.0, double.NaN, 4.0, double.PositiveInfinity, 6.0 };
        PairGridGeometry.ComputeAxisSpan(data, data.Length, out double min, out double span);
        Assert.Equal(2.0, min);
        Assert.Equal(4.0, span); // 6 - 2
    }

    [Fact]
    public void ComputeAxisSpan_EmptyArray_FallsBackToZeroOne()
    {
        PairGridGeometry.ComputeAxisSpan([], 0, out double min, out double span);
        Assert.Equal(0.0, min);
        Assert.Equal(1.0, span);
    }

    // ── MapPoint ─────────────────────────────────────────────────────────────

    [Fact]
    public void MapPoint_OriginInDataSpace_MapsToCellOriginXAndCellBottomY()
    {
        // Data (xMin, yMin) maps to (cell.X, cell.Bottom) since Y is inverted.
        var p = PairGridGeometry.MapPoint(0.0, 0.0, xMin: 0, xSpan: 10, yMin: 0, ySpan: 10, UnitCell);
        Assert.Equal(0.0,   p.X);
        Assert.Equal(100.0, p.Y); // cell.Bottom for a 100-tall cell
    }

    [Fact]
    public void MapPoint_DataMaxX_MapsToCellRight()
    {
        var p = PairGridGeometry.MapPoint(10.0, 0.0, xMin: 0, xSpan: 10, yMin: 0, ySpan: 10, UnitCell);
        Assert.Equal(100.0, p.X);
    }

    [Fact]
    public void MapPoint_DataMaxY_MapsToCellTop()
    {
        var p = PairGridGeometry.MapPoint(0.0, 10.0, xMin: 0, xSpan: 10, yMin: 0, ySpan: 10, UnitCell);
        // Larger Y in data space → smaller pixel Y (top of cell).
        Assert.Equal(0.0, p.Y);
    }
}
