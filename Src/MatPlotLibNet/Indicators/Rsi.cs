// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>Relative Strength Index indicator. Measures momentum on a 0-100 scale.</summary>
/// <remarks>RSI above 70 is typically considered overbought; below 30 is oversold.
/// Best placed in a separate subplot with <c>AxHLine(70)</c> and <c>AxHLine(30)</c> reference lines.</remarks>
public sealed class Rsi : Indicator<SignalResult>
{
    private readonly double[] _prices;
    private readonly int _period;

    /// <summary>Creates a new RSI indicator.</summary>
    /// <param name="prices">The price data (typically close prices).</param>
    /// <param name="period">The lookback period (default 14).</param>
    public Rsi(double[] prices, int period = 14)
    {
        _prices = prices;
        _period = period;
        Label = $"RSI({period})";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        if (_prices.Length <= _period) return Array.Empty<double>();
        int n = _prices.Length - _period;
        var result = new double[n];

        double avgGain = 0, avgLoss = 0;
        for (int i = 1; i <= _period; i++)
        {
            double change = _prices[i] - _prices[i - 1];
            if (change > 0) avgGain += change;
            else avgLoss -= change;
        }
        avgGain /= _period;
        avgLoss /= _period;

        result[0] = avgLoss == 0 ? 100 : 100 - 100 / (1 + avgGain / avgLoss);

        for (int i = 1; i < n; i++)
        {
            double change = _prices[_period + i] - _prices[_period + i - 1];
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
        double[] rsi = Compute();
        var x = new double[rsi.Length];
        for (int i = 0; i < rsi.Length; i++) x[i] = _period + i;
        var series = axes.Plot(x, rsi);
        series.Label = Label;
        if (Color.HasValue) series.Color = Color.Value;
        series.LineWidth = LineWidth;
        axes.YAxis.Min = 0;
        axes.YAxis.Max = 100;
    }
}
