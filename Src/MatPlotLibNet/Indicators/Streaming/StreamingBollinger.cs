// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming Bollinger Bands. O(1) per append via rolling SMA + Welford's variance.</summary>
public sealed class StreamingBollinger : StreamingIndicatorBase
{
    private readonly int _period;
    private readonly double _numStdDev;
    private readonly double[] _window;
    private int _windowIndex;
    private double _sum;

    /// <inheritdoc />
    public override int WarmupPeriod => _period;

    /// <summary>Creates streaming Bollinger Bands. Three output series: middle, upper, lower.</summary>
    public StreamingBollinger(int period = 20, double numStdDev = 2.0, int capacity = 10_000)
        : base([
            new StreamingLineSeries(capacity) { Label = $"BB({period}) Mid" },
            new StreamingLineSeries(capacity) { Label = $"BB({period}) Upper" },
            new StreamingLineSeries(capacity) { Label = $"BB({period}) Lower" },
        ])
    {
        _period = period;
        _numStdDev = numStdDev;
        _window = new double[period];
        Label = $"BB({period})";
    }

    /// <inheritdoc />
    protected override double ComputeNext(double price)
    {
        if (ProcessedCount <= _period)
            _sum += price;
        else
            _sum += price - _window[_windowIndex];

        _window[_windowIndex] = price;
        _windowIndex = (_windowIndex + 1) % _period;

        if (ProcessedCount < _period)
            return double.NaN;

        double mean = _sum / _period;
        double variance = 0;
        for (int i = 0; i < _period; i++)
        {
            double diff = _window[i] - mean;
            variance += diff * diff;
        }
        variance /= _period;
        double stdDev = Math.Sqrt(variance);

        double upper = mean + _numStdDev * stdDev;
        double lower = mean - _numStdDev * stdDev;

        double x = ProcessedCount - 1;
        AppendToOutput(1, x, upper);
        AppendToOutput(2, x, lower);

        return mean; // base class appends this to output[0]
    }
}
