// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Numerics.Tensors;
using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Numerics;

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

    /// <inheritdoc />
    private protected override int BarsIn => BarCount;

    /// <summary>Creates a new candle indicator from the given bars.</summary>
    protected CandleIndicator(OhlcvBars bars) : this(bars, IndicatorSpec.None) { }

    /// <summary>An indicator of the library over the given bars, named and placed by its spec.</summary>
    /// <param name="bars">The bars.</param>
    /// <param name="spec">What the batch class's <c>Spec</c> factory said of the parameters.</param>
    private protected CandleIndicator(OhlcvBars bars, IndicatorSpec spec) : base(spec)
    {
        Open = bars.Open;
        High = bars.High;
        Low = bars.Low;
        Close = bars.Close;
        Volume = bars.Volume;
        BarCount = Math.Min(Math.Min(High.Length, Low.Length), Close.Length);
    }

    /// <summary>The values of a step that reads the high, the low and the close of a bar, from the indicator's first value on
    /// (<see cref="Indicator{TResult}.FoldBars{TFold, TOut, TLines}"/>).</summary>
    /// <typeparam name="TStep">The step.</typeparam>
    /// <param name="step">A new step.</param>
    /// <returns>One value per bar from the first value on.</returns>
    private protected double[] FoldHlc<TStep>(TStep step) where TStep : struct, IHlcStep<double> =>
        FoldHlc<TStep, double, ValueLine>(step, new ValueLine(ValueCount)).Values;

    /// <summary>The lines of a step that reads the high, the low and the close of a bar and says a point of several lines.</summary>
    /// <typeparam name="TStep">The step.</typeparam>
    /// <typeparam name="TOut">The point it says.</typeparam>
    /// <typeparam name="TLines">The lines the points are written to.</typeparam>
    /// <param name="step">A new step.</param>
    /// <param name="lines">The lines, made for <see cref="Indicator{TResult}.ValueCount"/> entries.</param>
    /// <returns>The lines, written.</returns>
    private protected TLines FoldHlc<TStep, TOut, TLines>(TStep step, TLines lines)
        where TStep : struct, IHlcStep<TOut>
        where TLines : struct, IOutputLines<TOut> =>
        FoldBars<HlcFold<TStep, TOut>, TOut, TLines>(new(this, step), lines);

    /// <summary>The values of a step that reads the close and the volume of a bar, from the indicator's first value on.</summary>
    /// <typeparam name="TStep">The step.</typeparam>
    /// <param name="step">A new step.</param>
    /// <returns>One value per bar from the first value on.</returns>
    private protected double[] FoldCloseVolume<TStep>(TStep step) where TStep : struct, ICloseVolumeStep<double> =>
        FoldBars<CloseVolumeFold<TStep>, double, ValueLine>(new(this, step), new ValueLine(ValueCount)).Values;

    /// <summary>The values of a step that reads the high, the low, the close and the volume of a bar, from the indicator's first
    /// value on.</summary>
    /// <typeparam name="TStep">The step.</typeparam>
    /// <param name="step">A new step.</param>
    /// <returns>One value per bar from the first value on.</returns>
    private protected double[] FoldHlcv<TStep>(TStep step) where TStep : struct, IHlcvStep<double> =>
        FoldHlcv<TStep, double, ValueLine>(step, new ValueLine(ValueCount)).Values;

    /// <summary>The lines of a step that reads the high, the low, the close and the volume of a bar and says a point of several
    /// lines.</summary>
    /// <typeparam name="TStep">The step.</typeparam>
    /// <typeparam name="TOut">The point it says.</typeparam>
    /// <typeparam name="TLines">The lines the points are written to.</typeparam>
    /// <param name="step">A new step.</param>
    /// <param name="lines">The lines, made for <see cref="Indicator{TResult}.ValueCount"/> entries.</param>
    /// <returns>The lines, written.</returns>
    private protected TLines FoldHlcv<TStep, TOut, TLines>(TStep step, TLines lines)
        where TStep : struct, IHlcvStep<TOut>
        where TLines : struct, IOutputLines<TOut> =>
        FoldBars<HlcvFold<TStep, TOut>, TOut, TLines>(new(this, step), lines);

    /// <summary>A step over the bars that takes the high, the low and the close of each.</summary>
    private struct HlcFold<TStep, TOut>(CandleIndicator<TResult> bars, TStep step) : IBarFold<TOut> where TStep : struct, IHlcStep<TOut>
    {
        private TStep _step = step;

        /// <inheritdoc />
        public TOut Next(int bar) => _step.Next(bars.High[bar], bars.Low[bar], bars.Close[bar]);
    }

    /// <summary>A step over the bars that takes the close and the volume of each.</summary>
    private struct CloseVolumeFold<TStep>(CandleIndicator<TResult> bars, TStep step) : IBarFold<double> where TStep : struct, ICloseVolumeStep<double>
    {
        private TStep _step = step;

        /// <inheritdoc />
        public double Next(int bar) => _step.Next(bars.Close[bar], bars.Volume[bar]);
    }

    /// <summary>A step over the bars that takes the high, the low, the close and the volume of each.</summary>
    private struct HlcvFold<TStep, TOut>(CandleIndicator<TResult> bars, TStep step) : IBarFold<TOut> where TStep : struct, IHlcvStep<TOut>
    {
        private TStep _step = step;

        /// <inheritdoc />
        public TOut Next(int bar) => _step.Next(bars.High[bar], bars.Low[bar], bars.Close[bar], bars.Volume[bar]);
    }

    // ── Derived computations ──

    /// <summary>Computes the true range for each bar after the first (bar 0 uses adjacent bars, length = BarCount - 1, none over
    /// fewer than two bars).</summary>
    protected double[] ComputeTrueRange()
    {
        var tr = new double[PairsIn];
        for (int i = 0; i < tr.Length; i++)
        {
            tr[i] = PriceSources.TrueRange(High[i + 1], Low[i + 1], Close[i]);
        }
        return tr;
    }

    /// <summary>The rolling mean over <paramref name="period"/> bars of a value worked out for each bar after the first, from that bar and
    /// the one before it: the walk over consecutive bar pairs that Amihud illiquidity, Corwin-Schultz and the ease of movement share, each
    /// with its own value.</summary>
    /// <param name="period">The window length.</param>
    /// <param name="valueAtBar">The value of the bar at the index it is given, which is at least 1: it may read the bar before.</param>
    /// <returns>The rolling mean: nothing while there are fewer pairs than a window, so nothing when the indicator has no value.</returns>
    private protected double[] RollingMeanOfPairs(int period, Func<int, double> valueAtBar)
    {
        var values = new double[PairsIn];
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = valueAtBar(i + 1);
        }

        return values.RollingMean(period);
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
        High.RollingMax(period, hhBuf);
        Low.RollingMin(period, llBuf);
        var result = new double[len];
        var hhSlice = ((ReadOnlySpan<double>)hhBuf).Slice(period - 1, len);
        var llSlice = ((ReadOnlySpan<double>)llBuf).Slice(period - 1, len);
        TensorPrimitives.Add(hhSlice, llSlice, result);
        TensorPrimitives.Divide(result, 2.0, result);
        return result;
    }
}
