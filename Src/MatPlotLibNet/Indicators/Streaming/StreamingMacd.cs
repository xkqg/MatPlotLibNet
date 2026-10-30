// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming MACD (Moving Average Convergence Divergence). Three outputs: MACD line, signal, histogram. The line is the
/// fast <see cref="EmaStep"/> less the slow one, from the bar the slow one has a value; the signal is the simple mean of the last
/// <c>signal</c> values of the line (as in the batch <see cref="Macd"/>), and the histogram is the line less the signal.</summary>
public sealed class StreamingMacd : StreamingIndicator
{
    private readonly int _slowPeriod;
    private readonly int _signalPeriod;
    private EmaStep _fast;
    private EmaStep _slow;
    private RollingMeanStep _signal;

    /// <inheritdoc />
    public override int WarmupPeriod => _slowPeriod + _signalPeriod - 1;

    /// <summary>Creates streaming MACD. Three output series: MACD line, signal line, histogram.</summary>
    public StreamingMacd(int fastPeriod = 12, int slowPeriod = 26, int signalPeriod = 9, int capacity = 10_000)
        : base([
            new StreamingLineSeries(capacity) { Label = "MACD" },
            new StreamingLineSeries(capacity) { Label = "Signal" },
            new StreamingLineSeries(capacity) { Label = "Histogram" },
        ])
    {
        _slowPeriod = slowPeriod;
        _signalPeriod = signalPeriod;
        _fast = new EmaStep(fastPeriod);
        _slow = new EmaStep(slowPeriod);
        _signal = new RollingMeanStep(signalPeriod);
        Label = $"MACD({fastPeriod},{slowPeriod},{signalPeriod})";
    }

    /// <inheritdoc />
    protected override double ComputeNext(double price)
    {
        double fast = _fast.Next(price);
        double slow = _slow.Next(price);
        if (ProcessedCount < _slowPeriod) return double.NaN;

        double line = fast - slow;
        double signal = _signal.Next(line);
        SetOutput(1, signal);
        SetOutput(2, line - signal);
        return line;
    }
}
