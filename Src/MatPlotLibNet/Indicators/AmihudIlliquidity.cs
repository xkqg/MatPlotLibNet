// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>Amihud illiquidity — ratio of absolute return to dollar volume, averaged over a
/// rolling window. High = illiquid; low = deep, liquid market. Most-cited illiquidity measure
/// in academic finance.</summary>
/// <remarks>Per-bar: <c>ILLIQ_t = |ln(C_t/C_{t-1})| / (C_t · V_t)</c>. Output is the rolling
/// mean over <c>period</c> bars. When <c>V_t = 0</c>, the bar contributes
/// <see cref="double.PositiveInfinity"/> (matches academic convention: no volume = maximally
/// illiquid). Reference: Amihud (2002), <i>Journal of Financial Markets</i> 5(1).</remarks>
public sealed class AmihudIlliquidity : CandleIndicator<SignalResult>
{
    private readonly int _period;

    /// <summary>Creates a new Amihud illiquidity indicator.</summary>
    /// <param name="close">Close prices (strictly positive).</param>
    /// <param name="volume">Volume per bar (same length as <paramref name="close"/>).</param>
    /// <param name="period">Rolling window length (default 20, must be ≥ 1).</param>
    /// <exception cref="ArgumentException">Thrown on length mismatch, non-positive period,
    /// or non-positive close.</exception>
    public AmihudIlliquidity(double[] close, double[] volume, int period = 20)
        : base([], close, close, close, volume)
    {
        if (close.Length != volume.Length)
            throw new ArgumentException(
                $"close ({close.Length}) and volume ({volume.Length}) must have equal length.", nameof(volume));
        if (period <= 0)
            throw new ArgumentException($"period must be > 0 (got {period}).", nameof(period));
        for (int i = 0; i < close.Length; i++)
        {
            if (close[i] <= 0)
                throw new ArgumentException(
                    $"AmihudIlliquidity requires strictly positive close; bar {i} has {close[i]}.",
                    nameof(close));
        }
        _period = period;
        Label = $"ILLIQ({period})";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int n = BarCount;
        if (n <= _period) return Array.Empty<double>();

        // Per-bar ILLIQ over bars 1..n-1 (length n-1).
        var illiq = new double[n - 1];
        for (int i = 0; i < n - 1; i++)
        {
            int t = i + 1;
            double absR = Math.Abs(Math.Log(Close[t] / Close[t - 1]));
            double dollarVol = Close[t] * Volume[t];
            illiq[i] = dollarVol == 0 ? double.PositiveInfinity : absR / dollarVol;
        }

        int outLen = n - _period;
        var result = new double[outLen];
        double sum = 0;
        for (int i = 0; i < _period; i++) sum += illiq[i];
        result[0] = sum / _period;
        for (int i = 1; i < outLen; i++)
        {
            sum += illiq[i + _period - 1] - illiq[i - 1];
            result[i] = sum / _period;
        }
        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), _period);
}
