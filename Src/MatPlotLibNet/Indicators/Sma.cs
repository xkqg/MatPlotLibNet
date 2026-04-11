// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>Simple Moving Average indicator. Computes the unweighted mean of the last N prices.</summary>
public sealed class Sma : PriceIndicator<SignalResult>
{
    private readonly int _period;

    /// <summary>Creates a new SMA indicator from a price array.</summary>
    public Sma(double[] prices, int period) : base(prices)
    {
        _period = period;
        Label = $"SMA({period})";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        if (Prices.Length < _period) return Array.Empty<double>();
        return VectorMath.RollingMean(Prices, _period);
    }

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), _period - 1);
}
