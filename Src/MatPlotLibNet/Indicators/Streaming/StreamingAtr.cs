// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming Average True Range (Wilder's smoothing). O(1) per candle.</summary>
public sealed class StreamingAtr : StreamingIndicatorBase
{
    private readonly int _period;
    private double _atr;
    private double _prevClose;
    private double _trSum;
    private bool _hasPrev;

    /// <inheritdoc />
    public override int WarmupPeriod => _period + 1;

    /// <summary>Creates a streaming ATR indicator.</summary>
    public StreamingAtr(int period = 14, int capacity = 10_000) : base(capacity)
    {
        _period = period;
        Label = $"ATR({period})";
    }

    /// <inheritdoc />
    public override void AppendCandle(OhlcBar bar)
    {
        ProcessATR(bar.High, bar.Low, bar.Close);
    }

    /// <inheritdoc />
    protected override double ComputeNext(double price) => double.NaN; // use AppendCandle

    private void ProcessATR(double high, double low, double close)
    {
        ProcessedCount++;

        double tr;
        if (!_hasPrev)
        {
            tr = high - low;
            _hasPrev = true;
        }
        else
        {
            tr = Math.Max(high - low, Math.Max(Math.Abs(high - _prevClose), Math.Abs(low - _prevClose)));
        }

        _prevClose = close;

        if (ProcessedCount <= _period + 1)
        {
            _trSum += tr;
            if (ProcessedCount == _period + 1)
            {
                _atr = _trSum / _period;
                OutputSeries[0].AppendPoint(ProcessedCount - 1, _atr);
            }
            else
            {
                OutputSeries[0].AppendPoint(ProcessedCount - 1, double.NaN);
            }
        }
        else
        {
            _atr = (_atr * (_period - 1) + tr) / _period;
            OutputSeries[0].AppendPoint(ProcessedCount - 1, _atr);
        }
    }
}
