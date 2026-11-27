// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models;

/// <summary>The line and scatter verbs of one Y axis slot, the one place that checks the two data arrays line up, makes the
/// series and puts it on the slot. <c>Axes.Plot</c> and <c>Axes.Scatter</c> are these verbs over the primary slot, the
/// obsolete secondary verbs over the first right slot, and the slot builder over its own.</summary>
public class YAxisSlotSeriesVerbsTests
{
    private static readonly double[] X = [1.0, 2.0, 3.0];
    private static readonly double[] Y = [4.0, 5.0, 6.0];

    [Fact]
    public void Plot_AddsALineSeriesToThatSlotOnly()
    {
        var axes = new Axes();
        var right = axes.YAxes.Right.Add();

        var line = right.Plot(X, Y);

        Assert.Same(line, Assert.Single(right.Series));
        Assert.Same(X, line.XData);
        Assert.Same(Y, line.YData);
        Assert.Empty(axes.Series);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PlotAndScatter_RefuseDataThatDoesNotLineUp_AndAddNothing(bool plot)
    {
        var slot = new Axes().YAxes.Right.Add();

        Assert.Throws<ArgumentException>(() => { if (plot) slot.Plot(X, [1.0]); else slot.Scatter(X, [1.0]); });

        Assert.Empty(slot.Series);
    }

    [Fact]
    public void ThePrimaryVerbs_AreTheSlotVerbsOverTheLeftSlot()
    {
        var axes = new Axes();

        var line = axes.Plot(X, Y);
        var points = axes.Scatter(X, Y);

        Assert.Equal<ISeries>([line, points], axes.YAxes.Left[0].Series);
    }
}
