// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>Simple Moving Average indicator. Computes the unweighted mean of the last N prices.</summary>
public sealed class Sma : PriceIndicator<SignalResult>
{
    private readonly int _period;

    /// <summary>Creates a new SMA indicator from a price array.</summary>
    public Sma(double[] prices, int period) : base(prices, Spec(period))
    {
        _period = period;
    }

    internal static IndicatorSpec Spec(int period) => IndicatorSpec.OfWindow("SMA", period, period - 1);

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        return Prices.RollingMean(_period);
    }

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotLine(axes, Compute());
}
