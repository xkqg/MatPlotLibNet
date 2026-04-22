// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>Bayesian Online Changepoint Detection (Adams &amp; MacKay 2007). Maintains a
/// posterior over run lengths; the changepoint probability is <c>P(r_t = 0)</c> — the
/// posterior probability that the current bar starts a new run.</summary>
/// <remarks>Output is per-bar changepoint probability in [0, 1]. Output length is
/// <c>prices.Length − 1</c> (first bar has no prior). Reference:
/// Adams &amp; MacKay (2007), arXiv:0710.3742.</remarks>
public sealed class Bocpd : PriceIndicator<SignalResult>
{
    private readonly double _hazard;
    private readonly double _priorVariance;
    private readonly int _maxRunLength;

    /// <summary>Creates a new BOCPD indicator.</summary>
    /// <param name="prices">Price series.</param>
    /// <param name="hazard">Per-bar prior probability of a changepoint, in (0, 1). Default 0.01.</param>
    /// <param name="priorVariance">Broad prior variance <c>κ²</c> added to the predictive. Must be &gt; 0.</param>
    /// <param name="maxRunLength">Maximum run length tracked (memory bound). Must be ≥ 1.</param>
    public Bocpd(double[] prices, double hazard = 0.01, double priorVariance = 1.0,
                 int maxRunLength = 500)
        : base(prices)
    {
        if (hazard <= 0 || hazard >= 1)
            throw new ArgumentException($"hazard must be in (0, 1) (got {hazard}).", nameof(hazard));
        if (priorVariance <= 0)
            throw new ArgumentException($"priorVariance must be > 0 (got {priorVariance}).", nameof(priorVariance));
        if (maxRunLength < 1)
            throw new ArgumentException($"maxRunLength must be >= 1 (got {maxRunLength}).", nameof(maxRunLength));
        _hazard = hazard;
        _priorVariance = priorVariance;
        _maxRunLength = maxRunLength;
        Label = $"BOCPD(h={hazard:0.###})";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int n = Prices.Length;
        if (n < 2) return Array.Empty<double>();

        int maxR = _maxRunLength;
        int size = maxR + 1;

        // State per run length r: probability, sample count, running mean, running sum-of-squared-deviations (M2).
        var runProb = new double[size];
        var runCount = new int[size];
        var runMean = new double[size];
        var runM2 = new double[size];
        var newProb = new double[size];

        // Bar 0 — initialize the r=0 run with the first observation.
        runProb[0] = 1.0;
        runCount[0] = 1;
        runMean[0] = Prices[0];

        double sqrt2pi = Math.Sqrt(2 * Math.PI);
        var result = new double[n - 1];

        for (int t = 1; t < n; t++)
        {
            double xt = Prices[t];
            Array.Clear(newProb, 0, size);

            // Predictive + mass redistribution.
            for (int r = 0; r < size; r++)
            {
                if (runCount[r] == 0) continue;

                double variance = (runCount[r] > 1 ? runM2[r] / (runCount[r] - 1) : 0.0) + _priorVariance;
                double diff = xt - runMean[r];
                double pred = Math.Exp(-0.5 * diff * diff / variance) / (sqrt2pi * Math.Sqrt(variance));
                double weight = runProb[r] * pred;

                // Growth: r → r+1, or absorbed at r=maxR.
                if (r < maxR)
                    newProb[r + 1] += weight * (1 - _hazard);
                else
                    newProb[r] += weight * (1 - _hazard);

                // Changepoint contribution.
                newProb[0] += weight * _hazard;
            }

            // Normalize — if sum underflowed to 0 (extreme surprise), force full changepoint.
            double sum = 0;
            for (int r = 0; r < size; r++) sum += newProb[r];
            if (sum > 0)
            {
                for (int r = 0; r < size; r++) newProb[r] /= sum;
            }
            else
            {
                Array.Clear(newProb, 0, size);
                newProb[0] = 1.0;
            }

            // Advance sufficient statistics: r+1's stats are (old r's stats) with xt appended.
            // Iterate high-to-low to avoid overwriting.
            for (int r = maxR - 1; r >= 0; r--)
            {
                if (runCount[r] == 0)
                {
                    runCount[r + 1] = 0;
                    runMean[r + 1] = 0;
                    runM2[r + 1] = 0;
                    continue;
                }
                int newCount = runCount[r] + 1;
                double delta = xt - runMean[r];
                double newMean = runMean[r] + delta / newCount;
                double delta2 = xt - newMean;
                runCount[r + 1] = newCount;
                runMean[r + 1] = newMean;
                runM2[r + 1] = runM2[r] + delta * delta2;
            }
            // r=0 starts fresh with xt as sole observation.
            runCount[0] = 1;
            runMean[0] = xt;
            runM2[0] = 0;

            Array.Copy(newProb, runProb, size);
            result[t - 1] = newProb[0];
        }
        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        PlotSignal(axes, Compute(), warmup: 1);
        axes.YAxis.Min = 0;
        axes.YAxis.Max = 1;
    }
}
