// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>Ehlers' Relative Vigor Index — intraday momentum via the ratio of close-open to
/// high-low, weighted-smoothed and SMA'd. Paired with its 4-bar weighted signal line;
/// crossovers mark momentum shifts. Reference: Ehlers (2002), S&amp;C 20(1).</summary>
public sealed class RelativeVigorIndex : CandleIndicator<RviResult>
{
    private readonly int _period;

    /// <summary>Creates a new Relative Vigor Index indicator.</summary>
    /// <param name="open">Open prices.</param>
    /// <param name="high">High prices.</param>
    /// <param name="low">Low prices.</param>
    /// <param name="close">Close prices.</param>
    /// <param name="period">Rolling SMA period on the weighted-smoothed value/range.
    /// Default 10. Must be ≥ 2.</param>
    public RelativeVigorIndex(double[] open, double[] high, double[] low, double[] close, int period = 10)
        : base(open, high, low, close, [])
    {
        if (open.Length != high.Length || high.Length != low.Length || low.Length != close.Length)
            throw new ArgumentException("OHLC arrays must have equal length.");
        if (period < 2)
            throw new ArgumentException($"period must be >= 2 (got {period}).", nameof(period));
        _period = period;
        Label = $"RVI({period})";
    }

    /// <inheritdoc />
    public override RviResult Compute()
    {
        int n = BarCount;
        // Need at least (period + 3 + 3) bars for Signal's valid tail:
        //   weighted smooth needs 4 bars (index ≥ 3)
        //   SMA over `period` smoothed values needs first valid at index period + 2
        //   signal is weighted smooth of RVI, needs 4 RVI values → first valid at period + 5
        if (n < _period + 4)
            return new RviResult(Array.Empty<double>(), Array.Empty<double>());

        // Per-bar value = C − O, range = H − L.
        var value = new double[n];
        var range = new double[n];
        for (int t = 0; t < n; t++)
        {
            value[t] = Close[t] - Open[t];
            range[t] = High[t] - Low[t];
        }

        // 4-bar weighted smooth (weights 1, 2, 2, 1, divisor 6). Valid from t = 3.
        var numSmooth = new double[n];
        var denSmooth = new double[n];
        for (int t = 3; t < n; t++)
        {
            numSmooth[t] = (value[t] + 2 * value[t - 1] + 2 * value[t - 2] + value[t - 3]) / 6.0;
            denSmooth[t] = (range[t] + 2 * range[t - 1] + 2 * range[t - 2] + range[t - 3]) / 6.0;
        }

        // Rolling SMA over `period` bars; first valid at t = period + 2
        // (needs smoothed values at [t - period + 1 .. t], all ≥ 3 → t ≥ period + 2).
        var rviFull = new double[n];
        for (int t = _period + 2; t < n; t++)
        {
            double sumNum = 0, sumDen = 0;
            for (int k = t - _period + 1; k <= t; k++)
            {
                sumNum += numSmooth[k];
                sumDen += denSmooth[k];
            }
            rviFull[t] = sumDen > 0 ? sumNum / sumDen : 0;
        }

        // Signal = 4-bar weighted smooth of RVI. First valid at t = period + 5.
        int rviStart = _period + 2;
        int sigStart = _period + 5;

        // RVI output: bars [period + 2 .. n-1], length = n - period - 2.
        int rviLen = n - rviStart;
        var rvi = new double[rviLen];
        Array.Copy(rviFull, rviStart, rvi, 0, rviLen);

        // Signal output: bars [period + 5 .. n-1], length = n - period - 5.
        int sigLen = n - sigStart;
        var signal = new double[sigLen];
        for (int t = sigStart; t < n; t++)
        {
            signal[t - sigStart] =
                (rviFull[t] + 2 * rviFull[t - 1] + 2 * rviFull[t - 2] + rviFull[t - 3]) / 6.0;
        }

        return new RviResult(rvi, signal);
    }

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        var result = Compute();
        PlotSignal(axes, result.Rvi, warmup: _period + 2, label: "RVI");
        PlotSignal(axes, result.Signal, warmup: _period + 5, label: "Signal");
    }
}
