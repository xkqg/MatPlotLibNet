// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Minimal <see cref="IAxesContext"/> used by the spot-check tests in this file.</summary>
file sealed class NullCtx : IAxesContext
{
    public double? XAxisMin => null;
    public double? XAxisMax => null;
    public double? YAxisMin => null;
    public double? YAxisMax => null;
    public BarMode BarMode => BarMode.Grouped;
    public IReadOnlyList<ISeries> AllSeries => [];
}

/// <summary>Verifies <see cref="DonutSeries"/> default properties and construction.</summary>
public class DonutSeriesTests
{
    /// <summary>Verifies that the constructor stores sizes.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        var series = new DonutSeries([30.0, 70.0]);
        Assert.Equal([30.0, 70.0], series.Sizes);
    }

    /// <summary>Verifies that InnerRadius defaults to 0.4.</summary>
    [Fact]
    public void DefaultInnerRadius_Is0Point4()
    {
        var series = new DonutSeries([50.0, 50.0]);
        Assert.Equal(0.4, series.InnerRadius);
    }
}

/// <summary>Verifies <see cref="BubbleSeries"/> default properties and construction.</summary>
public class BubbleSeriesTests
{
    /// <summary>Verifies that the constructor stores X, Y, and size data.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        var series = new BubbleSeries([1.0, 2.0], [3.0, 4.0], [10, 20]);
        Assert.Equal([1.0, 2.0], series.XData);
        Assert.Equal([3.0, 4.0], series.YData);
        Assert.Equal([10.0, 20.0], series.Sizes);
    }

}

/// <summary>Verifies <see cref="OhlcBarSeries"/> default properties and construction.</summary>
public class OhlcBarSeriesTests
{
    /// <summary>Verifies that the constructor stores OHLC data arrays.</summary>
    [Fact]
    public void Constructor_StoresOhlcData()
    {
        var series = new OhlcBarSeries([10], [15], [8], [13]);
        Assert.Equal([10.0], series.Open);
        Assert.Equal([15.0], series.High);
    }
}

/// <summary>Verifies <see cref="WaterfallSeries"/> default properties, construction, and
/// running-total Y-axis range computation. Adds spot-check coverage for the cumulative-sum
/// branch in <see cref="WaterfallSeries.ComputeDataRange"/> that wasn't previously exercised.</summary>
public class WaterfallSeriesTests
{
    /// <summary>Verifies that the constructor stores categories and values.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        var series = new WaterfallSeries(["Revenue", "Cost", "Profit"], [100, -60, 40]);
        Assert.Equal(["Revenue", "Cost", "Profit"], series.Categories);
        Assert.Equal([100.0, -60.0, 40.0], series.Values);
    }

    /// <summary>BarWidth defaults to 0.6 — matches the matplotlib bar() default scaled
    /// for typical waterfall density.</summary>
    [Fact]
    public void BarWidth_DefaultsTo0p6()
    {
        var series = new WaterfallSeries(["A"], [1.0]);
        Assert.Equal(0.6, series.BarWidth);
    }

    /// <summary>Default colors: increase=green, decrease=red, total=Tab10Blue. These match
    /// matplotlib's seaborn-waterfall convention.</summary>
    [Fact]
    public void DefaultColors_MatchConvention()
    {
        var series = new WaterfallSeries(["A"], [1.0]);
        Assert.Equal(Colors.Green, series.IncreaseColor);
        Assert.Equal(Colors.Red, series.DecreaseColor);
        Assert.Equal(Colors.Tab10Blue, series.TotalColor);
    }

    /// <summary>Y range tracks the running total: at each step the cumulative sum is
    /// recomputed and YMin/YMax bracket the lowest/highest cumulative point. With deltas
    /// [+100, -60, +40] the cumulative path is 100, 40, 80 — so YMax=100 and YMin=0
    /// (clamped at 0 since all cumulative values are non-negative).</summary>
    [Fact]
    public void ComputeDataRange_TracksRunningTotal()
    {
        var series = new WaterfallSeries(["A", "B", "C"], [100.0, -60.0, 40.0]);
        var range = series.ComputeDataRange(new NullCtx());
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
        var range = series.ComputeDataRange(new NullCtx());
        Assert.Equal(-80.0, range.YMin);
        Assert.Equal(0.0, range.YMax);
    }

    /// <summary>X range derives from the category count — context overrides take precedence
    /// when set so user-supplied limits still win.</summary>
    [Fact]
    public void ComputeDataRange_XSpansCategoryCount()
    {
        var series = new WaterfallSeries(["A", "B", "C", "D"], [10.0, 10.0, 10.0, 10.0]);
        var range = series.ComputeDataRange(new NullCtx());
        Assert.Equal(-0.5, range.XMin);
        Assert.Equal(3.5, range.XMax);   // 4 categories - 0.5
    }

    /// <summary>DTO type stamp must be "waterfall" so the renderer registry resolves it.</summary>
    [Fact]
    public void ToSeriesDto_TypeIsWaterfall()
    {
        var series = new WaterfallSeries(["A"], [1.0]);
        Assert.Equal("waterfall", series.ToSeriesDto().Type);
    }
}

