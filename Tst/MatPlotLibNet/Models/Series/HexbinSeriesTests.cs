// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="HexbinSeries"/> default properties and serialization.</summary>
public class HexbinSeriesTests
{
    /// <summary>Constructor stores X and Y data.</summary>
    [Fact]
    public void Constructor_StoresXAndYData()
    {
        double[] x = [1.0, 2.0];
        double[] y = [3.0, 4.0];
        var series = new HexbinSeries(x, y);
        Assert.Equal(x, series.X);
        Assert.Equal(y, series.Y);
    }

    /// <summary>GridSize defaults to 20.</summary>
    [Fact]
    public void GridSize_DefaultsTo20()
    {
        var series = new HexbinSeries([], []);
        Assert.Equal(20, series.GridSize);
    }

    /// <summary>MinCount defaults to 1.</summary>
    [Fact]
    public void MinCount_DefaultsTo1()
    {
        var series = new HexbinSeries([], []);
        Assert.Equal(1, series.MinCount);
    }

    /// <summary>ToSeriesDto returns type "hexbin".</summary>
    [Fact]
    public void ToSeriesDto_ReturnsTypeHexbin()
    {
        var series = new HexbinSeries([1.0], [1.0]);
        Assert.Equal("hexbin", series.ToSeriesDto().Type);
    }

    // ── Phase X.4 follow-up (v1.7.2, 2026-04-19) — branch lifts ──

    /// <summary>GetColorBarRange line 37: empty-X early return → (0, 1) sentinel.</summary>
    [Fact]
    public void GetColorBarRange_EmptyData_ReturnsSentinel()
    {
        var s = new HexbinSeries([], []);
        Assert.Equal(new MatPlotLibNet.Numerics.MinMaxRange(0.0, 1.0), s.GetColorBarRange());
    }

    /// <summary>GetColorBarRange line 40-41: degenerate axis (xMin==xMax / yMin==yMax)
    /// triggers the +1 fallback so HexGrid doesn't divide by zero.</summary>
    [Fact]
    public void GetColorBarRange_DegenerateAxes_AppliesFallback()
    {
        var s = new HexbinSeries([2.0, 2.0, 2.0], [3.0, 3.0, 3.0]);
        var (min, max) = s.GetColorBarRange();
        Assert.Equal(1, min);     // MinCount default
        Assert.True(max >= 1);    // some count in the bin
    }

    /// <summary>GetColorBarRange happy path with a populated grid (lines 38-46 fully).</summary>
    [Fact]
    public void GetColorBarRange_PopulatedGrid_ReturnsMinCountAndMaxBin()
    {
        var rng = new Random(42);
        var x = Enumerable.Range(0, 200).Select(_ => rng.NextDouble() * 10).ToArray();
        var y = Enumerable.Range(0, 200).Select(_ => rng.NextDouble() * 10).ToArray();
        var s = new HexbinSeries(x, y) { MinCount = 1 };
        var (min, max) = s.GetColorBarRange();
        Assert.Equal(1, min);
        Assert.True(max >= 1);
    }

    /// <summary>ComputeDataRange line 51: empty-X returns sentinel (null bounds).</summary>
    [Fact]
    public void ComputeDataRange_Empty_ReturnsNullBounds()
    {
        var s = new HexbinSeries([], []);
        var range = s.ComputeDataRange(new NullCtx());
        Assert.Null(range.XMin);
    }

    private sealed class NullCtx : IAxesContext
    {
        public double? XAxisMin => null;
        public double? XAxisMax => null;
        public double? YAxisMin => null;
        public double? YAxisMax => null;
        public BarMode BarMode => BarMode.Grouped;
        public IReadOnlyList<ISeries> AllSeries => [];
    }

    /// <summary>ToSeriesDto line 61-62: non-default GridSize/MinCount round-trips
    /// (the ?? null short-circuit's false arm fires when value differs from default).</summary>
    [Fact]
    public void ToSeriesDto_NonDefaultGridSizeAndMinCount_AreEmitted()
    {
        var s = new HexbinSeries([1.0], [1.0]) { GridSize = 50, MinCount = 5 };
        var dto = s.ToSeriesDto();
        Assert.Equal(50, dto.GridSize);
        Assert.Equal(5, dto.MinCount);
    }
}
