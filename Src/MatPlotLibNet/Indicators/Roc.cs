// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>Rate-of-Change indicator: <c>prices[t] / prices[t − lookback] − 1</c>.
/// Returns a proportional (not percentage) value — multiply by 100 for percent.</summary>
/// <remarks>Used internally by <c>RelativeRotationSeries</c> for the ZScore and LogReturn
/// RS-Momentum pipelines; also available as a standalone Tier-3 indicator.</remarks>
public sealed class Roc : PriceIndicator<SignalResult>
{
    private readonly int _lookback;

    /// <summary>Creates a new ROC indicator from a price array.</summary>
    public Roc(double[] prices, int lookback) : base(prices)
    {
        _lookback = lookback;
        Label = $"ROC({lookback})";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        if (Prices.Length <= _lookback) return Array.Empty<double>();

        int outLen = Prices.Length - _lookback;
        var result = new double[outLen];
        for (int i = 0; i < outLen; i++)
        {
            double prev = Prices[i];
            result[i] = prev == 0.0 ? double.NaN : Prices[i + _lookback] / prev - 1.0;
        }
        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), _lookback);
}
