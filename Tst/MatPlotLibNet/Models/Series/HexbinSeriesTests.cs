// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Styling;
using MatPlotLibNet;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="HexbinSeries"/> behaviour; what it starts from and the settings it stores are held by <see cref="Serialization.RestingValueCorpusTests"/> and the drawn-property round trips.</summary>
public class HexbinSeriesTests
{
    /// <summary>Unlike XYSeries-derived series (validated by the verb that constructs them), HexbinSeries has no
    /// constructing verb check -- the constructor validates itself, so a caller who constructs it directly is
    /// held to the same invariant.</summary>
    [Fact]
    public void Constructor_MismatchedLengths_Throws()
    {
        Assert.Throws<ArgumentException>(() => new HexbinSeries([1.0, 2.0, 3.0], [1.0, 2.0]));
    }

    // ── Phase X.4 follow-up (v1.7.2, 2026-04-19) — branch lifts ──

    /// <summary>GetColorBarRange: empty-X early return → (0, 1) sentinel.</summary>
    [Fact]
    public void GetColorBarRange_EmptyData_ReturnsSentinel()
    {
        var s = new HexbinSeries([], []);
        Assert.Equal(new MatPlotLibNet.Numerics.MinMaxRange(0.0, 1.0), s.GetColorBarRange());
    }

    /// <summary>GetColorBarRange: degenerate axis (xMin==xMax / yMin==yMax) is widened by
    /// <c>HexExtent</c> so the grid has a width and does not divide by zero.</summary>
    [Fact]
    public void GetColorBarRange_DegenerateAxes_AppliesFallback()
    {
        var s = new HexbinSeries([2.0, 2.0, 2.0], [3.0, 3.0, 3.0]);
        var (min, max) = s.GetColorBarRange();
        Assert.Equal(0.5, min);   // half a count under MinCount's default, the range the hexagons are coloured over (library themes)
        Assert.True(max >= 1);    // some count in the bin
    }

    /// <summary>GetColorBarRange happy path with a populated grid.</summary>
    [Fact]
    public void GetColorBarRange_PopulatedGrid_ReturnsMinCountAndMaxBin()
    {
        var rng = new Random(42);
        var x = Enumerable.Range(0, 200).Select(_ => rng.NextDouble() * 10).ToArray();
        var y = Enumerable.Range(0, 200).Select(_ => rng.NextDouble() * 10).ToArray();
        var s = new HexbinSeries(x, y) { MinCount = 1 };
        var (min, max) = s.GetColorBarRange();
        Assert.Equal(0.5, min);
        Assert.True(max >= 1);
    }

    /// <summary>ComputeDataRange: empty-X returns sentinel (null bounds).</summary>
    [Fact]
    public void ComputeDataRange_Empty_ReturnsNullBounds()
    {
        var s = new HexbinSeries([], []);
        var range = s.ComputeDataRange(new TestAxesContext());
        Assert.Null(range.XMin);
    }

    [Fact]
    public void HexbinSeries_GetColorBarRange_EmptyData_ReturnsZeroOne()
    {
        var s = new HexbinSeries((Vec)Array.Empty<double>(), (Vec)Array.Empty<double>());
        var (min, max) = s.GetColorBarRange();
        Assert.Equal(0, min);
        Assert.Equal(1, max);
    }
}
