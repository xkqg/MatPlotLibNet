// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Indicators;

/// <summary>Stochastic Oscillator indicator. Compares closing price to the high-low range over a lookback period.</summary>
/// <remarks>Produces two lines: %K (fast) and %D (slow, SMA of %K). Values range 0-100.
/// Above 80 is typically overbought; below 20 is oversold. Best placed in a separate subplot.</remarks>
public sealed class Stochastic : Indicator
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
    public override void Apply(Axes axes)
    {
        var (k, d) = Compute(_high, _low, _close, _kPeriod, _dPeriod);
        int kOffset = _kPeriod - 1;
        int dOffset = kOffset + _dPeriod - 1;

        // %K line
        var kX = new double[k.Length];
        for (int i = 0; i < k.Length; i++) kX[i] = kOffset + i;
        var kSeries = axes.Plot(kX, k);
        kSeries.Label = Label;
        if (Color.HasValue) kSeries.Color = Color.Value;
        kSeries.LineWidth = LineWidth;

        // %D line
        var dX = new double[d.Length];
        for (int i = 0; i < d.Length; i++) dX[i] = dOffset + i;
        var dSeries = axes.Plot(dX, d);
        dSeries.Label = $"%D({_dPeriod})";
        dSeries.Color = DColor ?? Styling.Color.FromHex("#ff7f0e");
        dSeries.LineWidth = LineWidth;
        dSeries.LineStyle = LineStyle.Dashed;

        axes.YAxis.Min = 0;
        axes.YAxis.Max = 100;
    }

    /// <summary>Computes the Stochastic %K and %D values.</summary>
    /// <returns>Tuple of (%K array, %D array).</returns>
    public static (double[] K, double[] D) Compute(double[] high, double[] low, double[] close, int kPeriod = 14, int dPeriod = 3)
    {
        int n = close.Length;
        if (n < kPeriod) return ([], []);

        int kLen = n - kPeriod + 1;
        var k = new double[kLen];

        for (int i = 0; i < kLen; i++)
        {
            double highest = double.MinValue, lowest = double.MaxValue;
            for (int j = i; j < i + kPeriod; j++)
            {
                if (high[j] > highest) highest = high[j];
                if (low[j] < lowest) lowest = low[j];
            }
            double range = highest - lowest;
            k[i] = range > 0 ? (close[i + kPeriod - 1] - lowest) / range * 100 : 50;
        }

        var d = Sma.Compute(k, dPeriod);
        return (k, d);
    }
}
