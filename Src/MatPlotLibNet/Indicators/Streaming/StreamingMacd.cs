// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming MACD (Moving Average Convergence Divergence). Three outputs: MACD line, signal, histogram. The line is the
/// fast exponential mean less the slow one, from the bar the slow one has a value; the signal is the exponential mean of the line, an
/// exponential mean as in the batch <see cref="Macd"/>, and the histogram is the line less the signal.</summary>
public sealed class StreamingMacd : StreamingIndicator<double>
{
    private EmaStep _fast;
    private EmaStep _slow;
    private EmaStep _signal;

    /// <inheritdoc />
    public override int WarmupPeriod => Spec.Warmup;

    /// <summary>Creates streaming MACD. Three output series: MACD line, signal line, histogram.</summary>
    /// <param name="fastPeriod">The period of the fast mean.</param>
    /// <param name="slowPeriod">The period of the slow mean.</param>
    /// <param name="signalPeriod">The period of the mean of the MACD line.</param>
    /// <param name="capacity">The number of points each output series keeps.</param>
    public StreamingMacd(int fastPeriod = Macd.DefaultFastPeriod, int slowPeriod = Macd.DefaultSlowPeriod, int signalPeriod = Macd.DefaultSignalPeriod, int capacity = StreamingSeries.RestingCapacity)
        : base(Macd.Spec(fastPeriod, slowPeriod, signalPeriod), capacity)
    {
        _fast = new EmaStep(fastPeriod);
        _slow = new EmaStep(slowPeriod);
        _signal = new EmaStep(signalPeriod);
    }

    /// <inheritdoc />
    protected override void Step(double input, int index, Span<double> outputs)
    {
        double fast = _fast.Next(input);
        double slow = _slow.Next(input);
        if (double.IsNaN(slow)) return;

        double line = fast - slow;
        double signal = _signal.Next(line);
        outputs[0] = line;
        outputs[1] = signal;
        outputs[2] = line - signal;
    }
}
