// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>Relative Strength Index indicator. Measures momentum on a 0-100 scale.</summary>
/// <remarks>RSI above 70 is typically considered overbought; below 30 is oversold.
/// Best placed in a separate subplot with <c>AxHLine(70)</c> and <c>AxHLine(30)</c> reference lines.</remarks>
public sealed class Rsi : PriceIndicator<SignalResult>
{
    private readonly int _period;

    /// <summary>Creates a new RSI indicator.</summary>
    /// <param name="prices">The price data (typically close prices).</param>
    /// <param name="period">The lookback period (default 14).</param>
    public Rsi(double[] prices, int period = 14) : base(prices)
    {
        _period = period;
        Label = $"RSI({period})";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        if (Prices.Length <= _period) return Array.Empty<double>();
        int n = Prices.Length - _period;
        var result = new double[n];

        double avgGain = 0, avgLoss = 0;
        for (int i = 1; i <= _period; i++)
        {
            double change = Prices[i] - Prices[i - 1];
            if (change > 0) avgGain += change;
            else avgLoss -= change;
        }
        avgGain /= _period;
        avgLoss /= _period;

        result[0] = avgLoss == 0 ? 100 : 100 - 100 / (1 + avgGain / avgLoss);

        for (int i = 1; i < n; i++)
        {
            double change = Prices[_period + i] - Prices[_period + i - 1];
            double gain = change > 0 ? change : 0;
            double loss = change < 0 ? -change : 0;
            avgGain = (avgGain * (_period - 1) + gain) / _period;
            avgLoss = (avgLoss * (_period - 1) + loss) / _period;
            result[i] = avgLoss == 0 ? 100 : 100 - 100 / (1 + avgGain / avgLoss);
        }
        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        PlotSignal(axes, Compute(), _period);
        axes.YAxis.Min = 0;
        axes.YAxis.Max = 100;
    }
}
