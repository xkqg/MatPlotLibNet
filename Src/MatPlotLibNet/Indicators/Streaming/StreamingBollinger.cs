// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming Bollinger Bands. The mean is the step the batch bands' moving average adds up, O(1) per
/// append; the spread is the population standard deviation of the step's window about that mean, as
/// the batch bands take it.</summary>
public sealed class StreamingBollinger : StreamingIndicator<double>
{
    private readonly double _numStdDev;
    private RollingMeanStep _mean;

    /// <inheritdoc />
    public override int WarmupPeriod => Spec.Warmup;

    /// <summary>Creates streaming Bollinger Bands. Three output series: middle, upper, lower.</summary>
    public StreamingBollinger(int period = BollingerBands.DefaultPeriod, double numStdDev = BollingerBands.DefaultStdDevMultiplier, int capacity = StreamingSeries.RestingCapacity)
        : base(BollingerBands.Spec(period, numStdDev), capacity)
    {
        _numStdDev = numStdDev;
        _mean = new RollingMeanStep(period);
    }

    /// <inheritdoc />
    protected override void Step(double input, int index, Span<double> outputs)
    {
        double mean = _mean.Next(input);
        if (!_mean.IsFull) return;

        double stdDev = _mean.Window.StdDev(mean);
        outputs[0] = mean;
        outputs[1] = mean + _numStdDev * stdDev;
        outputs[2] = mean - _numStdDev * stdDev;
    }
}
