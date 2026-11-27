// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Whether a series and an axes ask for the axes to be scaled under the matplotlib themes (<see cref="AutoscaleAskExtensions"/>):
/// a series that holds data asks, one with no data asks as its function does, one drawn without axes never asks; an axes asks along one
/// axis through its own series and those of the axes that share that scale, each axes once.</summary>
public class AutoscaleAskExtensionsTests
{
    [Fact]
    public void ASeries_AsksAsItsDataAndItsFunctionSay()
    {
        Assert.True(new ScatterSeries([1.0], [2.0]).AsksForTheAutoscale());
        Assert.True(new ScatterSeries([], []).AsksForTheAutoscale());
        Assert.False(new RugplotSeries(new Vec([])).AsksForTheAutoscale());
        Assert.True(new RugplotSeries(new Vec([1.0])).AsksForTheAutoscale());
        Assert.False(new ProgressBarSeries(0.5).AsksForTheAutoscale());
        Assert.True(new ForeignSeries().AsksForTheAutoscale());
    }

    [Fact]
    public void AnAxes_AsksThroughItsOwnSeries()
    {
        var asks = new Axes();
        asks.AddSeries(new ScatterSeries([1.0], [2.0]));
        var none = new Axes();
        none.AddSeries(new RugplotSeries(new Vec([])));

        Assert.True(asks.AsksForTheAutoscaleAlong(isX: true));
        Assert.False(none.AsksForTheAutoscaleAlong(isX: true));
        Assert.False(new Axes().AsksForTheAutoscaleAlong(isX: false));
    }

    /// <summary>An axes asks along a scale it shares when an axes down the chain of that scale holds a series that asks, and only along that
    /// scale; a chain that turns back on itself is walked once.</summary>
    [Fact]
    public void AnAxes_AsksThroughTheAxesThatShareTheScale_EachOnce()
    {
        var holder = new Axes();
        holder.AddSeries(new ScatterSeries([1.0], [2.0]));
        var middle = new Axes { ShareXWith = holder };
        var empty = new Axes { ShareXWith = middle };
        var first = new Axes();
        var second = new Axes { ShareYWith = first };
        first.ShareYWith = second;

        Assert.True(empty.AsksForTheAutoscaleAlong(isX: true));
        Assert.False(empty.AsksForTheAutoscaleAlong(isX: false));
        Assert.False(first.AsksForTheAutoscaleAlong(isX: false));
    }
}
