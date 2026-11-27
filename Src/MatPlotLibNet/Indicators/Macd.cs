// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;
using System.Numerics.Tensors;

namespace MatPlotLibNet.Indicators;

/// <summary>MACD (Moving Average Convergence Divergence) indicator. Shows trend direction and momentum.</summary>
/// <remarks>Adds three series: MACD line (fast EMA - slow EMA), signal line (the exponential moving average of the MACD line over
/// the signal period, as Appel defines it and as pandas-ta and TA-Lib compute it), and a histogram (bar series showing the difference).
/// Best placed in a separate subplot.</remarks>
public sealed class Macd : PriceIndicator<MacdResult>
{
    /// <summary>The <c>fastPeriod</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultFastPeriod = 12;

    /// <summary>The <c>slowPeriod</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultSlowPeriod = 26;

    /// <summary>The <c>signalPeriod</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultSignalPeriod = 9;

    private readonly int _fastPeriod;
    private readonly int _slowPeriod;
    private readonly int _signalPeriod;

    /// <summary>The colour of the signal line. <see langword="null"/> (the default) draws it orange under the library's own themes and
    /// in the next colour of the cycle under the matplotlib themes, as matplotlib draws a line that names no colour.</summary>
    public Color? SignalColor { get; set; }

    /// <summary>Creates a new MACD indicator.</summary>
    /// <param name="prices">The price data (typically close prices).</param>
    /// <param name="fastPeriod">Fast EMA period (default <see cref="DefaultFastPeriod"/>).</param>
    /// <param name="slowPeriod">Slow EMA period (default <see cref="DefaultSlowPeriod"/>).</param>
    /// <param name="signalPeriod">Signal line EMA period (default <see cref="DefaultSignalPeriod"/>).</param>
    public Macd(double[] prices, int fastPeriod = DefaultFastPeriod, int slowPeriod = DefaultSlowPeriod, int signalPeriod = DefaultSignalPeriod) : base(prices, Spec(fastPeriod, slowPeriod, signalPeriod))
    {
        _fastPeriod = fastPeriod;
        _slowPeriod = slowPeriod;
        _signalPeriod = signalPeriod;
    }

    internal static IndicatorSpec Spec(int fastPeriod, int slowPeriod, int signalPeriod)
    {
        string label = string.Invariant($"MACD({fastPeriod},{slowPeriod},{signalPeriod})");
        return IndicatorSpec.Of(slowPeriod + signalPeriod - 2, label, label, "Signal", "Histogram");
    }

    /// <inheritdoc />
    public override MacdResult Compute()
    {
        // The line has a value where the slow EMA has one, and the signal from the signal EMA's first value of the line on: the
        // first value of each is its EMA's own (Ema.Spec).
        int start = Ema.Spec(_slowPeriod).FirstValueIndex;
        int macdLen = Prices.Length - start;
        if (macdLen <= 0) return new MacdResult([], [], []);

        double[] fastEma = new Ema(Prices, _fastPeriod).Compute();
        double[] slowEma = new Ema(Prices, _slowPeriod).Compute();

        var macdLine = new double[macdLen];
        TensorPrimitives.Subtract(
            ((ReadOnlySpan<double>)fastEma).Slice(start, macdLen),
            ((ReadOnlySpan<double>)slowEma).Slice(start, macdLen),
            macdLine);

        // The signal is the exponential mean of the valid MACD values (the Ema over them, whose step the streaming form takes), from
        // its first value on: the signalPeriod-th of them.
        int sigStart = Ema.Spec(_signalPeriod).FirstValueIndex;
        double[] signalLine = new Ema(macdLine, _signalPeriod).ComputeFromFirstValue();
        int sigLen = signalLine.Length;

        var histogram = new double[sigLen];
        if (sigLen > 0)
            TensorPrimitives.Subtract(
                ((ReadOnlySpan<double>)macdLine).Slice(sigStart, sigLen),
                signalLine,
                histogram);

        return new MacdResult(macdLine, signalLine, histogram);
    }

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        var result = Compute();
        PlotLine(axes, result.MacdLine);
        PlotLine(axes, result.SignalLine, LineLabel(1), SignalColor, IndicatorLook.Library.Lines.Second);

        // The histogram stands on the numeric X axis of the two lines, each bar at the bar its value is worked out at.
        var histSeries = new BarSeries(Positions(result.Histogram.Length), result.Histogram) { Label = LineLabel(2), BarWidth = 0.6 };
        axes.AddSeries(histSeries);
    }
}
