// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>Alexander Elder's Force Index — volume-weighted price change, optionally
/// EMA-smoothed. Positive = buyers in control, negative = sellers. Reference: Elder (1993),
/// <i>Trading for a Living</i>, §7.</summary>
public sealed class ForceIndex : CandleIndicator<SignalResult>
{
    private readonly int _period;

    /// <summary>Creates a new Force Index indicator.</summary>
    /// <param name="close">Close prices.</param>
    /// <param name="volume">Volume per bar (same length as <paramref name="close"/>).</param>
    /// <param name="period">EMA smoothing period (default 13). Must be ≥ 1. Period 1 = no smoothing.</param>
    public ForceIndex(double[] close, double[] volume, int period = 13)
        : base([], close, close, close, volume)
    {
        if (close.Length != volume.Length)
            throw new ArgumentException(
                $"close ({close.Length}) and volume ({volume.Length}) must have equal length.", nameof(volume));
        if (period < 1)
            throw new ArgumentException($"period must be >= 1 (got {period}).", nameof(period));
        _period = period;
        Label = $"Force({period})";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int n = BarCount;
        if (n < 2) return Array.Empty<double>();

        int outLen = n - 1;
        var raw = new double[outLen];
        for (int i = 0; i < outLen; i++)
            raw[i] = Volume[i + 1] * (Close[i + 1] - Close[i]);

        if (_period == 1) return raw;

        // EMA with adjust=false convention: seed at raw[0], then recurse.
        double k = 2.0 / (_period + 1);
        var result = new double[outLen];
        result[0] = raw[0];
        for (int i = 1; i < outLen; i++)
            result[i] = k * raw[i] + (1 - k) * result[i - 1];
        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), warmup: 1);
}
