// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Indicators;

/// <summary>MACD (Moving Average Convergence Divergence) indicator. Shows trend direction and momentum.</summary>
/// <remarks>Adds three series: MACD line (fast EMA - slow EMA), signal line (EMA of MACD),
/// and a histogram (bar series showing the difference). Best placed in a separate subplot.</remarks>
public sealed class Macd : Indicator<MacdResult>
{
    private readonly double[] _prices;
    private readonly int _fastPeriod;
    private readonly int _slowPeriod;
    private readonly int _signalPeriod;

    /// <summary>Gets or sets the signal line color.</summary>
    public Color? SignalColor { get; set; }

    /// <summary>Creates a new MACD indicator.</summary>
    /// <param name="prices">The price data (typically close prices).</param>
    /// <param name="fastPeriod">Fast EMA period (default 12).</param>
    /// <param name="slowPeriod">Slow EMA period (default 26).</param>
    /// <param name="signalPeriod">Signal line EMA period (default 9).</param>
    public Macd(double[] prices, int fastPeriod = 12, int slowPeriod = 26, int signalPeriod = 9)
    {
        _prices = prices;
        _fastPeriod = fastPeriod;
        _slowPeriod = slowPeriod;
        _signalPeriod = signalPeriod;
        Label = $"MACD({fastPeriod},{slowPeriod},{signalPeriod})";
    }

    /// <inheritdoc />
    public override MacdResult Compute()
    {
        double[] fastEma = new Ema(_prices, _fastPeriod).Compute();
        double[] slowEma = new Ema(_prices, _slowPeriod).Compute();

        // Both EMAs are valid from slowPeriod-1 onward (slow EMA starts later)
        int start = _slowPeriod - 1;
        int macdLen = _prices.Length - start;
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
        var macdLine = result.MacdLine;
        var signalLine = result.SignalLine;
        var histogram = result.Histogram;
        int offset = _slowPeriod - 1;
        int signalOffset = offset + _signalPeriod - 1;

        // MACD line
        var macdX = VectorMath.Linspace(macdLine.Length, offset);
        var macdSeries = axes.Plot(macdX, macdLine);
        macdSeries.Label = Label;
        macdSeries.Color = Color ?? Colors.Tab10Blue;
        macdSeries.LineWidth = LineWidth;

        // Signal line
        var sigX = VectorMath.Linspace(signalLine.Length, signalOffset);
        var sigSeries = axes.Plot(sigX, signalLine);
        sigSeries.Label = "Signal";
        sigSeries.Color = SignalColor ?? Colors.Tab10Orange;
        sigSeries.LineWidth = LineWidth;

        // Histogram
        var histLabels = new string[histogram.Length];
        for (int i = 0; i < histogram.Length; i++) histLabels[i] = (signalOffset + i).ToString();
        var histSeries = axes.Bar(histLabels, histogram);
        histSeries.Label = "Histogram";
        histSeries.BarWidth = 0.6;
    }
}
