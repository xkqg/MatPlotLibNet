// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>Exponential Moving Average indicator. Gives more weight to recent prices via a smoothing multiplier.</summary>
public sealed class Ema : Indicator<double[]>
{
    private readonly double[] _prices;
    private readonly int _period;

    /// <summary>Creates a new EMA indicator from a price array.</summary>
    public Ema(double[] prices, int period)
    {
        _prices = prices;
        _period = period;
        Label = $"EMA({period})";
    }

    /// <summary>Creates a new EMA indicator from OHLC data with a selectable price source.</summary>
    public Ema(double[] open, double[] high, double[] low, double[] close, int period, PriceSource source = PriceSource.Close)
        : this(PriceSources.Resolve(source, open, high, low, close), period) { }

    /// <inheritdoc />
    public override double[] Compute() => Compute(_prices, _period);

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        var ema = Compute();
        var x = new double[ema.Length];
        for (int i = 0; i < ema.Length; i++) x[i] = i;
        x = ApplyOffset(x);
        var series = axes.Plot(x, ema);
        series.Label = Label;
        if (Color.HasValue) series.Color = Color.Value;
        series.LineWidth = LineWidth;
        series.LineStyle = LineStyle;
    }

    /// <summary>Computes the Exponential Moving Average. The first <paramref name="period"/> values use SMA as seed.</summary>
    public static double[] Compute(double[] prices, int period)
    {
        if (prices.Length < period) return [];
        var result = new double[prices.Length];
        double multiplier = 2.0 / (period + 1);
        double sum = 0;
        for (int i = 0; i < period; i++) { result[i] = double.NaN; sum += prices[i]; }
        result[period - 1] = sum / period;
        for (int i = period; i < prices.Length; i++)
            result[i] = (prices[i] - result[i - 1]) * multiplier + result[i - 1];
        return result;
    }
}
