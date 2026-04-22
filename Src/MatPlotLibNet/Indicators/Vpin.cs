// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>VPIN — Volume-Synchronized Probability of Informed Trading, BVC approximation.
/// High values precede liquidity crises. Reference: Easley, Lopez de Prado &amp; O'Hara (2012),
/// <i>Review of Financial Studies</i> 25(5).</summary>
/// <remarks>Bulk Volume Classification: <c>z_t = (r_t − μ_r)/σ_r</c>, <c>BuyVol = Vol·Φ(z_t)</c>,
/// <c>SellVol = Vol − BuyVol</c>. When <c>σ_r = 0</c>, <c>z</c> is treated as 0 (50/50 split)
/// yielding VPIN = 0 for that window.</remarks>
public sealed class Vpin : CandleIndicator<SignalResult>
{
    private readonly int _bucketPeriod;
    private readonly int _sigmaPeriod;

    /// <summary>Creates a new VPIN indicator.</summary>
    /// <param name="close">Close prices (strictly positive).</param>
    /// <param name="volume">Volume per bar (same length as close).</param>
    /// <param name="bucketPeriod">Rolling VPIN bucket length (default 50).</param>
    /// <param name="sigmaPeriod">Rolling return-std normalization window (default 50).</param>
    public Vpin(double[] close, double[] volume, int bucketPeriod = 50, int sigmaPeriod = 50)
        : base([], close, close, close, volume)
    {
        if (close.Length != volume.Length)
            throw new ArgumentException(
                $"close ({close.Length}) and volume ({volume.Length}) must have equal length.", nameof(volume));
        if (bucketPeriod <= 0)
            throw new ArgumentException($"bucketPeriod must be > 0 (got {bucketPeriod}).", nameof(bucketPeriod));
        if (sigmaPeriod <= 0)
            throw new ArgumentException($"sigmaPeriod must be > 0 (got {sigmaPeriod}).", nameof(sigmaPeriod));
        for (int i = 0; i < close.Length; i++)
        {
            if (close[i] <= 0)
                throw new ArgumentException(
                    $"Vpin requires strictly positive close; bar {i} has {close[i]}.", nameof(close));
        }
        _bucketPeriod = bucketPeriod;
        _sigmaPeriod = sigmaPeriod;
        Label = $"VPIN({bucketPeriod})";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int n = BarCount;
        int maxPeriod = Math.Max(_bucketPeriod, _sigmaPeriod);
        if (n <= maxPeriod) return Array.Empty<double>();

        // Log-returns indexed 0..n-2 (return i corresponds to bar transition i → i+1).
        int rLen = n - 1;
        var r = new double[rLen];
        for (int i = 0; i < rLen; i++)
            r[i] = Math.Log(Close[i + 1] / Close[i]);

        // Per-return BuyVolume via BVC. σ is computed over the expanding-then-rolling
        // window of the most recent min(i+1, sigmaPeriod) returns ending at index i.
        var buy = new double[rLen];
        for (int i = 0; i < rLen; i++)
        {
            int start = Math.Max(0, i - _sigmaPeriod + 1);
            int count = i - start + 1;
            double mean = 0, m2 = 0;
            for (int k = start; k <= i; k++) mean += r[k];
            mean /= count;
            for (int k = start; k <= i; k++)
            {
                double d = r[k] - mean;
                m2 += d * d;
            }
            double sigma = count > 1 ? Math.Sqrt(m2 / (count - 1)) : 0;

            // σ=0 policy: 50/50 split exactly (avoids Φ(0) approximation error).
            double phi = sigma == 0 ? 0.5 : NormalCdf((r[i] - mean) / sigma);
            buy[i] = Volume[i + 1] * phi;
        }

        // VPIN: for each output w, bucket ends at return index maxPeriod + w, spans
        // the last bucketPeriod returns.
        int outLen = n - maxPeriod - 1;
        var result = new double[outLen];
        for (int w = 0; w < outLen; w++)
        {
            int endIdx = maxPeriod + w;
            int startIdx = endIdx - _bucketPeriod + 1;
            double sumAbs = 0, sumVol = 0;
            for (int j = startIdx; j <= endIdx; j++)
            {
                double b = buy[j];
                double v = Volume[j + 1];
                double s = v - b;
                sumAbs += Math.Abs(b - s);
                sumVol += v;
            }
            result[w] = sumVol == 0 ? 0 : sumAbs / sumVol;
        }
        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        PlotSignal(axes, Compute(), Math.Max(_bucketPeriod, _sigmaPeriod) + 1);
        axes.YAxis.Min = 0;
        axes.YAxis.Max = 1;
    }

    // Standard-normal CDF via Abramowitz & Stegun 26.2.17 rational approximation.
    // Maximum error ≈ 1.5e-7 — more than enough for VPIN.
    private static double NormalCdf(double z)
    {
        const double a1 = 0.254829592;
        const double a2 = -0.284496736;
        const double a3 = 1.421413741;
        const double a4 = -1.453152027;
        const double a5 = 1.061405429;
        const double p = 0.3275911;

        int sign = z < 0 ? -1 : 1;
        double x = Math.Abs(z) / Math.Sqrt(2.0);
        double t = 1.0 / (1.0 + p * x);
        double y = 1.0 - ((((a5 * t + a4) * t + a3) * t + a2) * t + a1) * t * Math.Exp(-x * x);
        return 0.5 * (1.0 + sign * y);
    }
}
