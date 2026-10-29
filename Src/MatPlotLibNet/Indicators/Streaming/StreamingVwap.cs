// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming Volume-Weighted Average Price. O(1) cumulative: sum(price*volume) / sum(volume).</summary>
public sealed class StreamingVwap : StreamingIndicator
{
    private VolumeWeightedMean _mean;

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
        _mean.Add(price, volume);
        Append(price); // triggers base ProcessedCount++ and output append
    }

    /// <summary>Anchors the average at the next bar: forgets every bar so far, so the average from here on is the average price
    /// paid since this point (an anchored VWAP). The values already given stay as they were.</summary>
    public void AnchorHere() => _mean.Reset();

    /// <inheritdoc />
    protected override double ComputeNext(double price)
    {
        return _mean.Value(price);
    }
}
