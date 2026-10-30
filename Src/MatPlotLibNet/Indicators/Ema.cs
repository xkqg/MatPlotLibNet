// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
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
        var step = new EmaStep(_period);
        for (int i = 0; i < Prices.Length; i++)
            result[i] = step.Next(Prices[i]);
        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), 0);
}
