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
    public Ema(double[] prices, int period) : base(prices, Spec(period))
    {
        _period = period;
    }

    internal static IndicatorSpec Spec(int period) => IndicatorSpec.OfWindow("EMA", period, period - 1);

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        var result = new double[ValuedBars];
        var step = new EmaStep(_period);
        for (int i = 0; i < result.Length; i++)
            result[i] = step.Next(Prices[i]);
        return result;
    }

    /// <summary>The values from the first value on, the bars before it left out: what an indicator reads that takes the mean of a
    /// series of its own (the signal of a MACD, over its line).</summary>
    /// <returns>One value per bar from the first value on.</returns>
    internal double[] ComputeFromFirstValue() => FoldPrices(new EmaStep(_period));

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotLine(axes, Compute());
}
