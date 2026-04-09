// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Indicators;

/// <summary>Bollinger Bands indicator. Plots upper and lower bands at N standard deviations from a moving average.</summary>
public sealed class BollingerBands : Indicator<BandsResult>
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
    public override BandsResult Compute()
    {
        double[] sma = new Sma(_prices, _period).Compute();
        int n = sma.Length;
        var stdDev = new double[n];
        VectorMath.RollingStdDev(_prices, _period, sma, stdDev);
        // upper = sma + mult*stdDev;  lower = sma - mult*stdDev
        var scaledStd = new double[n];
        VectorMath.Multiply(stdDev, _stdDevMultiplier, scaledStd);
        var upper = new double[n];
        var lower = new double[n];
        VectorMath.Add(sma, scaledStd, upper);
        VectorMath.Subtract(sma, scaledStd, lower);
        return new BandsResult(sma, upper, lower);
    }

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        var result = Compute();
        var middle = result.Middle;
        var upper = result.Upper;
        var lower = result.Lower;
        var x = ApplyOffset(VectorMath.Linspace(middle.Length, _period - 1));

        var bandColor = Color ?? Colors.Tab10Blue;
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
}
