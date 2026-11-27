// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Verifies <see cref="BuySellSignal"/> behavior.</summary>
public class BuySellSignalIndicatorTests
{
    /// <summary>Verifies that Apply adds buy and sell signals to the axes.</summary>
    [Fact]
    public void Apply_AddsSignalsToAxes()
    {
        var axes = new Axes();
        axes.Plot([0, 1, 2, 3, 4], [10, 20, 15, 25, 18]);

        var indicator = new BuySellSignal(
            buyIndices: [0, 2],
            buyPrices: [10, 15],
            sellIndices: [1, 3],
            sellPrices: [20, 25]);
        indicator.Apply(axes);

        Assert.Equal(4, axes.Signals.Count);
        Assert.Equal(2, axes.Signals.Count(s => s.Direction == SignalDirection.Buy));
        Assert.Equal(2, axes.Signals.Count(s => s.Direction == SignalDirection.Sell));
    }
}
