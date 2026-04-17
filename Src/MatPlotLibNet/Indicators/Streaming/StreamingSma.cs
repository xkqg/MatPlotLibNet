// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming Simple Moving Average. O(1) per append via circular sum buffer.</summary>
public sealed class StreamingSma : StreamingIndicatorBase
{
    private readonly int _period;
    private readonly double[] _window;
    private int _windowIndex;
    private double _sum;

    /// <inheritdoc />
    public override int WarmupPeriod => _period;

    /// <summary>Creates a streaming SMA indicator.</summary>
    /// <param name="period">Number of values to average.</param>
    /// <param name="capacity">Output series buffer capacity.</param>
    public StreamingSma(int period, int capacity = 10_000) : base(capacity)
    {
        _period = period;
        _window = new double[period];
        Label = $"SMA({period})";
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

        return ProcessedCount >= _period ? _sum / _period : double.NaN;
    }
}
