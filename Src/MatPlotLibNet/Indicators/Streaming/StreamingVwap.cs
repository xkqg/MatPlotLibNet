// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;

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

    /// <inheritdoc />
    /// <remarks>A candle carries no volume, so it is a bar without one (see <see cref="AppendBar"/>).</remarks>
    public override void AppendCandle(OhlcBar bar) => AppendBar(OhlcvBar.From(bar));

    /// <inheritdoc />
    /// <remarks>The price of a bar is its typical price, (high + low + close) / 3, as in the batch <see cref="Vwap"/> over candles.
    /// A bar that comes without a volume (NaN) turns the sums NaN, so from there the average reads the price of each bar, which is
    /// what the batch does with a volume that is not a number.</remarks>
    public override void AppendBar(OhlcvBar bar) =>
        AppendWithVolume(PriceSources.TypicalPrice(bar.High, bar.Low, bar.Close), bar.Volume);

    /// <summary>Anchors the average at the next bar: forgets every bar so far, so the average from here on is the average price
    /// paid since this point (an anchored VWAP). The values already given stay as they were.</summary>
    public void AnchorHere() => _mean.Reset();

    /// <inheritdoc />
    protected override double ComputeNext(double price)
    {
        return _mean.Value(price);
    }
}
