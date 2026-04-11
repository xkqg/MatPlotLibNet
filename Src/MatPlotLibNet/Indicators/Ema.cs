// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>Exponential Moving Average indicator. Gives more weight to recent prices via a smoothing multiplier.</summary>
public sealed class Ema : PriceIndicator<SignalResult>
{
    private readonly int _period;

    /// <summary>Creates a new EMA indicator from a price array.</summary>
    public Ema(double[] prices, int period) : base(prices)
    {
        _period = period;
        Label = $"EMA({period})";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        if (Prices.Length < _period) return Array.Empty<double>();
        var result = new double[Prices.Length];
        double multiplier = 2.0 / (_period + 1);
        double sum = 0;
        for (int i = 0; i < _period; i++) { result[i] = double.NaN; sum += Prices[i]; }
        result[_period - 1] = sum / _period;
        for (int i = _period; i < Prices.Length; i++)
            result[i] = (Prices[i] - result[i - 1]) * multiplier + result[i - 1];
        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), 0);
}
