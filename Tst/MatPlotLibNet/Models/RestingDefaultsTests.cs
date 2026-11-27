// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models;

/// <summary>The settings an axes, an axis and a figure start with (the legend, the grid, the spines, the 3-D panes, the
/// ticks, the spacing) are records no one can change, so every new axes starts with the same one: a figure is built without
/// making them again, and a change to one axes is a new record on that axes alone.</summary>
public class RestingDefaultsTests
{
    [Fact]
    public void TwoNewAxes_StartWithTheSameSettings()
    {
        var first = new Axes();
        var second = new Axes();

        Assert.Same(first.Legend, second.Legend);
        Assert.Same(first.Grid, second.Grid);
        Assert.Same(first.Spines, second.Spines);
        Assert.Same(first.Pane3D, second.Pane3D);
        Assert.Same(first.XAxis.MajorTicks, second.YAxis.MajorTicks);
        Assert.Same(first.XAxis.MinorTicks, second.YAxis.MinorTicks);
    }

    [Fact]
    public void TwoNewFigures_StartWithTheSameSpacing()
    {
        Assert.Same(new Figure().Spacing, new Figure().Spacing);
    }

    [Fact]
    public void TheSharedSettings_AreTheTypesOwnDefaults()
    {
        var axes = new Axes();

        Assert.Equal(new Legend(), axes.Legend);
        Assert.Equal(new GridStyle(), axes.Grid);
        Assert.Equal(new SpinesConfig(), axes.Spines);
        Assert.Equal(new Pane3DConfig(), axes.Pane3D);
        Assert.Equal(new TickConfig(), axes.XAxis.MajorTicks);
        // matplotlib's minor ticks: xtick.minor.size 2 pt, xtick.minor.width 0.6 pt, through the one rule for a point.
        var minor = axes.XAxis.MinorTicks;
        Assert.Equal(2.0.PointsInPixels, minor.Length);
        Assert.Equal(0.6.PointsInPixels, minor.Width);
        Assert.Null(minor.GivenLength);   // read at rest, not given: a theme may name another
        Assert.Null(minor.GivenWidth);
        Assert.Equal(new SubPlotSpacing(), new Figure().Spacing);
    }

    [Fact]
    public void AChangeToOneAxes_LeavesTheOtherAtRest()
    {
        var changed = new Axes();
        var resting = new Axes();

        changed.Legend = changed.Legend with { Visible = false };
        changed.XAxis.MajorTicks = changed.XAxis.MajorTicks with { Length = 9 };

        Assert.True(resting.Legend.Visible);
        Assert.Equal(new TickConfig().Length, resting.XAxis.MajorTicks.Length);
    }
}
