// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>Adds buy and sell signal markers to the axes at specified indices and prices.</summary>
public sealed class BuySellSignal : Indicator
{
    private readonly int[] _buyIndices;
    private readonly double[] _buyPrices;
    private readonly int[] _sellIndices;
    private readonly double[] _sellPrices;

    /// <summary>Creates a new buy/sell signal indicator.</summary>
    /// <param name="buyIndices">X positions of buy signals.</param>
    /// <param name="buyPrices">Y prices of buy signals.</param>
    /// <param name="sellIndices">X positions of sell signals.</param>
    /// <param name="sellPrices">Y prices of sell signals.</param>
    public BuySellSignal(int[] buyIndices, double[] buyPrices, int[] sellIndices, double[] sellPrices)
    {
        _buyIndices = buyIndices;
        _buyPrices = buyPrices;
        _sellIndices = sellIndices;
        _sellPrices = sellPrices;
        Label = "Signals";
    }

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        for (int i = 0; i < _buyIndices.Length; i++)
            axes.AddSignal(_buyIndices[i], _buyPrices[i], SignalDirection.Buy);

        for (int i = 0; i < _sellIndices.Length; i++)
            axes.AddSignal(_sellIndices[i], _sellPrices[i], SignalDirection.Sell);
    }
}
