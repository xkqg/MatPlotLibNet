// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies hatch pattern properties on filled-region series.</summary>
public class HatchSeriesTests
{
    // ── BarSeries ─────────────────────────────────────────────────────────────

    [Fact]
    public void BarSeries_Hatch_DefaultsToNone()
    {
        var s = new BarSeries(["A"], [1.0]);
        Assert.Equal(HatchPattern.None, s.Hatch);
    }

    [Fact]
    public void BarSeries_Hatch_CanBeSet()
    {
        var s = new BarSeries(["A"], [1.0]) { Hatch = HatchPattern.ForwardDiagonal };
        Assert.Equal(HatchPattern.ForwardDiagonal, s.Hatch);
    }

    [Fact]
    public void BarSeries_HatchColor_DefaultsToNull()
    {
        var s = new BarSeries(["A"], [1.0]);
        Assert.Null(s.HatchColor);
    }

    // ── HistogramSeries ───────────────────────────────────────────────────────

    [Fact]
    public void HistogramSeries_Hatch_DefaultsToNone()
    {
        var s = new HistogramSeries([1.0, 2.0, 3.0]);
        Assert.Equal(HatchPattern.None, s.Hatch);
    }

    [Fact]
    public void HistogramSeries_HatchColor_DefaultsToNull()
    {
        var s = new HistogramSeries([1.0, 2.0]);
        Assert.Null(s.HatchColor);
    }

    // ── AreaSeries ────────────────────────────────────────────────────────────

    [Fact]
    public void AreaSeries_Hatch_DefaultsToNone()
    {
        var s = new AreaSeries([1.0], [2.0]);
        Assert.Equal(HatchPattern.None, s.Hatch);
    }

    [Fact]
    public void AreaSeries_HatchColor_DefaultsToNull()
    {
        var s = new AreaSeries([1.0], [2.0]);
        Assert.Null(s.HatchColor);
    }

    // ── StackedAreaSeries ─────────────────────────────────────────────────────

    [Fact]
    public void StackedAreaSeries_Hatch_DefaultsToNone()
    {
        var s = new StackedAreaSeries([1.0], [[2.0]]);
        Assert.Equal(HatchPattern.None, s.Hatch);
    }

    [Fact]
    public void StackedAreaSeries_HatchColor_DefaultsToNull()
    {
        var s = new StackedAreaSeries([1.0], [[2.0]]);
        Assert.Null(s.HatchColor);
    }

    // ── PieSeries ─────────────────────────────────────────────────────────────

    [Fact]
    public void PieSeries_Hatches_DefaultsToNull()
    {
        var s = new PieSeries([1.0, 2.0]);
        Assert.Null(s.Hatches);
    }

    [Fact]
    public void PieSeries_Hatches_CanBeSet()
    {
        var s = new PieSeries([1.0, 2.0])
        {
            Hatches = [HatchPattern.Cross, HatchPattern.Horizontal]
        };
        Assert.Equal(2, s.Hatches!.Length);
        Assert.Equal(HatchPattern.Cross, s.Hatches[0]);
    }

    // ── ContourfSeries ────────────────────────────────────────────────────────

    [Fact]
    public void ContourfSeries_Hatches_DefaultsToNull()
    {
        var s = new ContourfSeries([0.0, 1.0], [0.0, 1.0], new double[2, 2]);
        Assert.Null(s.Hatches);
    }

    [Fact]
    public void ContourfSeries_Hatches_CanBeSet()
    {
        var s = new ContourfSeries([0.0, 1.0], [0.0, 1.0], new double[2, 2])
        {
            Hatches = [HatchPattern.Vertical, HatchPattern.Dots]
        };
        Assert.Equal(2, s.Hatches!.Length);
    }
}
