// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Indicators;

/// <summary>Stochastic Oscillator indicator. Compares closing price to the high-low range over a lookback period.</summary>
/// <remarks>Produces two lines: %K (fast) and %D (slow, SMA of %K). Values range 0-100.
/// Above 80 is typically overbought; below 20 is oversold. Best placed in a separate subplot.</remarks>
public sealed class Stochastic : Indicator<StochasticResult>
{
    private readonly double[] _high;
    private readonly double[] _low;
    private readonly double[] _close;
    private readonly int _kPeriod;
    private readonly int _dPeriod;

    /// <summary>Gets or sets the %D line color.</summary>
    public Color? DColor { get; set; }

    /// <summary>Creates a new Stochastic Oscillator indicator.</summary>
    /// <param name="high">The high price data.</param>
    /// <param name="low">The low price data.</param>
    /// <param name="close">The close price data.</param>
    /// <param name="kPeriod">The %K lookback period (default 14).</param>
    /// <param name="dPeriod">The %D smoothing period (default 3).</param>
    public Stochastic(double[] high, double[] low, double[] close, int kPeriod = 14, int dPeriod = 3)
    {
        _high = high;
        _low = low;
        _close = close;
        _kPeriod = kPeriod;
        _dPeriod = dPeriod;
        Label = $"%K({kPeriod})";
    }

    /// <inheritdoc />
    public override StochasticResult Compute()
    {
        int n = _close.Length;
        if (n < _kPeriod) return new StochasticResult([], []);

        int kLen = n - _kPeriod + 1;
        var k = new double[kLen];

        // O(n) monotone-deque rolling max/min (replaces O(n×kPeriod) nested loop)
        var highBuf = new double[n];
        var lowBuf = new double[n];
        VectorMath.RollingMax(_high, _kPeriod, highBuf);
        VectorMath.RollingMin(_low, _kPeriod, lowBuf);

        for (int i = 0; i < kLen; i++)
        {
            double highest = highBuf[i + _kPeriod - 1];
            double lowest = lowBuf[i + _kPeriod - 1];
            double range = highest - lowest;
            k[i] = range > 0 ? (_close[i + _kPeriod - 1] - lowest) / range * 100 : 50;
        }

        double[] d = new Sma(k, _dPeriod).Compute();
        return new StochasticResult(k, d);
    }

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        var result = Compute();
        var k = result.K;
        var d = result.D;
        int kOffset = _kPeriod - 1;
        int dOffset = kOffset + _dPeriod - 1;

        // %K line
        var kX = VectorMath.Linspace(k.Length, kOffset);
        var kSeries = axes.Plot(kX, k);
        kSeries.Label = Label;
        if (Color.HasValue) kSeries.Color = Color.Value;
        kSeries.LineWidth = LineWidth;

        // %D line
        var dX = VectorMath.Linspace(d.Length, dOffset);
        var dSeries = axes.Plot(dX, d);
        dSeries.Label = $"%D({_dPeriod})";
        dSeries.Color = DColor ?? Colors.Tab10Orange;
        dSeries.LineWidth = LineWidth;
        dSeries.LineStyle = LineStyle.Dashed;

        axes.YAxis.Min = 0;
        axes.YAxis.Max = 100;
    }
}
