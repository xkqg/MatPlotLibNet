// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>What the indicators say on the closed-form bars the Fidelity suite draws, held against what pandas-ta 0.4.71b0 and TA-Lib
/// 0.8.3 print for the same bars (<c>close = 100 + 5 sin(2 pi i / 25) + 3 sin(2 pi i / 7)</c>, <c>high = close + 1 + 0.5 |sin(0.5 i)|</c>,
/// <c>low = close - 1 - 0.5 |cos(0.5 i)|</c>; 300 bars). pandas-ta runs with its own functions (<c>pandas_ta.Imports["talib"] = False</c>),
/// TA-Lib is its C library through <c>talib</c>. The indicators are defined by these two, not by a number this library once gave.</summary>
public sealed class IndicatorReferenceValueTests
{
    private const int Count = 300;

    private static readonly double[] Close = [.. Enumerable.Range(0, Count).Select(i => 100.0 + 5.0 * Math.Sin(2 * Math.PI * i / 25) + 3.0 * Math.Sin(2 * Math.PI * i / 7))];
    private static readonly double[] High = [.. Close.Select((c, i) => c + 1.0 + 0.5 * Math.Abs(Math.Sin(i * 0.5)))];
    private static readonly double[] Low = [.. Close.Select((c, i) => c - 1.0 - 0.5 * Math.Abs(Math.Cos(i * 0.5)))];
    // pandas_ta.macd(close, fast=12, slow=26, signal=9): MACD_12_26_9, MACDh_12_26_9, MACDs_12_26_9. The signal is
    // ema(macd from its first value, 9) (pandas_ta/momentum/macd.py:70).
    [Theory]
    [InlineData(33, -0.5159783530378313, 1.0617068708234934)]
    [InlineData(60, 0.8300700235806837, 0.5303519190396648)]
    [InlineData(100, -0.9185445143109137, 0.3903014450170348)]
    [InlineData(150, -0.853702237406958, 0.3816617043953927)]
    [InlineData(299, -0.920182691712422, -0.2623175010645059)]
    public void MacdSignal_IsTheExponentialMeanOfTheMacdLine_AsPandasTaHasIt(int bar, double signal, double histogram)
    {
        var result = new Macd(Close, 12, 26, 9).Compute();

        signal.AssertCloseTo(result.SignalLine[bar - 33], relative: 1e-9, floor: 1, what: $"signal[{bar}]");
        histogram.AssertCloseTo(result.Histogram[bar - 33], relative: 1e-9, floor: 1, what: $"histogram[{bar}]");
    }

    // talib.MACD(close, 12, 26, 9)[1]: the same line from the bar where TA-Lib's seeds no longer show.
    [Fact]
    public void MacdSignal_IsTheOneTaLibHas_OnceItsSeedHasFaded() =>
        (-0.9201826917124017).AssertCloseTo(new Macd(Close, 12, 26, 9).Compute().SignalLine[299 - 33], relative: 1e-9, floor: 1, what: "signal[299]");

    // pandas_ta.stoch(high, low, close, k=14, d=3): STOCHk_14_3_3 is the raw %K averaged over smooth_k = 3, STOCHd_14_3_3 the mean of it
    // over d = 3 (pandas_ta/momentum/stoch.py:53,85); talib.STOCH(high, low, close, 14, 3, 0, 3, 0) says the same from its first bar.
    [Theory]
    [InlineData(15, 20.88844688487754, double.NaN)]
    [InlineData(17, 22.772013431317067, 23.301175594588905)]
    [InlineData(60, 75.03632509311058, 84.85034450593804)]
    [InlineData(100, 65.33879162476538, 42.75975893174921)]
    [InlineData(150, 73.14865629627376, 54.70048991108331)]
    [InlineData(299, 43.701427625414794, 44.9321028327096)]
    public void Stochastic_IsTheSlowOne_AsPandasTaAndTaLibHaveIt(int bar, double k, double d)
    {
        var result = new Stochastic(OhlcvBars.Hlc(High, Low, Close), 14, 3).Compute();

        k.AssertCloseTo(result.K[bar - 15], relative: 1e-9, floor: 1, what: $"%K[{bar}]");
        if (!double.IsNaN(d)) d.AssertCloseTo(result.D[bar - 17], relative: 1e-9, floor: 1, what: $"%D[{bar}]");
    }

    // talib.ADX(high, low, close, 14): Wilder's ADX, each value (previous x 13 + this directional index) / 14 from the mean of the first 14.
    // pandas_ta.adx says the same line from its own first bars (rma over the directional index) and, once those have faded, to 1e-7. The seed
    // the directional indicators smooth from differs by one bar between Wilder (the library) and TA-Lib, a gap in the ADX of 0.7 percent at
    // bar 60 that fades to 1e-9 by bar 299.
    [Theory]
    [InlineData(60, 15.757922384760695, 1e-2)]
    [InlineData(100, 14.405351099737068, 1e-3)]
    [InlineData(150, 14.498085351865342, 1e-3)]
    [InlineData(299, 12.566239135400673, 1e-6)]
    public void Adx_IsWildersSmoothingOfTheDirectionalIndex_AsTaLibHasIt(int bar, double adx, double tolerance)
    {
        var result = new Adx(OhlcvBars.Hlc(High, Low, Close), 14).ComputeFull();

        adx.AssertCloseTo(result.Adx[bar - 28], relative: tolerance, floor: 1, what: $"adx[{bar}]");
    }

    // The directional indicators were Wilder's by the book already: talib.PLUS_DI / MINUS_DI differ only by the seed they smooth from
    // (TA-Lib sums 13 movements, then smooths with the 14th; Wilder sums 14), a gap that fades: 0.6 percent of the value at the first bar, 2e-4 at bar 60, 1e-9 at bar 299.
    [Theory]
    [InlineData(150, 29.712181237409812, 24.616121458389895, 1e-4)]
    [InlineData(299, 21.262287428649973, 27.05314791806226, 1e-8)]
    public void TheDirectionalIndicators_AreTheOnesTaLibHas_OnceItsSeedHasFaded(int bar, double plus, double minus, double tolerance)
    {
        var result = new Adx(OhlcvBars.Hlc(High, Low, Close), 14).ComputeFull();

        plus.AssertCloseTo(result.PlusDi[bar - 28], relative: tolerance, floor: 1, what: $"+DI[{bar}]");
        minus.AssertCloseTo(result.MinusDi[bar - 28], relative: tolerance, floor: 1, what: $"-DI[{bar}]");
    }
}
