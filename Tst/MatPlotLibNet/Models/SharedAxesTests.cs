// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Verifies shared axes (sharex/sharey) behavior on <see cref="Axes"/> and <see cref="Figure"/>.</summary>
public class SharedAxesTests
{
    [Fact]
    public void ShareXWith_DefaultIsNull()
    {
        var axes = new Axes();
        Assert.Null(axes.ShareXWith);
    }

    [Fact]
    public void ShareYWith_DefaultIsNull()
    {
        var axes = new Axes();
        Assert.Null(axes.ShareYWith);
    }

    [Fact]
    public void AddSubPlot_WithShareX_SetsReference()
    {
        var fig = new Figure();
        var ax1 = fig.AddSubPlot(1, 2, 1);
        var ax2 = fig.AddSubPlot(1, 2, 2, sharex: ax1);

        Assert.Same(ax1, ax2.ShareXWith);
    }

    [Fact]
    public void AddSubPlot_WithShareY_SetsReference()
    {
        var fig = new Figure();
        var ax1 = fig.AddSubPlot(2, 1, 1);
        var ax2 = fig.AddSubPlot(2, 1, 2, sharey: ax1);

        Assert.Same(ax1, ax2.ShareYWith);
    }

    [Fact]
    public void AddSubPlot_WithBothShared_SetsBothReferences()
    {
        var fig = new Figure();
        var ax1 = fig.AddSubPlot(2, 2, 1);
        var ax2 = fig.AddSubPlot(2, 2, 2, sharex: ax1, sharey: ax1);

        Assert.Same(ax1, ax2.ShareXWith);
        Assert.Same(ax1, ax2.ShareYWith);
    }

    [Fact]
    public void AddSubPlot_GridSpec_WithShareX_SetsReference()
    {
        var fig = new Figure();
        var gs = new GridSpec { Rows = 1, Cols = 2 };
        fig.GridSpec = gs;
        var ax1 = fig.AddSubPlot(gs, GridPosition.Single(0, 0));
        var ax2 = fig.AddSubPlot(gs, GridPosition.Single(0, 1), sharex: ax1);

        Assert.Same(ax1, ax2.ShareXWith);
    }
}
