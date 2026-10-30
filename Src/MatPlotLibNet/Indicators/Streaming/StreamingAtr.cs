// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Indicators.Steps;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming Average True Range (Wilder's smoothing): the <see cref="AtrStep"/> the batch <see cref="Atr"/> folds over its
/// bars, taken one candle at a time. O(1) per candle. The first candle has no true range, so the first value comes with the
/// candle after <c>period</c> more.</summary>
public sealed class StreamingAtr : StreamingIndicator
{
    private readonly int _period;
    private AtrStep _step;

    /// <inheritdoc />
    public override int WarmupPeriod => _period + 1;

    /// <summary>Creates a streaming ATR indicator.</summary>
    public StreamingAtr(int period = 14, int capacity = 10_000) : base(capacity)
    {
        _period = period;
        _step = new AtrStep(period);
        Label = $"ATR({period})";
    }

    /// <inheritdoc />
    public override void AppendCandle(OhlcBar bar)
    {
        ProcessedCount++;
        double value = _step.Next(bar.High, bar.Low, bar.Close);
        OutputSeries[0].AppendPoint(ProcessedCount - 1, value);
    }

    /// <inheritdoc />
    protected override double ComputeNext(double price) => double.NaN; // use AppendCandle
}
