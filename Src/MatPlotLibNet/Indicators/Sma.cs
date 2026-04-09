// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>Simple Moving Average indicator. Computes the unweighted mean of the last N prices.</summary>
public sealed class Sma : Indicator<SignalResult>
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
    public override SignalResult Compute()
    {
        if (_prices.Length < _period) return Array.Empty<double>();
        return VectorMath.RollingMean(_prices, _period);
    }

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        var sma = Compute();
        var x = ApplyOffset(VectorMath.Linspace(sma.Length, _period - 1));
        var series = axes.Plot(x, sma);
        series.Label = Label;
        if (Color.HasValue) series.Color = Color.Value;
        series.LineWidth = LineWidth;
        series.LineStyle = LineStyle;
    }
}
