// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>Simple Moving Average indicator. Computes the unweighted mean of the last N prices.</summary>
public sealed class Sma : Indicator<double[]>
{
    private readonly double[] _prices;
    private readonly int _period;

    /// <summary>Creates a new SMA indicator from a price array.</summary>
    public Sma(double[] prices, int period)
    {
        _prices = prices;
        _period = period;
        Label = $"SMA({period})";
    }

    /// <summary>Creates a new SMA indicator from OHLC data with a selectable price source.</summary>
    public Sma(double[] open, double[] high, double[] low, double[] close, int period, PriceSource source = PriceSource.Close)
        : this(PriceSources.Resolve(source, open, high, low, close), period) { }

    /// <inheritdoc />
    public override double[] Compute() => Compute(_prices, _period);

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        var sma = Compute();
        var x = new double[sma.Length];
        for (int i = 0; i < sma.Length; i++) x[i] = _period - 1 + i;
        x = ApplyOffset(x);
        var series = axes.Plot(x, sma);
        series.Label = Label;
        if (Color.HasValue) series.Color = Color.Value;
        series.LineWidth = LineWidth;
        series.LineStyle = LineStyle;
    }

    /// <summary>Computes the Simple Moving Average from the given price array.</summary>
    public static double[] Compute(double[] prices, int period)
    {
        if (prices.Length < period) return [];
        var result = new double[prices.Length - period + 1];
        double sum = 0;
        for (int i = 0; i < period; i++) sum += prices[i];
        result[0] = sum / period;
        for (int i = period; i < prices.Length; i++)
        {
            sum += prices[i] - prices[i - period];
            result[i - period + 1] = sum / period;
        }
        return result;
    }
}
