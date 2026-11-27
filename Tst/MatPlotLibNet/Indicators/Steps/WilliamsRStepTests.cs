// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;

namespace MatPlotLibNet.Tests.Indicators.Steps;

/// <summary>Williams %R one bar at a time: where the close stands in the range of the last <c>period</c> highs and lows, from 0 at the
/// highest high to -100 at the lowest low, and -50 for a range of none. The batch <c>WilliamsR</c> folds it over its bars and
/// <c>StreamingWilliamsR</c> takes a bar at a time, so the formula exists here only.</summary>
public sealed class WilliamsRStepTests
{
    /// <summary>A step of the given period that has taken each bar (high, low, close).</summary>
    private static WilliamsRStep Fed(int period, params double[][] bars)
    {
        var step = new WilliamsRStep(period);
        foreach (var bar in bars) step.Next(bar[0], bar[1], bar[2]);
        return step;
    }

    [Fact]
    public void BeforePeriodBars_TheStepSaysNaN()
    {
        var step = new WilliamsRStep(3);

        Assert.True(double.IsNaN(step.Next(12, 8, 10)));
        Assert.True(double.IsNaN(step.Next(13, 9, 11)));
    }

    [Fact]
    public void FromThePeriodthBar_TheCloseIsPlacedInTheRangeOfTheLastHighsAndLows()
    {
        var step = Fed(3, [12, 8, 10], [13, 9, 11]);

        // highest high 14, lowest low 8: (14 - 12) / (14 - 8) * -100
        Assert.Equal(-100.0 / 3, step.Next(14, 10, 12), 12);
    }

    [Fact]
    public void ABarThatHasLeftTheWindow_NoLongerSetsTheRange()
    {
        var step = Fed(2, [20, 1, 10], [12, 8, 10]);

        // the first bar is out of the window: range 14 - 8 = 6, close 13: (14 - 13) / 6 * -100
        Assert.Equal(-100.0 / 6, step.Next(14, 9, 13), 12);
    }

    [Fact]
    public void ARangeOfNone_IsMinusFifty()
    {
        var step = new WilliamsRStep(2);
        step.Next(10, 10, 10);

        Assert.Equal(-50.0, step.Next(10, 10, 10));
    }
}
