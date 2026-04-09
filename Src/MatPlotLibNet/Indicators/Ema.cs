// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>Exponential Moving Average indicator. Gives more weight to recent prices via a smoothing multiplier.</summary>
public sealed class Ema : Indicator<SignalResult>
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
    public override SignalResult Compute()
    {
        if (_prices.Length < _period) return Array.Empty<double>();
        var result = new double[_prices.Length];
        double multiplier = 2.0 / (_period + 1);
        double sum = 0;
        for (int i = 0; i < _period; i++) { result[i] = double.NaN; sum += _prices[i]; }
        result[_period - 1] = sum / _period;
        for (int i = _period; i < _prices.Length; i++)
            result[i] = (_prices[i] - result[i - 1]) * multiplier + result[i - 1];
        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        var ema = Compute();
        var x = ApplyOffset(VectorMath.Linspace(ema.Length, 0.0));
        var series = axes.Plot(x, ema);
        series.Label = Label;
        if (Color.HasValue) series.Color = Color.Value;
        series.LineWidth = LineWidth;
        series.LineStyle = LineStyle;
    }
}
