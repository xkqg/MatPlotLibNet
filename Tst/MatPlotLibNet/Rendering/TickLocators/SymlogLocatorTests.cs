// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet.Tests.Rendering.TickLocators;

/// <summary>Verifies <see cref="SymlogLocator"/> behaviour.</summary>
public class SymlogLocatorTests
{
    /// <summary>Zero, the threshold on both sides and its decades, inside the range.</summary>
    [Fact]
    public void Locate_ARangeAroundZero_YieldsZeroTheThresholdAndItsDecades()
    {
        double[] ticks = new SymlogLocator(1).Locate(-50, 50);

        Assert.Equal([-10.0, -1, 0, 1, 10], ticks);
    }

    /// <summary>A range that holds none of the candidate ticks (it lies between two decades of the threshold) has no
    /// ticks; it used to throw, so a symmetric-log axis over such data could not be drawn at all.</summary>
    [Fact]
    public void Locate_ARangeBetweenTwoDecades_YieldsNoTicks()
    {
        double[] ticks = new SymlogLocator(2).Locate(45000, 45400);

        Assert.Empty(ticks);
    }

    /// <summary>The same range as a drawn axis: the figure renders.</summary>
    [Fact]
    public void ASymmetricLogAxisOverDataBetweenTwoDecades_Renders()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2, 3], [45010.0, 45200, 45390])).Build();
        figure.SubPlots[0].YAxis.Scale = AxisScale.SymLog;
        figure.SubPlots[0].YAxis.SymLogLinThresh = 2;

        var thrown = Record.Exception(() => figure.ToSvg());

        Assert.Null(thrown);
    }
}
