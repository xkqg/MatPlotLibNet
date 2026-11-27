// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Indicators.Steps;

namespace MatPlotLibNet.Indicators;

/// <summary>VWAP Z-Score — standardised deviation from the rolling Volume-Weighted Average
/// Price. Quantifies how far price has wandered from its volume-weighted fair value in units
/// of rolling-deviation standard deviations. Mean-reversion traders use extreme values (±2σ)
/// as entry signals. Modern quant-microstructure technique; no single attribution.</summary>
public sealed class VwapZScore : CandleIndicator<SignalResult>
{
    /// <summary>The <c>window</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultWindow = 20;

    private readonly int _window;

    /// <summary>Creates a new VWAP Z-Score indicator.</summary>
    /// <param name="bars">Close and Volume; built via <see cref="OhlcvBars.Cv"/>.</param>
    /// <param name="window">Rolling window length. Default <see cref="DefaultWindow"/>. Must be ≥ 2.</param>
    public VwapZScore(OhlcvBars bars, int window = DefaultWindow)
        : base(bars, Spec(window))
    {
        _window = window;
    }

    internal static IndicatorSpec Spec(int window) => IndicatorSpec.OfCheckedWindow("VwapZ", window, 2, window - 1);

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int outLen = ValueCount;
        var result = new double[outLen];

        for (int w = 0; w < outLen; w++)
        {
            int start = w;
            int end = w + _window;                                // exclusive

            // 1) Window VWAP at the current window end.
            var windowMean = new VolumeWeightedMean();
            for (int i = start; i < end; i++) windowMean.Add(Close[i], Volume[i]);
            if (windowMean.Volume <= 0) { result[w] = 0; continue; }
            double vwap = windowMean.Value(0);
            double deviation = Close[end - 1] - vwap;

            // 2) Rolling std of per-bar VWAP deviations within the same window.
            //    For each i in window, compute VWAP of the window ENDING at i, then dev_i = Close[i] - VwapEndingAt_i.
            //    Only the i's where a full _window fits (i >= _window - 1 in global indexing) are usable.
            int count = 0;
            double[] devs = new double[_window];
            for (int i = start; i < end; i++)
            {
                if (i < FirstValueIndex) continue;               // not enough history for a full VWAP window
                var localMean = new VolumeWeightedMean();
                for (int j = i - _window + 1; j <= i; j++) localMean.Add(Close[j], Volume[j]);
                double localVwap = localMean.Value(Close[i]);
                devs[count] = Close[i] - localVwap;
                count++;
            }

            if (count < 2) { result[w] = 0; continue; }
            // Sample standard deviation (ddof=1) to match the Python reference in the spec.
            double std = devs.AsSpan(0, count).StdDev(ddof: 1);
            result[w] = std > 0 ? deviation / std : 0.0;
        }

        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotLine(axes, Compute());
}
