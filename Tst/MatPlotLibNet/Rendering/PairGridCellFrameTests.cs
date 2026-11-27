// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Direct branch coverage for the pair-grid cell's geometry: the span of one variable
/// (<see cref="AxisSpan.Of"/>) and the map from data to a pixel of the cell (<see cref="PairGridCellFrame.Map"/>).
/// Indirect coverage via the render tests leaves a few branches dark — these direct
/// tests close those gaps.</summary>
public class PairGridCellFrameTests
{
    private static readonly PairGridCellFrame UnitFrame = new(new AxisSpan(0, 10), new AxisSpan(0, 10), new Rect(0, 0, 100, 100));

    // ── AxisSpan.Of ──────────────────────────────────────────────────────

    [Fact]
    public void AxisSpan_AscendingValues_FindsMinAndMax()
    {
        var data = new[] { 1.0, 2.0, 3.0, 4.0 };
        var (min, span) = AxisSpan.Of(data, data.Length);
        Assert.Equal(1.0, min);
        Assert.Equal(3.0, span);
    }

    [Fact]
    public void AxisSpan_UnorderedValues_NewMinFoundLater()
    {
        // First sample is 5; subsequent 1 must trigger the v<mn branch.
        var data = new[] { 5.0, 1.0, 3.0 };
        var (min, span) = AxisSpan.Of(data, data.Length);
        Assert.Equal(1.0, min);
        Assert.Equal(4.0, span);
    }

    [Fact]
    public void AxisSpan_AllEqualValues_DegenerateGuardKicksIn()
    {
        // All-equal samples → mn == mx → degenerate-guard sets mx = mn + 1.
        var data = new[] { 7.0, 7.0, 7.0 };
        var (min, span) = AxisSpan.Of(data, data.Length);
        Assert.Equal(7.0, min);
        Assert.Equal(1.0, span);
    }

    [Fact]
    public void AxisSpan_AllNonFinite_FallsBackToZeroOne()
    {
        // No finite samples → fallback (min=0, span=1) so Map stays finite.
        var data = new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity };
        var (min, span) = AxisSpan.Of(data, data.Length);
        Assert.Equal(0.0, min);
        Assert.Equal(1.0, span);
    }

    [Fact]
    public void AxisSpan_MixedFiniteAndNaN_SkipsNaN()
    {
        var data = new[] { 2.0, double.NaN, 4.0, double.PositiveInfinity, 6.0 };
        var (min, span) = AxisSpan.Of(data, data.Length);
        Assert.Equal(2.0, min);
        Assert.Equal(4.0, span); // 6 - 2
    }

    [Fact]
    public void AxisSpan_EmptyArray_FallsBackToZeroOne()
    {
        var (min, span) = AxisSpan.Of([], 0);
        Assert.Equal(0.0, min);
        Assert.Equal(1.0, span);
    }

    // ── Map ─────────────────────────────────────────────────────────────

    [Fact]
    public void Map_OriginInDataSpace_MapsToCellOriginXAndCellBottomY()
    {
        // Data (xMin, yMin) maps to (cell.X, cell.Bottom) since Y is inverted.
        var p = UnitFrame.Map(0.0, 0.0);
        Assert.Equal(0.0,   p.X);
        Assert.Equal(100.0, p.Y); // cell.Bottom for a 100-tall cell
    }

    [Fact]
    public void Map_DataMaxX_MapsToCellRight()
    {
        var p = UnitFrame.Map(10.0, 0.0);
        Assert.Equal(100.0, p.X);
    }

    [Fact]
    public void Map_DataMaxY_MapsToCellTop()
    {
        var p = UnitFrame.Map(0.0, 10.0);
        // Larger Y in data space → smaller pixel Y (top of cell).
        Assert.Equal(0.0, p.Y);
    }

    /// <summary>Samples none of which can be drawn span 0 to 1, and samples of one value span one unit up from it.</summary>
    [Theory]
    [InlineData(double.NaN, double.NaN, 0.0, 1.0)]
    [InlineData(3.0, 3.0, 3.0, 1.0)]
    public void AxisSpan_OfNothingToDraw_OrOfOneValue_HasAWidth(double first, double second, double min, double span)
    {
        Assert.Equal(new AxisSpan(min, span), AxisSpan.Of([first, second], 2));
    }

    /// <summary>The span is read over the first samples asked for only: a sample past them does not widen it.</summary>
    [Fact]
    public void AxisSpan_ReadsTheFirstSamplesOnly()
    {
        Assert.Equal(new AxisSpan(1, 1), AxisSpan.Of([1.0, 2.0, 100.0], 2));
    }
}
