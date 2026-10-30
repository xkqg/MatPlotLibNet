// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
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
        Label = $"BOCPD(h={hazard.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)})";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int n = Prices.Length;
        if (n < 2) return Array.Empty<double>();

        var result = new double[n - 1];
        var step = new BocpdStep(_hazard, _priorVariance, _maxRunLength);
        for (int t = 0; t < n; t++)
        {
            double changePoint = step.Next(Prices[t]);
            if (t >= 1) result[t - 1] = changePoint;
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
