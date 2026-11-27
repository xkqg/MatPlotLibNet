// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A run of ticks carries the edge it lies on (<see cref="TickRun.Edge"/>) and asks its placement along that edge itself:
/// where a tick stands, whether the plot holds any, the scale and the view it is drawn over and the minor ticks that scale places.</summary>
public class TickRunTests
{
    private static readonly Rect Plot = new(0, 0, 400, 300);

    [Fact]
    public void ATick_StandsAlongTheRunsEdge_AnXOnATopOrBottomEdge_AYOnALeftOrRightOne()
    {
        Assert.Equal(200.0, new TickRun([1.0], Plot.Linear(xMax: 2, yMax: 2), EdgeTerms.Bottom).PlaceOnPlot(1.0));
        Assert.Equal(200.0, new TickRun([1.0], Plot.Linear(xMax: 2, yMax: 2), EdgeTerms.Top).PlaceOnPlot(1.0));
        Assert.Equal(150.0, new TickRun([1.0], Plot.Linear(xMax: 2, yMax: 2), EdgeTerms.Left).PlaceOnPlot(1.0));
        Assert.Equal(150.0, new TickRun([1.0], Plot.Linear(xMax: 2, yMax: 2), EdgeTerms.Right).PlaceOnPlot(1.0));
    }

    [Fact]
    public void ARun_HoldsATick_WhenThePlotHoldsAnyOfIts()
    {
        Assert.True(new TickRun([5.0, 1.0], Plot.Linear(xMax: 2, yMax: 2), EdgeTerms.Bottom).HoldsAny());
        Assert.False(new TickRun([5.0, -3.0], Plot.Linear(xMax: 2, yMax: 2), EdgeTerms.Bottom).HoldsAny());
        Assert.False(new TickRun([], Plot.Linear(xMax: 2, yMax: 2), EdgeTerms.Left).HoldsAny());
    }

    [Fact]
    public void TheScaleAndTheView_AreThoseOfThePlacementAlongTheRunsEdge()
    {
        var mixed = Plot.LogX(yMax: 2, xMax: 1000);

        Assert.Same(ScaleRule.Log, new TickRun([], mixed, EdgeTerms.Bottom).Scale);
        Assert.Same(ScaleRule.Linear, new TickRun([], mixed, EdgeTerms.Left).Scale);
        Assert.Equal(new Range1D(1, 1000), new TickRun([], mixed, EdgeTerms.Top).View);
        Assert.Equal(new Range1D(0, 2), new TickRun([], mixed, EdgeTerms.Right).View);
    }

    /// <summary>A log scale places its minor ticks (matplotlib's <c>LogLocator(subs='auto')</c>, the run's ticks left out); a linear
    /// one places none, and the minor ticks split the major step instead.</summary>
    [Fact]
    public void TheMinorTicksOfALogRun_AreTheOnesItsScalePlaces_ALinearRunHasNone()
    {
        var minors = new TickRun([1.0, 10.0, 100.0, 1000.0], Plot.LogLog(1000, 1000), EdgeTerms.Left).ScaleMinors(13, Theme.MatplotlibV2);

        Assert.NotNull(minors);
        Assert.Contains(2.0, minors);
        Assert.Contains(500.0, minors);
        Assert.DoesNotContain(10.0, minors);
        Assert.Null(new TickRun([0.0, 1.0, 2.0], Plot.Linear(xMax: 2, yMax: 2), EdgeTerms.Left).ScaleMinors(13, Theme.MatplotlibV2));
    }
}
