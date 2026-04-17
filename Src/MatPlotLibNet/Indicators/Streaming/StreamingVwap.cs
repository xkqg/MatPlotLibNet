// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming Volume-Weighted Average Price. O(1) cumulative: sum(price*volume) / sum(volume).</summary>
public sealed class StreamingVwap : StreamingIndicatorBase
{
    private double _cumulativePriceVolume;
    private double _cumulativeVolume;

    /// <inheritdoc />
    public override int WarmupPeriod => 1;

    /// <summary>Creates a streaming VWAP indicator.</summary>
    public StreamingVwap(int capacity = 10_000) : base(capacity)
    {
        Label = "VWAP";
    }

    /// <summary>Appends a price + volume pair.</summary>
    public void AppendWithVolume(double price, double volume)
    {
        _cumulativePriceVolume += price * volume;
        _cumulativeVolume += volume;
        Append(price); // triggers base ProcessedCount++ and output append
    }

    /// <inheritdoc />
    protected override double ComputeNext(double price)
    {
        return _cumulativeVolume > 0 ? _cumulativePriceVolume / _cumulativeVolume : price;
    }
}
