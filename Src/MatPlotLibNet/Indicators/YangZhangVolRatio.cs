// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>Regime-detection ratio of short-window to long-window Yang-Zhang volatility.
/// Ratio &gt; 1 = vol expansion (potential breakout), ratio &lt; 1 = vol contraction
/// (consolidation). Reuses <see cref="YangZhang"/> for both components — same formula, two
/// different windows. Reference for underlying vol: Yang &amp; Zhang (2000),
/// <i>Journal of Business</i> 73(3); ratio framing is a common practitioner adaptation.</summary>
public sealed class YangZhangVolRatio : CandleIndicator<SignalResult>
{
    /// <summary>The <c>shortWindow</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultShortWindow = 20;

    /// <summary>The <c>longWindow</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultLongWindow = 60;

    private readonly int _shortWindow;
    private readonly int _longWindow;

    /// <summary>Creates a new Yang-Zhang volatility ratio indicator.</summary>
    /// <param name="bars">Open, High, Low and Close (all strictly positive); built via <see cref="OhlcvBars.Ohlc"/>.</param>
    /// <param name="shortWindow">Short YZ window. Default <see cref="DefaultShortWindow"/>. Must be ≥ 2.</param>
    /// <param name="longWindow">Long YZ window. Default <see cref="DefaultLongWindow"/>. Must be &gt; <paramref name="shortWindow"/>.</param>
    public YangZhangVolRatio(OhlcvBars bars, int shortWindow = DefaultShortWindow, int longWindow = DefaultLongWindow)
        : base(bars, Spec(shortWindow, longWindow))
    {
        _shortWindow = shortWindow;
        _longWindow = longWindow;
    }

    internal static IndicatorSpec Spec(int shortWindow, int longWindow)
    {
        shortWindow.ThrowIfBelow(2, nameof(shortWindow));
        longWindow.ThrowIfBelow(shortWindow + 1, nameof(longWindow));
        return IndicatorSpec.Of(longWindow, string.Invariant($"YZRatio({shortWindow}/{longWindow})"));
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
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
    public override void Apply(Axes axes) => PlotLine(axes, Compute());
}
