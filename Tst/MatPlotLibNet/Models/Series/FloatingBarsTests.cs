// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Bars that float: each has a place of its own along X, a foot and a height, and a name; together they are one bar
/// series of one width and one colour. A Renko brick and the mark of a calendar event are both such a bar.</summary>
public class FloatingBarsTests
{
    [Fact]
    public void TheBars_BecomeOneSeries_WithTheirNamesPlacesFeetAndHeights()
    {
        var bars = new FloatingBars(0.4, Colors.Green);
        bars.Add("a", 0.5, 10, 2);
        bars.Add("b", 1.5, 12, 3);

        var series = bars.ToSeries("up");

        Assert.Equal(["a", "b"], series.Categories);
        Assert.Equal([2.0, 3.0], series.Values);
        Assert.Equal([0.5, 1.5], series.XCoordinate!);
        Assert.Equal([10.0, 12.0], series.StackBaseline!);
        Assert.Equal(0.4, series.BarWidth);
        Assert.Equal(Colors.Green, series.Color);
        Assert.Equal("up", series.Label);
    }

    [Fact]
    public void ABarGivenNoColourOrNoLabel_LeavesBothUnset()
    {
        var bars = new FloatingBars(1, null);
        bars.Add("x", 0.5, 0, 1);

        var series = bars.ToSeries(null);

        Assert.Null(series.Color);
        Assert.Null(series.Label);
    }

    [Fact]
    public void NoBars_AreAnEmptySeries_ThatKeepsItsWidthAndColour()
    {
        var series = new FloatingBars(1, Colors.Red).ToSeries("down");

        Assert.Empty(series.Categories);
        Assert.Empty(series.Values);
        Assert.Equal(1.0, series.BarWidth);
        Assert.Equal(Colors.Red, series.Color);
        Assert.Equal("down", series.Label);
    }

    [Fact]
    public void TheSeries_HoldsItsOwnArrays_SoAnotherBarAddedLaterDoesNotMoveIt()
    {
        var bars = new FloatingBars(1, null);
        bars.Add("x", 0.5, 0, 1);
        var first = bars.ToSeries(null);

        bars.Add("y", 1.5, 1, 1);

        Assert.Single(first.Categories);
        Assert.Equal(2, bars.ToSeries(null).Categories.Length);
    }
}
