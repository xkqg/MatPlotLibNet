// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>VWAP Z-Score — standardised deviation from the rolling Volume-Weighted Average
/// Price. Quantifies how far price has wandered from its volume-weighted fair value in units
/// of rolling-deviation standard deviations. Mean-reversion traders use extreme values (±2σ)
/// as entry signals. Modern quant-microstructure technique; no single attribution.</summary>
public sealed class VwapZScore : CandleIndicator<SignalResult>
{
    private readonly int _window;

    /// <summary>Creates a new VWAP Z-Score indicator.</summary>
    /// <param name="close">Close prices.</param>
    /// <param name="volume">Volume per bar. Must match <paramref name="close"/> length.</param>
    /// <param name="window">Rolling window length. Default 20. Must be ≥ 2.</param>
    public VwapZScore(double[] close, double[] volume, int window = 20)
        : base([], close, close, close, volume) // H=L=close placeholder; Compute() uses only Close + Volume
    {
        if (close.Length != volume.Length)
            throw new ArgumentException(
                $"close ({close.Length}) and volume ({volume.Length}) must have equal length.");
        if (window < 2)
            throw new ArgumentException($"window must be >= 2 (got {window}).", nameof(window));
        _window = window;
        Label = $"VwapZ({window})";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int n = BarCount;
        if (n < _window) return Array.Empty<double>();

        int outLen = n - _window + 1;
        var result = new double[outLen];

        for (int w = 0; w < outLen; w++)
        {
            int start = w;
            int end = w + _window;                                // exclusive

            // 1) Window VWAP at the current window end.
            double sumPv = 0, sumV = 0;
            for (int i = start; i < end; i++)
            {
                sumPv += Close[i] * Volume[i];
                sumV += Volume[i];
            }
            if (sumV <= 0) { result[w] = 0; continue; }
            double vwap = sumPv / sumV;
            double deviation = Close[end - 1] - vwap;

            // 2) Rolling std of per-bar VWAP deviations within the same window.
            //    For each i in window, compute VWAP of the window ENDING at i, then dev_i = Close[i] - VwapEndingAt_i.
            //    Only the i's where a full _window fits (i >= _window - 1 in global indexing) are usable.
            double mean = 0;
            int count = 0;
            double[] devs = new double[_window];
            for (int i = start; i < end; i++)
            {
                if (i < _window - 1) continue;                    // not enough history for a full VWAP window
                double sPv = 0, sV = 0;
                for (int j = i - _window + 1; j <= i; j++)
                {
                    sPv += Close[j] * Volume[j];
                    sV += Volume[j];
                }
                double localVwap = sV > 0 ? sPv / sV : Close[i];
                devs[count] = Close[i] - localVwap;
                mean += devs[count];
                count++;
            }

            if (count < 2) { result[w] = 0; continue; }
            mean /= count;
            double sqSum = 0;
            for (int k = 0; k < count; k++)
            {
                double d = devs[k] - mean;
                sqSum += d * d;
            }
            // Sample standard deviation (ddof=1) to match the Python reference in the spec.
            double std = Math.Sqrt(sqSum / (count - 1));
            result[w] = std > 0 ? deviation / std : 0.0;
        }

        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), warmup: _window - 1);
}
