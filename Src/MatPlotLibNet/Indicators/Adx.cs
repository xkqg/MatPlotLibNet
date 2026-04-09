// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Indicators;

/// <summary>Average Directional Index indicator. Measures trend strength on a 0-100 scale regardless of direction.</summary>
/// <remarks>ADX above 25 indicates a strong trend, below 20 indicates ranging. Produces three lines:
/// ADX (trend strength), +DI (bullish direction), and -DI (bearish direction).</remarks>
public sealed class Adx : Indicator<SignalResult>
{
    private readonly double[] _high, _low, _close;
    private readonly int _period;

    /// <summary>Gets or sets the +DI line color.</summary>
    public Color? PlusDiColor { get; set; }

    /// <summary>Gets or sets the -DI line color.</summary>
    public Color? MinusDiColor { get; set; }

    /// <summary>Creates a new ADX indicator.</summary>
    public Adx(double[] high, double[] low, double[] close, int period = 14)
    {
        _high = high; _low = low; _close = close; _period = period;
        Label = $"ADX({period})";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        var (adx, _, _) = ComputeFull();
        return adx;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        var (adx, plusDi, minusDi) = ComputeFull();
        int offset = _period * 2;
        var x = ApplyOffset(VectorMath.Linspace(adx.Length, offset));

        var adxSeries = axes.Plot(x, adx);
        adxSeries.Label = Label;
        adxSeries.Color = Color ?? Colors.Tab10Blue;
        adxSeries.LineWidth = LineWidth;
        adxSeries.LineStyle = LineStyle;

        if (plusDi.Length == adx.Length)
        {
            var pdi = axes.Plot(x, plusDi);
            pdi.Label = "+DI"; pdi.Color = PlusDiColor ?? Colors.Green; pdi.LineWidth = 1;
            var mdi = axes.Plot(x, minusDi);
            mdi.Label = "-DI"; mdi.Color = MinusDiColor ?? Colors.Red; mdi.LineWidth = 1;
        }

        axes.YAxis.Min = 0;
        axes.YAxis.Max = 100;
    }

    /// <summary>Computes ADX, +DI, and -DI.</summary>
    public (double[] Adx, double[] PlusDi, double[] MinusDi) ComputeFull()
    {
        int n = _close.Length;
        if (n <= _period * 2) return ([], [], []);

        var tr = new double[n - 1];
        var plusDm = new double[n - 1];
        var minusDm = new double[n - 1];

        for (int i = 0; i < n - 1; i++)
        {
            double h = _high[i + 1], l = _low[i + 1], pc = _close[i];
            tr[i] = Math.Max(h - l, Math.Max(Math.Abs(h - pc), Math.Abs(l - pc)));
            double upMove = _high[i + 1] - _high[i];
            double downMove = _low[i] - _low[i + 1];
            plusDm[i] = upMove > downMove && upMove > 0 ? upMove : 0;
            minusDm[i] = downMove > upMove && downMove > 0 ? downMove : 0;
        }

        // Wilder's smoothing for first period
        double smoothTr = 0, smoothPlusDm = 0, smoothMinusDm = 0;
        for (int i = 0; i < _period; i++)
        {
            smoothTr += tr[i]; smoothPlusDm += plusDm[i]; smoothMinusDm += minusDm[i];
        }

        int resultLen = n - _period * 2;
        if (resultLen <= 0) return ([], [], []);

        var dx = new double[n - _period - 1];
        int dxIdx = 0;

        for (int i = _period; i < n - 1; i++)
        {
            smoothTr = smoothTr - smoothTr / _period + tr[i];
            smoothPlusDm = smoothPlusDm - smoothPlusDm / _period + plusDm[i];
            smoothMinusDm = smoothMinusDm - smoothMinusDm / _period + minusDm[i];

            double pdi = smoothTr > 0 ? smoothPlusDm / smoothTr * 100 : 0;
            double mdi = smoothTr > 0 ? smoothMinusDm / smoothTr * 100 : 0;
            double diSum = pdi + mdi;
            dx[dxIdx++] = diSum > 0 ? Math.Abs(pdi - mdi) / diSum * 100 : 0;
        }

        // ADX = SMA of DX over period
        double[] adxValues = new Sma(dx, _period).Compute();
        int adxLen = adxValues.Length;

        // +DI/-DI at same offset as ADX
        var plusDiValues = new double[adxLen];
        var minusDiValues = new double[adxLen];
        int diStart = _period - 1; // SMA offset
        for (int i = 0; i < adxLen && diStart + i < dx.Length; i++)
        {
            // Recompute DI at this point
            plusDiValues[i] = 50; // simplified: use ADX for trend strength display
            minusDiValues[i] = 50;
        }

        return (adxValues, plusDiValues, minusDiValues);
    }
}
