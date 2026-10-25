// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>Regime-detection ratio of short-window to long-window Yang-Zhang volatility.
/// Ratio &gt; 1 = vol expansion (potential breakout), ratio &lt; 1 = vol contraction
/// (consolidation). Reuses <see cref="YangZhang"/> for both components — same formula, two
/// different windows. Reference for underlying vol: Yang &amp; Zhang (2000),
/// <i>Journal of Business</i> 73(3); ratio framing is a common practitioner adaptation.</summary>
public sealed class YangZhangVolRatio : CandleIndicator<SignalResult>
{
    private readonly int _shortWindow;
    private readonly int _longWindow;

    /// <summary>Creates a new Yang-Zhang volatility ratio indicator.</summary>
    /// <param name="bars">Open, High, Low and Close (all strictly positive); built via <see cref="OhlcvBars.Ohlc"/>.</param>
    /// <param name="shortWindow">Short YZ window. Default 20. Must be ≥ 2.</param>
    /// <param name="longWindow">Long YZ window. Default 60. Must be &gt; <paramref name="shortWindow"/>.</param>
    public YangZhangVolRatio(OhlcvBars bars, int shortWindow = 20, int longWindow = 60)
        : base(bars)
    {
        if (shortWindow < 2)
            throw new ArgumentException($"shortWindow must be >= 2 (got {shortWindow}).", nameof(shortWindow));
        if (longWindow <= shortWindow)
            throw new ArgumentException(
                $"longWindow must be > shortWindow (got long={longWindow}, short={shortWindow}).",
                nameof(longWindow));
        _shortWindow = shortWindow;
        _longWindow = longWindow;
        Label = $"YZRatio({shortWindow}/{longWindow})";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int n = BarCount;
        if (n <= _longWindow) return Array.Empty<double>();

        var bars = OhlcvBars.Ohlc(Open, High, Low, Close);
        var shortVol = new YangZhang(bars, _shortWindow).Compute().Values;
        var longVol = new YangZhang(bars, _longWindow).Compute().Values;

        // shortVol length = n - shortWindow;  longVol length = n - longWindow.
        // Align on the longer warmup: short-vol offset = longWindow - shortWindow.
        int outLen = longVol.Length;
        int offset = _longWindow - _shortWindow;
        var result = new double[outLen];
        for (int i = 0; i < outLen; i++)
        {
            double lv = longVol[i];
            double sv = shortVol[i + offset];
            result[i] = lv > 0 ? sv / lv : 1.0;
        }
        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), warmup: _longWindow);
}
