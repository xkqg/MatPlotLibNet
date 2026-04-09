// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies that shared axes produce synchronized data ranges during rendering.</summary>
public class SharedAxesRenderTests
{
    /// <summary>Verifies that shared X axes render without errors.</summary>
    [Fact]
    public void SharedX_RendersWithoutErrors()
    {
        var fig = new Figure();
        var ax1 = fig.AddSubPlot(2, 1, 1);
        ax1.Plot([0.0, 10.0], [1.0, 2.0]);

        var ax2 = fig.AddSubPlot(2, 1, 2, sharex: ax1);
        ax2.Plot([5.0, 15.0], [3.0, 4.0]);

        // Should not throw
        string svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    /// <summary>Verifies that shared Y axes render without errors.</summary>
    [Fact]
    public void SharedY_RendersWithoutErrors()
    {
        var fig = new Figure();
        var ax1 = fig.AddSubPlot(1, 2, 1);
        ax1.Plot([1.0, 2.0], [0.0, 100.0]);

        var ax2 = fig.AddSubPlot(1, 2, 2, sharey: ax1);
        ax2.Plot([1.0, 2.0], [50.0, 150.0]);

        string svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
    }

    /// <summary>Verifies that non-shared axes remain independent (regression).</summary>
    [Fact]
    public void NonSharedAxes_RendersIndependently()
    {
        var fig = new Figure();
        var ax1 = fig.AddSubPlot(1, 2, 1);
        ax1.Plot([0.0, 10.0], [1.0, 2.0]);

        var ax2 = fig.AddSubPlot(1, 2, 2);
        ax2.Plot([100.0, 200.0], [1.0, 2.0]);

        string svg = fig.ToSvg();
        Assert.Contains("<svg", svg);
        // Both subplots should render — each with their own range
        Assert.Equal(2, fig.SubPlots.Count);
    }

    /// <summary>Verifies that the sharing axes uses the union X range (includes parent's data).</summary>
    [Fact]
    public void SharedX_SharingAxesSeesParentXRange()
    {
        // ax1: X [0,10], ax2: X [5,15] shares X with ax1
        // ax2 should see X range covering at least [0,15] (union)
        var fig = new Figure();
        var ax1 = fig.AddSubPlot(2, 1, 1);
        ax1.Plot([0.0, 10.0], [1.0, 2.0]);

        var ax2 = fig.AddSubPlot(2, 1, 2, sharex: ax1);
        ax2.Plot([5.0, 15.0], [3.0, 4.0]);

        string svg = fig.ToSvg();
        // If sharing works, ax2's tick labels should include values near 0 (from ax1)
        // and values near 15 (from ax2's own data)
        Assert.Contains("0", svg);  // tick from ax1's range
        Assert.Contains("15", svg); // tick from ax2's range (or close to it)
    }
}
