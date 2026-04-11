// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators;

/// <summary>Base class for indicators that operate on OHLCV candlestick data.</summary>
/// <typeparam name="TResult">The typed computation result.</typeparam>
/// <remarks>Stores the full OHLCV dataframe once and provides shared derived computations
/// (true range, typical price, Donchian midpoint). Plotting helpers MakeX/PlotSignal
/// are inherited from <see cref="Indicator"/>.</remarks>
public abstract class CandleIndicator<TResult> : Indicator<TResult>
    where TResult : IIndicatorResult
{
    /// <summary>Gets the open prices.</summary>
    protected double[] Open { get; }

    /// <summary>Gets the high prices.</summary>
    protected double[] High { get; }

    /// <summary>Gets the low prices.</summary>
    protected double[] Low { get; }

    /// <summary>Gets the close prices.</summary>
    protected double[] Close { get; }

    /// <summary>Gets the volume data (empty when not supplied).</summary>
    protected double[] Volume { get; }

    /// <summary>Gets the number of bars in the dataset.</summary>
    protected int BarCount { get; }

    /// <summary>Creates a new candle indicator from HLC data.</summary>
    protected CandleIndicator(double[] high, double[] low, double[] close)
        : this([], high, low, close, []) { }

    /// <summary>Creates a new candle indicator from full OHLCV data.</summary>
    protected CandleIndicator(double[] open, double[] high, double[] low,
                              double[] close, double[] volume)
    {
        Open = open;
        High = high;
        Low = low;
        Close = close;
        Volume = volume;
        BarCount = Math.Min(Math.Min(high.Length, low.Length), close.Length);
    }

    // ── Derived computations ──

    /// <summary>Computes the true range for each bar (bar 0 uses adjacent bars, length = BarCount - 1).</summary>
    protected double[] ComputeTrueRange()
    {
        int n = BarCount;
        var tr = new double[n - 1];
        for (int i = 0; i < n - 1; i++)
        {
            double h = High[i + 1], l = Low[i + 1], pc = Close[i];
            tr[i] = Math.Max(h - l, Math.Max(Math.Abs(h - pc), Math.Abs(l - pc)));
        }
        return tr;
    }

    /// <summary>Computes the typical price (H+L+C)/3 for each bar.</summary>
    protected double[] ComputeTypicalPrice()
    {
        int n = BarCount;
        var tp = new double[n];
        for (int i = 0; i < n; i++)
            tp[i] = (High[i] + Low[i] + Close[i]) / 3.0;
        return tp;
    }

    /// <summary>Computes the Donchian channel midpoint (highest-high + lowest-low) / 2
    /// over a rolling window.</summary>
    protected double[] ComputeDonchianMid(int period)
    {
        int n = BarCount;
        if (n < period) return [];
        int len = n - period + 1;
        var hhBuf = new double[n];
        var llBuf = new double[n];
        Numerics.VectorMath.RollingMax(High, period, hhBuf);
        Numerics.VectorMath.RollingMin(Low, period, llBuf);
        var result = new double[len];
        var hhSlice = ((ReadOnlySpan<double>)hhBuf).Slice(period - 1, len);
        var llSlice = ((ReadOnlySpan<double>)llBuf).Slice(period - 1, len);
        Numerics.VectorMath.Add(hhSlice, llSlice, result);
        Numerics.VectorMath.Divide(result, 2.0, result);
        return result;
    }
}
