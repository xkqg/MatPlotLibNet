// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
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
        var result = new double[Prices.Length - _period];
        var step = new RsiStep(_period);
        for (int i = 0; i < Prices.Length; i++)
        {
            double value = step.Next(Prices[i]);
            if (i >= _period) result[i - _period] = value;
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
