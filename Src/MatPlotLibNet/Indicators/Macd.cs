// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Indicators;

/// <summary>MACD (Moving Average Convergence Divergence) indicator. Shows trend direction and momentum.</summary>
/// <remarks>Adds three series: MACD line (fast EMA - slow EMA), signal line (EMA of MACD),
/// and a histogram (bar series showing the difference). Best placed in a separate subplot.</remarks>
public sealed class Macd : PriceIndicator<MacdResult>
{
    private readonly int _fastPeriod;
    private readonly int _slowPeriod;
    private readonly int _signalPeriod;

    public Color? SignalColor { get; set; }

    /// <summary>Creates a new MACD indicator.</summary>
    /// <param name="prices">The price data (typically close prices).</param>
    /// <param name="fastPeriod">Fast EMA period (default 12).</param>
    /// <param name="slowPeriod">Slow EMA period (default 26).</param>
    /// <param name="signalPeriod">Signal line EMA period (default 9).</param>
    public Macd(double[] prices, int fastPeriod = 12, int slowPeriod = 26, int signalPeriod = 9) : base(prices)
    {
        _fastPeriod = fastPeriod;
        _slowPeriod = slowPeriod;
        _signalPeriod = signalPeriod;
        Label = $"MACD({fastPeriod},{slowPeriod},{signalPeriod})";
    }

    /// <inheritdoc />
    public override MacdResult Compute()
    {
        double[] fastEma = new Ema(Prices, _fastPeriod).Compute();
        double[] slowEma = new Ema(Prices, _slowPeriod).Compute();

        // Both EMAs are valid from slowPeriod-1 onward (slow EMA starts later)
        int start = _slowPeriod - 1;
        int macdLen = Prices.Length - start;
        var macdLine = new double[macdLen];
        VectorMath.Subtract(
            ((ReadOnlySpan<double>)fastEma).Slice(start, macdLen),
            ((ReadOnlySpan<double>)slowEma).Slice(start, macdLen),
            macdLine);

        // Compute signal from valid MACD values only
        double[] signalSma = new Sma(macdLine, _signalPeriod).Compute();
        int sigStart = _signalPeriod - 1;
        int sigLen = signalSma.Length;
        var signalLine = signalSma;
        var histogram = new double[sigLen];
        if (sigLen > 0)
            VectorMath.Subtract(
                ((ReadOnlySpan<double>)macdLine).Slice(sigStart, sigLen),
                signalLine,
                histogram);

        return new MacdResult(macdLine, signalLine, histogram);
    }

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        var result = Compute();
        int offset = _slowPeriod - 1;
        int signalOffset = offset + _signalPeriod - 1;
        PlotSignal(axes, result.MacdLine, offset);
        PlotSignal(axes, result.SignalLine, signalOffset, "Signal", SignalColor ?? Colors.Tab10Orange);
        var histLabels = new string[result.Histogram.Length];
        for (int i = 0; i < result.Histogram.Length; i++) histLabels[i] = (signalOffset + i).ToString();
        var histSeries = axes.Bar(histLabels, result.Histogram);
        histSeries.Label = "Histogram";
        histSeries.BarWidth = 0.6;
    }
}
