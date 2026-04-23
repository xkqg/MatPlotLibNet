// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>Richard Arms' Ease of Movement (EMV) — measures how easily price moves a given
/// distance relative to volume. Positive = rises on light volume (accumulation), negative =
/// falls on light volume (distribution). Reference: Arms, R. W. (1975),
/// <i>Volume Cycles in the Stock Market</i>.</summary>
public sealed class EaseOfMovement : CandleIndicator<SignalResult>
{
    private readonly int _period;
    private readonly double _scale;

    /// <summary>Creates a new Ease of Movement indicator.</summary>
    /// <param name="high">High prices.</param>
    /// <param name="low">Low prices.</param>
    /// <param name="volume">Volume per bar. Must match H/L length.</param>
    /// <param name="period">SMA smoothing period for the raw EMV. Default 14. Must be ≥ 2.</param>
    /// <param name="scale">Volume-scale divisor — normalises volume to price-move magnitude.
    /// Default 10^6 (suits crypto / intraday). Must be &gt; 0.</param>
    public EaseOfMovement(double[] high, double[] low, double[] volume,
        int period = 14, double scale = 1_000_000)
        : base([], high, low, high, volume) // close=high placeholder; Compute() uses only H, L, V
    {
        if (high.Length != low.Length || high.Length != volume.Length)
            throw new ArgumentException(
                $"high ({high.Length}), low ({low.Length}), and volume ({volume.Length}) must have equal length.");
        if (period < 2)
            throw new ArgumentException($"period must be >= 2 (got {period}).", nameof(period));
        if (scale <= 0)
            throw new ArgumentException($"scale must be > 0 (got {scale}).", nameof(scale));
        _period = period;
        _scale = scale;
        Label = $"EMV({period})";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int n = BarCount;
        if (n <= _period) return Array.Empty<double>();

        var emv1 = new double[n];
        for (int i = 1; i < n; i++)
        {
            double mid = (High[i] + Low[i]) / 2.0;
            double prevMid = (High[i - 1] + Low[i - 1]) / 2.0;
            double move = mid - prevMid;
            double range = High[i] - Low[i];
            // BoxRatio = (Volume / scale) / range. Guard: zero volume or zero range → 0.
            if (Volume[i] == 0 || range == 0) { emv1[i] = 0; continue; }
            double boxRatio = (Volume[i] / _scale) / range;
            emv1[i] = move / boxRatio;
        }

        int outLen = n - _period;
        var result = new double[outLen];
        // Rolling SMA of emv1 over _period, starting at index _period.
        double sum = 0;
        for (int i = 1; i <= _period; i++) sum += emv1[i];
        result[0] = sum / _period;
        for (int w = 1; w < outLen; w++)
        {
            sum += emv1[w + _period] - emv1[w];
            result[w] = sum / _period;
        }
        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), warmup: _period);
}
