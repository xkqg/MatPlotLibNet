// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators;

/// <summary>Base class for indicators that operate on OHLCV candlestick data.</summary>
/// <typeparam name="TResult">The typed computation result.</typeparam>
/// <remarks>Stores the full OHLCV dataframe once and provides shared derived computations
/// (true range, typical price, Donchian midpoint). Plotting helpers MakeX/PlotSignal
/// are inherited from <see cref="Indicator"/>.</remarks>
public abstract class CandleIndicator<TResult> : Indicator<TResult>
    where TResult : IIndicatorResult
{
    /// <summary>The open price of each bar. Empty when the bars were built without it, which is so for every indicator that does not read it.</summary>
    protected double[] Open { get; }

    /// <summary>The high price of each bar.</summary>
    protected double[] High { get; }

    /// <summary>The low price of each bar.</summary>
    protected double[] Low { get; }

    /// <summary>The close price of each bar.</summary>
    protected double[] Close { get; }

    /// <summary>The traded volume of each bar. Empty when the bars were built without it, which is so for every indicator that does not read it.</summary>
    protected double[] Volume { get; }

    /// <summary>The number of bars the derived computations walk: the length of the shortest of <see cref="High"/>, <see cref="Low"/> and <see cref="Close"/>.</summary>
    protected int BarCount { get; }

    /// <summary>Creates a new candle indicator from the given bars.</summary>
    protected CandleIndicator(OhlcvBars bars)
    {
        Open = bars.Open;
        High = bars.High;
        Low = bars.Low;
        Close = bars.Close;
        Volume = bars.Volume;
        BarCount = Math.Min(Math.Min(High.Length, Low.Length), Close.Length);
    }

    // ── Derived computations ──

    /// <summary>Computes the true range for each bar (bar 0 uses adjacent bars, length = BarCount - 1).</summary>
    protected double[] ComputeTrueRange()
    {
        int n = BarCount;
        var tr = new double[n - 1];
        for (int i = 0; i < n - 1; i++)
        {
            tr[i] = PriceSources.TrueRange(High[i + 1], Low[i + 1], Close[i]);
        }
        return tr;
    }

    /// <summary>Computes the typical price (H+L+C)/3 for each bar.</summary>
    protected double[] ComputeTypicalPrice()
    {
        int n = BarCount;
        var tp = new double[n];
        for (int i = 0; i < n; i++)
            tp[i] = PriceSources.TypicalPrice(High[i], Low[i], Close[i]);
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
