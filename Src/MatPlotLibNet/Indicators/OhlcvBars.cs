// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators;

/// <summary>The candle data a <see cref="CandleIndicator{TResult}"/> is built from: High, Low and Close always
/// present, Open and Volume present only when the indicator reads them. The named-values type every indicator
/// constructor that used to take 3-5 parallel <c>double[]</c> arrays now takes instead — the same reasoning as
/// <see cref="Models.Series.BarGroup"/>: several arrays that must travel together and stay the same
/// length are one value, not several positional ones.</summary>
/// <param name="High">High price per bar.</param>
/// <param name="Low">Low price per bar.</param>
/// <param name="Close">Close price per bar. Equal to <see cref="High"/> when an indicator that reads only High and
/// Low (<see cref="Hl"/>) or High, Low and Volume (<see cref="Hlv"/>) needs no Close of its own — matching what
/// those indicators' own constructors already did before this type existed.</param>
/// <param name="Open">Open price per bar, or empty when the indicator does not read it.</param>
/// <param name="Volume">Volume per bar, or empty when the indicator does not read it.</param>
public readonly record struct OhlcvBars(double[] High, double[] Low, double[] Close, double[] Open, double[] Volume)
{
    /// <summary>Bars for an indicator that reads High, Low and Close.</summary>
    public static OhlcvBars Hlc(double[] high, double[] low, double[] close)
    {
        high.ThrowIfMisaligned(low);
        high.ThrowIfMisaligned(close);
        return new(high, low, close, [], []);
    }

    /// <summary>Bars for an indicator that reads only High and Low.</summary>
    public static OhlcvBars Hl(double[] high, double[] low)
    {
        high.ThrowIfMisaligned(low);
        return new(high, low, high, [], []);
    }

    /// <summary>Bars for an indicator that reads High, Low and Volume.</summary>
    public static OhlcvBars Hlv(double[] high, double[] low, double[] volume)
    {
        high.ThrowIfMisaligned(low);
        high.ThrowIfMisaligned(volume);
        return new(high, low, high, [], volume);
    }

    /// <summary>Bars for an indicator that reads only Close and Volume.</summary>
    public static OhlcvBars Cv(double[] close, double[] volume)
    {
        close.ThrowIfMisaligned(volume);
        return new(close, close, close, [], volume);
    }

    /// <summary>Bars for an indicator that reads Open, High, Low and Close.</summary>
    public static OhlcvBars Ohlc(double[] open, double[] high, double[] low, double[] close)
    {
        open.ThrowIfMisaligned(high);
        high.ThrowIfMisaligned(low);
        high.ThrowIfMisaligned(close);
        return new(high, low, close, open, []);
    }

    /// <summary>Bars for an indicator that reads High, Low, Close and Volume.</summary>
    public static OhlcvBars Hlcv(double[] high, double[] low, double[] close, double[] volume)
    {
        high.ThrowIfMisaligned(low);
        high.ThrowIfMisaligned(close);
        high.ThrowIfMisaligned(volume);
        return new(high, low, close, [], volume);
    }
}
