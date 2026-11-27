// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>VPIN — Volume-Synchronized Probability of Informed Trading, BVC approximation.
/// High values precede liquidity crises. Reference: Easley, Lopez de Prado &amp; O'Hara (2012),
/// <i>Review of Financial Studies</i> 25(5).</summary>
/// <remarks>Bulk Volume Classification: <c>z_t = (r_t − μ_r)/σ_r</c>, <c>BuyVol = Vol·Φ(z_t)</c>,
/// <c>SellVol = Vol − BuyVol</c>. When <c>σ_r = 0</c>, <c>z</c> is treated as 0 (50/50 split)
/// yielding VPIN = 0 for that window.</remarks>
public sealed class Vpin : CandleIndicator<SignalResult>
{
    /// <summary>The <c>bucketPeriod</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultBucketPeriod = 50;

    /// <summary>The <c>sigmaPeriod</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultSigmaPeriod = 50;

    private readonly int _bucketPeriod;
    private readonly int _sigmaPeriod;

    /// <summary>Creates a new VPIN indicator.</summary>
    /// <param name="bars">Close (strictly positive) and Volume; built via <see cref="OhlcvBars.Cv"/>.</param>
    /// <param name="bucketPeriod">Rolling VPIN bucket length (default <see cref="DefaultBucketPeriod"/>).</param>
    /// <param name="sigmaPeriod">Rolling return-std normalization window (default <see cref="DefaultSigmaPeriod"/>).</param>
    public Vpin(OhlcvBars bars, int bucketPeriod = DefaultBucketPeriod, int sigmaPeriod = DefaultSigmaPeriod)
        : base(bars, Spec(bucketPeriod, sigmaPeriod))
    {
        bars.Close.ThrowIfNotPositivePrices(nameof(Vpin), "close", nameof(bars));
        _bucketPeriod = bucketPeriod;
        _sigmaPeriod = sigmaPeriod;
    }

    internal static IndicatorSpec Spec(int bucketPeriod, int sigmaPeriod)
    {
        bucketPeriod.ThrowIfBelow(1, nameof(bucketPeriod));
        sigmaPeriod.ThrowIfBelow(1, nameof(sigmaPeriod));
        return IndicatorSpec.OfWindow("VPIN", bucketPeriod, Math.Max(bucketPeriod, sigmaPeriod) + 1);
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int maxPeriod = Math.Max(_bucketPeriod, _sigmaPeriod);

        // Log-returns indexed 0..n-2 (return i corresponds to bar transition i → i+1).
        int rLen = PairsIn;
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
            var recent = r.AsSpan(start, count);
            double mean = recent.Mean();
            double sigma = count > 1 ? recent.StdDev(mean, ddof: 1) : 0;

            // σ=0 policy: 50/50 split exactly (avoids Φ(0) approximation error).
            double phi = sigma == 0 ? 0.5 : NormalCdf((r[i] - mean) / sigma);
            buy[i] = Volume[i + 1] * phi;
        }

        // VPIN: for each output w, bucket ends at return index maxPeriod + w, spans
        // the last bucketPeriod returns.
        int outLen = ValueCount;
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

    /// <summary>The range the values of the indicator live in: its pane, and the pane of <see cref="Streaming.StreamingVpin"/>, are pinned to it.</summary>
    internal static PaneRange Pane { get; } = PaneRange.Unit;

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        PlotLine(axes, Compute());
        Pane.Pin(axes);
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
