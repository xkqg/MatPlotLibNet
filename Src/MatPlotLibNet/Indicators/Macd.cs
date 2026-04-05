// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Indicators;

/// <summary>MACD (Moving Average Convergence Divergence) indicator. Shows trend direction and momentum.</summary>
/// <remarks>Adds three series: MACD line (fast EMA - slow EMA), signal line (EMA of MACD),
/// and a histogram (bar series showing the difference). Best placed in a separate subplot.</remarks>
public sealed class Macd : Indicator
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
    public override void Apply(Axes axes)
    {
        var (macdLine, signalLine, histogram) = Compute(_prices, _fastPeriod, _slowPeriod, _signalPeriod);
        int offset = _slowPeriod - 1;
        int signalOffset = offset + _signalPeriod - 1;

        // MACD line
        var macdX = new double[macdLine.Length];
        for (int i = 0; i < macdLine.Length; i++) macdX[i] = offset + i;
        var macdSeries = axes.Plot(macdX, macdLine);
        macdSeries.Label = Label;
        macdSeries.Color = Color ?? Styling.Color.FromHex("#1f77b4");
        macdSeries.LineWidth = LineWidth;

        // Signal line
        var sigX = new double[signalLine.Length];
        for (int i = 0; i < signalLine.Length; i++) sigX[i] = signalOffset + i;
        var sigSeries = axes.Plot(sigX, signalLine);
        sigSeries.Label = "Signal";
        sigSeries.Color = SignalColor ?? Styling.Color.FromHex("#ff7f0e");
        sigSeries.LineWidth = LineWidth;

        // Histogram
        var histLabels = new string[histogram.Length];
        for (int i = 0; i < histogram.Length; i++) histLabels[i] = (signalOffset + i).ToString();
        var histSeries = axes.Bar(histLabels, histogram);
        histSeries.Label = "Histogram";
        histSeries.BarWidth = 0.6;
    }

    /// <summary>Computes MACD line, signal line, and histogram.</summary>
    /// <returns>Tuple of (MACD line, signal line, histogram) arrays.</returns>
    public static (double[] MacdLine, double[] SignalLine, double[] Histogram) Compute(
        double[] prices, int fastPeriod = 12, int slowPeriod = 26, int signalPeriod = 9)
    {
        var fastEma = Ema.Compute(prices, fastPeriod);
        var slowEma = Ema.Compute(prices, slowPeriod);

        // Both EMAs are valid from slowPeriod-1 onward (slow EMA starts later)
        int start = slowPeriod - 1;
        int macdLen = prices.Length - start;
        var macdLine = new double[macdLen];
        for (int i = 0; i < macdLen; i++)
            macdLine[i] = fastEma[start + i] - slowEma[start + i];

        // Compute signal from valid MACD values only
        var signalEma = Sma.Compute(macdLine, signalPeriod); // Use SMA for signal to avoid NaN seed issues
        int sigStart = signalPeriod - 1;
        int sigLen = signalEma.Length;
        var signalLine = signalEma;
        var histogram = new double[sigLen];
        for (int i = 0; i < sigLen; i++)
            histogram[i] = macdLine[sigStart + i] - signalLine[i];

        return (macdLine, signalLine, histogram);
    }
}
