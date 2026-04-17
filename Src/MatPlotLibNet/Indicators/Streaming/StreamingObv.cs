// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming On-Balance Volume. O(1) cumulative: OBV += sign(close change) * volume.</summary>
public sealed class StreamingObv : StreamingIndicatorBase
{
    private double _obv;
    private double _prevClose;
    private double _lastVolume;

    /// <inheritdoc />
    public override int WarmupPeriod => 1;

    /// <summary>Creates a streaming OBV indicator.</summary>
    public StreamingObv(int capacity = 10_000) : base(capacity)
    {
        Label = "OBV";
    }

    /// <summary>Appends a close price + volume pair.</summary>
    public void AppendWithVolume(double close, double volume)
    {
        _lastVolume = volume;
        Append(close);
    }

    /// <inheritdoc />
    public override void AppendCandle(OhlcBar bar) => Append(bar.Close);

    /// <inheritdoc />
    protected override double ComputeNext(double price)
    {
        if (ProcessedCount == 1)
        {
            _prevClose = price;
            return 0;
        }

        if (price > _prevClose) _obv += _lastVolume > 0 ? _lastVolume : 1;
        else if (price < _prevClose) _obv -= _lastVolume > 0 ? _lastVolume : 1;

        _prevClose = price;
        return _obv;
    }
}