/// <summary>Verifies <see cref="FunnelSeries"/> default properties and construction.</summary>
public class FunnelSeriesTests
{
    /// <summary>Verifies that the constructor stores labels and values.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        var series = new FunnelSeries(["Visits", "Signups", "Paid"], [1000, 300, 50]);
        Assert.Equal(["Visits", "Signups", "Paid"], series.Labels);
        Assert.Equal([1000.0, 300.0, 50.0], series.Values);
    }
}

/// <summary>Verifies <see cref="GanttSeries"/> default properties, task array invariants,
/// and timeline range computation. Adds spot-check coverage for parallel-array length
/// invariants and the <c>Math.Min/Max</c> branches in <see cref="GanttSeries.ComputeDataRange"/>.</summary>
public class GanttSeriesTests
{
    /// <summary>Verifies that the constructor stores tasks, starts, and ends.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        var series = new GanttSeries(["Task A", "Task B"], [0, 2], [3, 5]);
        Assert.Equal(["Task A", "Task B"], series.Tasks);
        Assert.Equal([0.0, 2.0], series.Starts);
        Assert.Equal([3.0, 5.0], series.Ends);
    }

    /// <summary>Tasks/Starts/Ends must be parallel arrays with equal length — the renderer
    /// iterates the shortest, but a mismatch is a contract violation upstream.</summary>
    [Fact]
    public void ParallelArrays_HaveEqualLength()
    {
        var series = new GanttSeries(["A", "B", "C"], [0.0, 1.0, 2.0], [1.0, 2.0, 3.0]);
        Assert.Equal(series.Tasks.Length, series.Starts.Length);
        Assert.Equal(series.Tasks.Length, series.Ends.Length);
    }

    /// <summary>BarHeight defaults to 0.6 — leaves a 0.4-row gap between bars at unit row spacing.</summary>
    [Fact]
    public void BarHeight_DefaultsTo0p6()
    {
        var series = new GanttSeries(["A"], [0.0], [1.0]);
        Assert.Equal(0.6, series.BarHeight);
    }

    /// <summary>Color defaults to null so the theme prop-cycler picks a colour at render time.</summary>
    [Fact]
    public void Color_DefaultsToNull()
    {
        var series = new GanttSeries(["A"], [0.0], [1.0]);
        Assert.Null(series.Color);
    }

    /// <summary>X range must span Min(Starts) to Max(Ends) — covers the case where a task
    /// finishes earlier than another starts (overlapping/nested tasks).</summary>
    [Fact]
    public void ComputeDataRange_XSpansEarliestStartToLatestEnd()
    {
        // Task A: 5..10, Task B: 0..3, Task C: 8..15  → X range [0, 15]
        var series = new GanttSeries(["A", "B", "C"], [5.0, 0.0, 8.0], [10.0, 3.0, 15.0]);
        var range = series.ComputeDataRange(new NullCtx());
        Assert.Equal(0.0, range.XMin);
        Assert.Equal(15.0, range.XMax);
    }

    /// <summary>Y range falls back to <c>[-0.5, N - 0.5]</c> when the context has no Y limits,
    /// matching the single-row-per-task convention.</summary>
    [Fact]
    public void ComputeDataRange_YDefaultsToTaskRowBounds()
    {
        var series = new GanttSeries(["A", "B", "C", "D"], [0.0, 0.0, 0.0, 0.0], [1.0, 1.0, 1.0, 1.0]);
        var range = series.ComputeDataRange(new NullCtx());
        Assert.Equal(-0.5, range.YMin);
        Assert.Equal(3.5, range.YMax);   // 4 tasks - 0.5
    }

    /// <summary>Sticky edges fix both X and Y so the auto-margin pass cannot push the range.
    /// This matches matplotlib's behaviour of treating Gantt bars as "fully bounded" (left
    /// and right edges of the bar are exact data points).</summary>
    [Fact]
    public void ComputeDataRange_StickyEdgesArePopulated()
    {
        var series = new GanttSeries(["A"], [0.0], [10.0]);
        var range = series.ComputeDataRange(new NullCtx());
        Assert.NotNull(range.StickyXMin);
        Assert.NotNull(range.StickyXMax);
        Assert.NotNull(range.StickyYMin);
        Assert.NotNull(range.StickyYMax);
    }

    /// <summary>DTO type stamp must be "gantt" so the renderer registry resolves it.</summary>
    [Fact]
    public void ToSeriesDto_TypeIsGantt()
    {
        var series = new GanttSeries(["A"], [0.0], [1.0]);
        Assert.Equal("gantt", series.ToSeriesDto().Type);
    }
}
