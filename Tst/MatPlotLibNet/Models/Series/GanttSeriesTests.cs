// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="GanttSeries"/> default properties, task array invariants,
/// and timeline range computation. Adds spot-check coverage for parallel-array length
/// invariants and the <c>Math.Min/Max</c> branches in <see cref="GanttSeries.ComputeDataRange"/>.</summary>
public class GanttSeriesTests
{
    /// <summary>Tasks/Starts/Ends must be parallel arrays with equal length — the renderer
    /// iterates the shortest, but a mismatch is a contract violation upstream.</summary>
    [Fact]
    public void ParallelArrays_HaveEqualLength()
    {
        var series = new GanttSeries(["A", "B", "C"], [0.0, 1.0, 2.0], [1.0, 2.0, 3.0]);
        Assert.Equal(series.Tasks.Length, series.Starts.Length);
        Assert.Equal(series.Tasks.Length, series.Ends.Length);
    }

    /// <summary>X range must span Min(Starts) to Max(Ends) — covers the case where a task
    /// finishes earlier than another starts (overlapping/nested tasks).</summary>
    [Fact]
    public void ComputeDataRange_XSpansEarliestStartToLatestEnd()
    {
        // Task A: 5..10, Task B: 0..3, Task C: 8..15  → X range [0, 15]
        var series = new GanttSeries(["A", "B", "C"], [5.0, 0.0, 8.0], [10.0, 3.0, 15.0]);
        var range = series.ComputeDataRange(new TestAxesContext());
        Assert.Equal(0.0, range.XMin);
        Assert.Equal(15.0, range.XMax);
    }

    /// <summary>Y range falls back to the rows' own extent when the context has no Y limits: half a bar beyond the first and the
    /// last row, where a bar stands <c>BarHeight</c> tall (0.6), as matplotlib's barh draws each task; the rows stand in the category
    /// slots their names are ticked on, centred half a slot past their index.</summary>
    [Fact]
    public void ComputeDataRange_YDefaultsToTheBarsAcrossTheirRows()
    {
        var series = new GanttSeries(["A", "B", "C", "D"], [0.0, 0.0, 0.0, 0.0], [1.0, 1.0, 1.0, 1.0]);
        var range = series.ComputeDataRange(new TestAxesContext());
        Assert.Equal(0.2, range.YMin!.Value, 12);   // the lowest slot's centre 0.5 less half a bar of 0.6
        Assert.Equal(3.8, range.YMax!.Value, 12);   // the highest slot's centre 3.5 + half a bar
    }

    /// <summary>The only sticky edges are the bars' starts on X (Axes.bar registers a bar's foot and nothing else), so the margin
    /// pass pads the far end and the rows.</summary>
    [Fact]
    public void ComputeDataRange_StickyEdgesAreTheStartsAlone()
    {
        var series = new GanttSeries(["A"], [0.0], [10.0]);
        var range = series.ComputeDataRange(new TestAxesContext());
        Assert.Equal(0.0, range.StickyXMin);
        Assert.Equal(0.0, range.StickyXMax);
        Assert.Null(range.StickyYMin);
        Assert.Null(range.StickyYMax);
    }
}
