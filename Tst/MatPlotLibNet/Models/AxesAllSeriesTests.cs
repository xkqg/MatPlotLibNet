// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models;

/// <summary>An axes holds three series lists — primary, secondary-Y, secondary-X — and every consumer that had
/// to say "everything drawn on this subplot" answered differently: one list, two lists, never three. One member
/// answers it now, in draw order. (<c>IAxesContext.AllSeries</c> keeps its narrower, per-axis meaning: the series a
/// stacked bar sums over; the two are documented against each other.)</summary>
public class AxesAllSeriesTests
{
    [Fact]
    public void AllSeries_EmptyInput_IsEmpty()
    {
        Assert.Empty(new Axes().AllSeries);
    }

    [Fact]
    public void AllSeries_WalksPrimaryThenSecondaryYThenSecondaryX()
    {
        var axes = new Axes();
        var primary = axes.Plot([1.0], [1.0]);
        var secondaryY = axes.AddSecondarySeries(new LineSeries([1.0], [2.0]));
        var secondaryX = axes.PlotXSecondary([1.0], [3.0]);

        Assert.Equal([primary, secondaryY, secondaryX], axes.AllSeries.ToArray());
    }

    [Fact]
    public void AllSeries_SinglePoint_OnePrimarySeriesOnly()
    {
        var axes = new Axes();
        var only = axes.Plot([1.0], [1.0]);

        Assert.Equal([only], axes.AllSeries.ToArray());
    }
}
