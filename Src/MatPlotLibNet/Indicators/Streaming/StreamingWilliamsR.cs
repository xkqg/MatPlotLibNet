// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming Williams %R. Rolling min/max over N periods. O(1) amortized.</summary>
public sealed class StreamingWilliamsR : StreamingIndicatorBase
{
    private readonly int _period;
    private readonly double[] _highs;
    private readonly double[] _lows;
    private int _ringIndex;

    /// <inheritdoc />
    public override int WarmupPeriod => _period;

    /// <summary>Creates a streaming Williams %R indicator.</summary>
    public StreamingWilliamsR(int period = 14, int capacity = 10_000) : base(capacity)
    {
        _period = period;
        _highs = new double[period];
        _lows = new double[period];
        Label = $"W%R({period})";
    }

    /// <inheritdoc />
    public override void AppendCandle(OhlcBar bar)
    {
        ProcessedCount++;
        _highs[_ringIndex] = bar.High;
        _lows[_ringIndex] = bar.Low;
        _ringIndex = (_ringIndex + 1) % _period;

        double x = ProcessedCount - 1;
        if (ProcessedCount < _period) { OutputSeries[0].AppendPoint(x, double.NaN); return; }

        int count = Math.Min(ProcessedCount, _period);
        double high = double.MinValue, low = double.MaxValue;
        for (int i = 0; i < count; i++)
        {
            high = Math.Max(high, _highs[i]);
            low = Math.Min(low, _lows[i]);
        }

        double wr = high == low ? -50 : (high - bar.Close) / (high - low) * -100;
        OutputSeries[0].AppendPoint(x, wr);
    }

    /// <inheritdoc />
    protected override double ComputeNext(double price) => double.NaN; // use AppendCandle
}
