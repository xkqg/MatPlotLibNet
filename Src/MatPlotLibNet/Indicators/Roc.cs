// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>Rate-of-Change indicator: <c>prices[t] / prices[t − lookback] − 1</c>.
/// Returns a proportional (not percentage) value — multiply by 100 for percent.</summary>
/// <remarks>The RS-Momentum of a relative rotation graph's ZScore and LogReturn formulas is built on it; it is also an
/// indicator of its own.</remarks>
public sealed class Roc : PriceIndicator<SignalResult>
{
    private readonly int _lookback;

    /// <summary>Creates a new ROC indicator from a price array.</summary>
    public Roc(double[] prices, int lookback) : base(prices, Spec(lookback))
    {
        _lookback = lookback;
    }

    internal static IndicatorSpec Spec(int lookback) => IndicatorSpec.OfWindow("ROC", lookback, lookback);

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int outLen = ValueCount;
        var result = new double[outLen];
        for (int i = 0; i < outLen; i++)
        {
            double prev = Prices[i];
            result[i] = prev == 0.0 ? double.NaN : Prices[i + _lookback] / prev - 1.0;
        }
        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotLine(axes, Compute());
}
