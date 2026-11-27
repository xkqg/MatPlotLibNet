// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>A candle indicator hands its subclasses the true range of the bars after the first.</summary>
public sealed class CandleIndicatorTests
{
    private sealed record Ranges(double[] Values) : IIndicatorResult;

    private sealed class RangeProbe(OhlcvBars bars) : CandleIndicator<Ranges>(bars)
    {
        public override Ranges Compute() => new(ComputeTrueRange());

        public override void Apply(Axes axes) { }
    }

    [Fact]
    public void TheTrueRange_IsTheWidestOfTheBarsRangeAndItsGapFromTheCloseBefore()
    {
        double[] high = [10, 12, 11];
        double[] low = [8, 9, 6];
        double[] close = [9, 11, 7];

        var ranges = new RangeProbe(OhlcvBars.Hlc(high, low, close)).Compute().Values;

        // bar 1: high 12, low 9, previous close 9 -> 3; bar 2: high 11, low 6, previous close 11 -> 5
        Assert.Equal([3.0, 5.0], ranges);
    }

    /// <summary>Over no bar there is no pair of bars, so no true range: nothing, as every indicator gives over no bar, not a refusal.</summary>
    [Fact]
    public void OverNoBar_TheTrueRangeIsEmpty()
    {
        var ranges = new RangeProbe(OhlcvBars.Hlc([], [], [])).Compute().Values;

        Assert.Empty(ranges);
    }
}
