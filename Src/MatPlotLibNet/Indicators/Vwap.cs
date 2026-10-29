// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>Volume-Weighted Average Price indicator. Computes the cumulative average price weighted by volume, from the first bar
/// or from an anchor bar.</summary>
/// <remarks>VWAP is an intraday benchmark used by institutional traders. The value at each point
/// represents the average price paid per share up to that point, weighted by volume. Anchored at a bar (a swing, a gap, an
/// earnings day) it is the average price paid since that bar, and there is no value before it.</remarks>
public sealed class Vwap : PriceIndicator<SignalResult>
{
    private readonly double[] _volumes;
    private readonly int _anchor;

    /// <summary>Creates a new VWAP indicator from the first bar.</summary>
    /// <param name="prices">The price data (typically the typical price: (H+L+C)/3).</param>
    /// <param name="volumes">The volume data for each period, one per price.</param>
    /// <exception cref="ArgumentException">The prices and the volumes differ in number.</exception>
    public Vwap(double[] prices, double[] volumes) : base(prices)
    {
        prices.ThrowIfMisaligned(volumes);
        _volumes = volumes;
        Label = "VWAP";
    }

    /// <summary>Creates a new VWAP indicator over candles, from the first bar or from an anchor bar: the price of a bar is its
    /// typical price, (high + low + close) / 3, and the average is weighted by its volume. Before the anchor the indicator has
    /// no value (NaN); at the anchor it is that bar's own typical price (the price, when the bar has no volume).</summary>
    /// <param name="bars">High, Low, Close and Volume; built via <see cref="OhlcvBars.Hlcv"/>.</param>
    /// <param name="anchor">The index of the bar the average starts at, from 0 (the first bar, the default) to the last bar.</param>
    /// <exception cref="ArgumentException">The arrays of <paramref name="bars"/> differ in number.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="anchor"/> is below 0 or not before the end of the bars.</exception>
    public Vwap(OhlcvBars bars, int anchor = 0) : base(TypicalPrices(bars))
    {
        if (anchor < 0 || anchor >= Prices.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(anchor), anchor, $"The anchor is the index of a bar, from 0 to {Prices.Length - 1}.");
        }

        _volumes = bars.Volume;
        _anchor = anchor;
        Label = "VWAP";
    }

    /// <summary>The typical price of each bar, after checking that the arrays pair up.</summary>
    private static double[] TypicalPrices(OhlcvBars bars)
    {
        bars.High.ThrowIfMisaligned(bars.Low);
        bars.High.ThrowIfMisaligned(bars.Close);
        bars.High.ThrowIfMisaligned(bars.Volume);
        return [.. bars.High.Select((high, i) => PriceSources.TypicalPrice(high, bars.Low[i], bars.Close[i]))];
    }

    /// <inheritdoc />
    /// <remarks>One value for each bar; the bars before the anchor are NaN.</remarks>
    public override SignalResult Compute()
    {
        int n = Prices.Length;
        var result = new double[n];
        Array.Fill(result, double.NaN, 0, _anchor);
        var mean = new VolumeWeightedMean();
        for (int i = _anchor; i < n; i++)
        {
            mean.Add(Prices[i], _volumes[i]);
            result[i] = mean.Value(Prices[i]);
        }
        return result;
    }

    /// <inheritdoc />
    /// <remarks>The line is drawn from the anchor bar on: the bars before it have no value and are not drawn.</remarks>
    public override void Apply(Axes axes) => PlotSignal(axes, Compute().Values[_anchor..], _anchor);
}
