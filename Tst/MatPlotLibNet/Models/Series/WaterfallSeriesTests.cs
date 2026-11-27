// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="WaterfallSeries"/> default properties, construction, and
/// running-total Y-axis range computation. Adds spot-check coverage for the cumulative-sum
/// branch in <see cref="WaterfallSeries.ComputeDataRange"/> that wasn't previously exercised.</summary>
public class WaterfallSeriesTests
{
    /// <summary>Y range tracks the running total: at each step the cumulative sum is
    /// recomputed and YMin/YMax bracket the lowest/highest cumulative point. With deltas
    /// [+100, -60, +40] the cumulative path is 100, 40, 80 — so YMax=100 and YMin=0
    /// (clamped at 0 since all cumulative values are non-negative).</summary>
    [Fact]
    public void ComputeDataRange_TracksRunningTotal()
    {
        var series = new WaterfallSeries(["A", "B", "C"], [100.0, -60.0, 40.0]);
        var range = series.ComputeDataRange(new TestAxesContext());
        // Cumulative: 100, 40, 80 → max=100, min=0 (sticky baseline)
        Assert.Equal(100.0, range.YMax);
        Assert.Equal(0.0, range.YMin);
    }

    /// <summary>Y range goes negative when cumulative path dips below zero — the loop
    /// must record the running minimum, not just the per-step minimum value.</summary>
    [Fact]
    public void ComputeDataRange_NegativeCumulative_RecordsRunningMinimum()
    {
        // Deltas [-50, -30, +60] → cumulative -50, -80, -20. Min=-80, Max=0.
        var series = new WaterfallSeries(["A", "B", "C"], [-50.0, -30.0, 60.0]);
        var range = series.ComputeDataRange(new TestAxesContext());
        Assert.Equal(-80.0, range.YMin);
        Assert.Equal(0.0, range.YMax);
    }

    /// <summary>X range derives from the category count — the slots of the steps, 0 to the number of steps; context
    /// overrides take precedence when set so user-supplied limits still win.</summary>
    [Fact]
    public void ComputeDataRange_XSpansCategoryCount()
    {
        var series = new WaterfallSeries(["A", "B", "C", "D"], [10.0, 10.0, 10.0, 10.0]);
        var range = series.ComputeDataRange(new TestAxesContext());
        Assert.Equal(0.0, range.XMin);
        Assert.Equal(4.0, range.XMax);   // 4 category slots
    }
}
