// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming Commodity Channel Index. O(n/period) per candle (mean deviation scan).</summary>
public sealed class StreamingCci : StreamingIndicatorBase
{
    private readonly int _period;
    private readonly double[] _typicalPrices;
    private int _ringIndex;

    /// <inheritdoc />
    public override int WarmupPeriod => _period;

    /// <summary>Creates a streaming CCI indicator.</summary>
    public StreamingCci(int period = 20, int capacity = 10_000) : base(capacity)
    {
        _period = period;
        _typicalPrices = new double[period];
        Label = $"CCI({period})";
    }

    /// <inheritdoc />
    public override void AppendCandle(OhlcBar bar)
    {
        ProcessedCount++;
        double tp = (bar.High + bar.Low + bar.Close) / 3.0;

        _typicalPrices[_ringIndex] = tp;
        _ringIndex = (_ringIndex + 1) % _period;

        double x = ProcessedCount - 1;
        if (ProcessedCount < _period) { OutputSeries[0].AppendPoint(x, double.NaN); return; }

        int count = Math.Min(ProcessedCount, _period);
        double mean = 0;
        for (int i = 0; i < count; i++) mean += _typicalPrices[i];
        mean /= count;

        double meanDev = 0;
        for (int i = 0; i < count; i++) meanDev += Math.Abs(_typicalPrices[i] - mean);
        meanDev /= count;

        double cci = meanDev == 0 ? 0 : (tp - mean) / (0.015 * meanDev);
        OutputSeries[0].AppendPoint(x, cci);
    }

    /// <inheritdoc />
    protected override double ComputeNext(double price) => double.NaN; // use AppendCandle
}
