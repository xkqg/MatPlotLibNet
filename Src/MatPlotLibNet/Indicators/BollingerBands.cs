// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Indicators;

/// <summary>Bollinger Bands indicator. Plots upper and lower bands at N standard deviations from a moving average.</summary>
public sealed class BollingerBands : PriceIndicator<BandsResult>
{
    private readonly int _period;
    private readonly double _stdDevMultiplier;

    public double Alpha { get; set; } = 0.15;

    /// <summary>Creates a new Bollinger Bands indicator from a price array.</summary>
    public BollingerBands(double[] prices, int period = 20, double stdDevMultiplier = 2.0) : base(prices)
    {
        _period = period;
        _stdDevMultiplier = stdDevMultiplier;
        Label = $"BB({period},{stdDevMultiplier})";
    }

    /// <inheritdoc />
    public override BandsResult Compute()
    {
        double[] sma = new Sma(Prices, _period).Compute();
        int n = sma.Length;
        var stdDev = new double[n];
        VectorMath.RollingStdDev(Prices, _period, sma, stdDev);
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
    public override void Apply(Axes axes) => PlotBands(axes, Compute(), _period - 1, Alpha);
}
