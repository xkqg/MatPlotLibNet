// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Verifies <see cref="Figure"/> behavior.</summary>
public class FigureTests
{
    /// <summary>Verifies that AddSubPlot returns a new Axes and adds it to SubPlots.</summary>
    [Fact]
    public void AddSubPlot_ReturnsNewAxes()
    {
        var fig = new Figure();
        var axes = fig.AddSubPlot();
        Assert.NotNull(axes);
        Assert.Single(fig.SubPlots);
    }

    /// <summary>Verifies that multiple AddSubPlot calls each add a separate subplot.</summary>
    [Fact]
    public void AddSubPlot_MultipleCalls_AddsMultiple()
    {
        var fig = new Figure();
        fig.AddSubPlot();
        fig.AddSubPlot();
        fig.AddSubPlot();
        Assert.Equal(3, fig.SubPlots.Count);
    }

    /// <summary>Verifies that AddSubPlot with grid parameters stores the correct grid index.</summary>
    [Fact]
    public void AddSubPlot_WithGridPosition_StoresLayout()
    {
        var fig = new Figure();
        var ax1 = fig.AddSubPlot(2, 2, 1);
        var ax2 = fig.AddSubPlot(2, 2, 2);
        Assert.Equal(2, fig.SubPlots.Count);
        Assert.Equal(1, ax1.GridIndex);
        Assert.Equal(2, ax2.GridIndex);
    }

    /// <summary>Verifies that the Theme defaults to Theme.MatplotlibV2 (changed from Theme.Default in v1.1.4).</summary>
    [Fact]
    public void ANewFigure_WearsTheMatplotlibV2Theme()
    {
        var fig = new Figure();
        Assert.Same(Theme.MatplotlibV2, fig.Theme);
    }

    // --- GridSpec integration ---

    /// <summary>Verifies that AddSubPlot with GridSpec and GridPosition stores position on the Axes.</summary>
    [Fact]
    public void AddSubPlot_WithGridSpecAndPosition_StoresPosition()
    {
        var fig = new Figure();
        var gs = new GridSpec { Rows = 2, Cols = 3 };
        fig.GridSpec = gs;
        var ax = fig.AddSubPlot(gs, GridPosition.Single(0, 0));

        Assert.Single(fig.SubPlots);
        Assert.NotNull(ax.GridPosition);
        Assert.Equal(0, ax.GridPosition.Value.RowStart);
        Assert.Equal(1, ax.GridPosition.Value.RowEnd);
        Assert.Equal(0, ax.GridPosition.Value.ColStart);
        Assert.Equal(1, ax.GridPosition.Value.ColEnd);
    }

    /// <summary>Verifies that AddSubPlot with spanning stores the correct GridPosition.</summary>
    [Fact]
    public void AddSubPlot_WithGridSpecSpanning_StoresSpan()
    {
        var fig = new Figure();
        var gs = new GridSpec { Rows = 3, Cols = 3 };
        fig.GridSpec = gs;
        var ax = fig.AddSubPlot(gs, 0, 1, 0, 3);

        Assert.NotNull(ax.GridPosition);
        Assert.Equal(0, ax.GridPosition.Value.RowStart);
        Assert.Equal(1, ax.GridPosition.Value.RowEnd);
        Assert.Equal(0, ax.GridPosition.Value.ColStart);
        Assert.Equal(3, ax.GridPosition.Value.ColEnd);
    }

    /// <summary>Verifies that the legacy AddSubPlot(rows, cols, index) still works unchanged.</summary>
    [Fact]
    public void AddSubPlot_LegacyOverload_StillWorks()
    {
        var fig = new Figure();
        var ax = fig.AddSubPlot(2, 2, 1);
        Assert.Equal(2, ax.GridRows);
        Assert.Equal(2, ax.GridCols);
        Assert.Equal(1, ax.GridIndex);
        Assert.Null(ax.GridPosition);
    }
}
