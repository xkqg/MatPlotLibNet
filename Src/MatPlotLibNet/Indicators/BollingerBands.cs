// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Indicators;

/// <summary>Bollinger Bands indicator. Plots upper and lower bands at N standard deviations from a moving average.</summary>
public sealed class BollingerBands : Indicator<(double[] Middle, double[] Upper, double[] Lower)>
{
    private readonly double[] _prices;
    private readonly int _period;
    private readonly double _stdDevMultiplier;

    /// <summary>Gets or sets the fill opacity for the band region.</summary>
    public double Alpha { get; set; } = 0.15;

    /// <summary>Creates a new Bollinger Bands indicator from a price array.</summary>
    public BollingerBands(double[] prices, int period = 20, double stdDevMultiplier = 2.0)
    {
        _prices = prices;
        _period = period;
        _stdDevMultiplier = stdDevMultiplier;
        Label = $"BB({period},{stdDevMultiplier})";
    }

    /// <summary>Creates a new Bollinger Bands indicator from OHLC data with a selectable price source.</summary>
    public BollingerBands(double[] open, double[] high, double[] low, double[] close, int period = 20, double stdDevMultiplier = 2.0, PriceSource source = PriceSource.Close)
        : this(PriceSources.Resolve(source, open, high, low, close), period, stdDevMultiplier) { }

    /// <inheritdoc />
    public override (double[] Middle, double[] Upper, double[] Lower) Compute() =>
        Compute(_prices, _period, _stdDevMultiplier);

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        var (middle, upper, lower) = Compute();
        var x = new double[middle.Length];
        for (int i = 0; i < middle.Length; i++) x[i] = _period - 1 + i;
        x = ApplyOffset(x);

        var bandColor = Color ?? Styling.Color.FromHex("#1f77b4");
        var fill = axes.FillBetween(x, upper, lower);
        fill.Color = bandColor;
        fill.Alpha = Alpha;
        fill.LineWidth = 0;

        var mid = axes.Plot(x, middle);
        mid.Label = Label;
        mid.Color = bandColor;
        mid.LineWidth = LineWidth;
        mid.LineStyle = LineStyle;
    }

    /// <summary>Computes Bollinger Bands from the given price array.</summary>
    public static (double[] Middle, double[] Upper, double[] Lower) Compute(
        double[] prices, int period, double stdDevMultiplier = 2.0)
    {
        var sma = Sma.Compute(prices, period);
        int n = sma.Length;
        var upper = new double[n];
        var lower = new double[n];
        for (int i = 0; i < n; i++)
        {
            double sumSq = 0;
            for (int j = 0; j < period; j++) { double diff = prices[i + j] - sma[i]; sumSq += diff * diff; }
            double stdDev = Math.Sqrt(sumSq / period);
            upper[i] = sma[i] + stdDevMultiplier * stdDev;
            lower[i] = sma[i] - stdDevMultiplier * stdDev;
        }
        return (sma, upper, lower);
    }
}
