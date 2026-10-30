// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Data;

/// <summary>A single Open-High-Low-Close bar that also says how much traded. It is what an indicator that reads the volume
/// (on-balance volume, VWAP, money flow) is fed bar by bar; <see cref="OhlcBar"/> cannot carry it.</summary>
/// <remarks>A bar that arrives without a volume carries NaN, never 0: a volume of 0 is a fact (nothing traded), NaN is the
/// absence of one, and an indicator answers the two differently. Two bars without a volume are equal.</remarks>
/// <param name="Open">Opening price.</param>
/// <param name="High">Highest price in the period.</param>
/// <param name="Low">Lowest price in the period.</param>
/// <param name="Close">Closing price.</param>
/// <param name="Volume">Quantity traded in the period; NaN when the bar came without one.</param>
public readonly record struct OhlcvBar(double Open, double High, double Low, double Close, double Volume = double.NaN)
{
    /// <summary>The bar of an <see cref="OhlcBar"/>: the same prices, and no volume (NaN).</summary>
    /// <param name="bar">The prices.</param>
    public static OhlcvBar From(OhlcBar bar) => new(bar.Open, bar.High, bar.Low, bar.Close);

    /// <summary>The prices of this bar, without the volume.</summary>
    public OhlcBar Ohlc => new(Open, High, Low, Close);
}
