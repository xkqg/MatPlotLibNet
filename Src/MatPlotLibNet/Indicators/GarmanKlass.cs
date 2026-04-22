// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>Garman-Klass volatility estimator. 7.4× more efficient than close-to-close,
/// assumes zero drift. Pure OHLC, no volume needed.</summary>
/// <remarks>σ²_GK(bar) = 0.5·(ln(H/L))² − (2·ln(2) − 1)·(ln(C/O))². Per-window output is
/// <c>√mean(σ²_GK)</c> over the rolling window. Annualization is the caller's responsibility.
/// Reference: Garman &amp; Klass (1980), <i>Journal of Business</i> 53(1).</remarks>
public sealed class GarmanKlass : CandleIndicator<SignalResult>
{
    private static readonly double TwoLn2Minus1 = 2 * Math.Log(2) - 1;

    private readonly int _period;

    /// <summary>Creates a new Garman-Klass volatility indicator.</summary>
    /// <param name="open">Open prices (all strictly positive).</param>
    /// <param name="high">High prices (all strictly positive).</param>
    /// <param name="low">Low prices (all strictly positive).</param>
    /// <param name="close">Close prices (all strictly positive).</param>
    /// <param name="period">Rolling window length in bars (default 20).</param>
    /// <exception cref="ArgumentException">Thrown when any input bar has a non-positive price.</exception>
    public GarmanKlass(double[] open, double[] high, double[] low, double[] close, int period = 20)
        : base(open, high, low, close, [])
    {
        GuardPositive(open, high, low, close);
        _period = period;
        Label = $"GK({period})";
    }

    private static void GuardPositive(double[] o, double[] h, double[] l, double[] c)
    {
        int n = Math.Min(Math.Min(o.Length, h.Length), Math.Min(l.Length, c.Length));
        for (int i = 0; i < n; i++)
        {
            if (o[i] <= 0 || h[i] <= 0 || l[i] <= 0 || c[i] <= 0)
                throw new ArgumentException(
                    $"GarmanKlass requires strictly positive OHLC; bar {i} has non-positive value.");
        }
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int n = BarCount;
        if (n < _period) return Array.Empty<double>();

        // Per-bar σ²_GK
        var sigma2 = new double[n];
        for (int i = 0; i < n; i++)
        {
            double lhl = Math.Log(High[i] / Low[i]);
            double lco = Math.Log(Close[i] / Open[i]);
            sigma2[i] = 0.5 * lhl * lhl - TwoLn2Minus1 * lco * lco;
        }

        int outLen = n - _period + 1;
        var result = new double[outLen];
        double sum = 0;
        for (int i = 0; i < _period; i++) sum += sigma2[i];
        result[0] = Math.Sqrt(Math.Max(0, sum / _period));
        for (int i = 1; i < outLen; i++)
        {
            sum += sigma2[i + _period - 1] - sigma2[i - 1];
            result[i] = Math.Sqrt(Math.Max(0, sum / _period));
        }
        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), _period - 1);
}
