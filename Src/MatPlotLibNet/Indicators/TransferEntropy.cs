// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>Schreiber's transfer entropy (2000) — information-theoretic measure of directional
/// influence from one time series to another. Unlike correlation (symmetric, linear),
/// transfer entropy captures asymmetric, nonlinear information flow. Uses equal-width
/// histogram binning to estimate joint / marginal probabilities; output is a scalar in nats
/// (natural-log units). Reference: Schreiber, T. (2000), <i>Measuring Information Transfer</i>,
/// Physical Review Letters 85(2), 461. Applied to finance: Marschinski &amp; Kantz (2002),
/// Kwon &amp; Yang (2008); feature-importance framing: Lopez de Prado (2018), Ch. 8.</summary>
public sealed class TransferEntropy : Indicator<SignalResult>
{
    private readonly double[] _source;
    private readonly double[] _target;
    private readonly int _bins;
    private readonly int _lag;

    /// <summary>Creates a new transfer-entropy indicator measuring <c>TE(source → target)</c>.</summary>
    /// <param name="source">Source series X — the potentially-influencing series.</param>
    /// <param name="target">Target series Y — the potentially-influenced series. Must match
    /// <paramref name="source"/> in length.</param>
    /// <param name="bins">Histogram bin count per series. Default 8. Must be ≥ 2.</param>
    /// <param name="lag">Source lag τ: the triple uses <c>x_{t-τ+1}</c>. Default 1
    /// (<c>x_t</c> predicts <c>y_{t+1}</c>). Must be ≥ 1. Length must be ≥ <c>lag + 2</c>.</param>
    public TransferEntropy(double[] source, double[] target, int bins = 8, int lag = 1)
    {
        if (source is null) throw new ArgumentException("source is required.", nameof(source));
        if (target is null) throw new ArgumentException("target is required.", nameof(target));
        if (source.Length != target.Length)
            throw new ArgumentException(
                $"source ({source.Length}) and target ({target.Length}) must have equal length.");
        if (bins < 2)
            throw new ArgumentException($"bins must be >= 2 (got {bins}).", nameof(bins));
        if (lag < 1)
            throw new ArgumentException($"lag must be >= 1 (got {lag}).", nameof(lag));
        if (source.Length < lag + 2)
            throw new ArgumentException(
                $"length ({source.Length}) must be >= lag + 2 ({lag + 2}) to form triples.",
                nameof(source));
        _source = source;
        _target = target;
        _bins = bins;
        _lag = lag;
        Label = $"TE(bins={bins},lag={lag})";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        return new double[] { ComputeScalar() };
    }

    /// <summary>Computes the scalar transfer-entropy value in nats.</summary>
    internal double ComputeScalar()
    {
        int n = _source.Length;
        var sBins = Discretize(_source, _bins);
        var tBins = Discretize(_target, _bins);

        // Triple (y_future, y_present, x_lagged) at time t, t ∈ [lag-1, n-2]
        // y_future = Y[t+1], y_present = Y[t], x_lagged = X[t - lag + 1].
        var joint = new int[_bins, _bins, _bins];
        var yfp = new int[_bins, _bins];    // (y_future, y_present)
        var yxp = new int[_bins, _bins];    // (y_present, x_lagged)
        var yp = new int[_bins];            // y_present
        int total = 0;
        for (int t = _lag - 1; t <= n - 2; t++)
        {
            int yf = tBins[t + 1];
            int ypCur = tBins[t];
            int xl = sBins[t - _lag + 1];
            joint[yf, ypCur, xl]++;
            yfp[yf, ypCur]++;
            yxp[ypCur, xl]++;
            yp[ypCur]++;
            total++;
        }

        if (total == 0) return 0.0;
        double invTotal = 1.0 / total;
        double te = 0;
        for (int a = 0; a < _bins; a++)
        for (int b = 0; b < _bins; b++)
        for (int c = 0; c < _bins; c++)
        {
            int cnt = joint[a, b, c];
            if (cnt == 0) continue;
            double pj = cnt * invTotal;
            double pf = yfp[a, b] * invTotal;
            double pc = yxp[b, c] * invTotal;
            double pb = yp[b] * invTotal;
            if (pf <= 0 || pc <= 0 || pb <= 0) continue;
            te += pj * Math.Log(pj * pb / (pf * pc));
        }
        return te;
    }

    /// <summary>Equal-width binning. If the series has zero range (all values equal) every
    /// sample maps to bin 0 — no information content, TE collapses to 0.</summary>
    private static int[] Discretize(double[] values, int bins)
    {
        int n = values.Length;
        var result = new int[n];
        double min = values[0], max = values[0];
        for (int i = 1; i < n; i++)
        {
            if (values[i] < min) min = values[i];
            if (values[i] > max) max = values[i];
        }
        double range = max - min;
        if (range == 0) return result; // all zeros — all samples in bin 0
        double invWidth = bins / range;
        for (int i = 0; i < n; i++)
        {
            int b = (int)((values[i] - min) * invWidth);
            if (b < 0) b = 0;
            if (b >= bins) b = bins - 1;
            result[i] = b;
        }
        return result;
    }

    /// <inheritdoc />
    /// <remarks>Transfer entropy is a scalar, not a per-bar series — callers typically compute
    /// <see cref="ComputeScalar"/> and display the result as a number or annotation. This
    /// method is a no-op to keep the <see cref="IIndicator"/> contract satisfied without
    /// injecting a misleading line series.</remarks>
    public override void Apply(Axes axes) { }
}
