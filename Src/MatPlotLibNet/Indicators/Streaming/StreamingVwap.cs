// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Indicators.Steps;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming Volume-Weighted Average Price. O(1) cumulative: sum(price*volume) / sum(volume). It reads a price and the
/// volume traded at it (<see cref="TradedPrice"/>); a bar is its typical price, (high + low + close) / 3, as in the batch
/// <see cref="Vwap"/> over candles. A bar that comes without a volume (NaN) turns the sums NaN, so from there the average reads the
/// price of each bar, which is what the batch does with a volume that is not a number.</summary>
public sealed class StreamingVwap : StreamingIndicator<TradedPrice>
{
    private VolumeWeightedMean _mean;

    /// <inheritdoc />
    public override int WarmupPeriod => Spec.Warmup;

    /// <summary>Creates a streaming VWAP indicator.</summary>
    public StreamingVwap(int capacity = StreamingSeries.RestingCapacity) : base(Vwap.Spec(), capacity)
    {
    }

    /// <summary>Appends a price + volume pair.</summary>
    /// <param name="price">The price.</param>
    /// <param name="volume">The volume traded at it.</param>
    public void AppendWithVolume(double price, double volume) => Append(new TradedPrice(price, volume));

    /// <summary>Anchors the average at the next bar: forgets every bar so far, so the average from here on is the average price
    /// paid since this point (an anchored VWAP). The values already given stay as they were.</summary>
    public void AnchorHere() => _mean.Reset();

    /// <inheritdoc />
    /// <remarks>A bar is its typical price, with its volume.</remarks>
    protected override TradedPrice FromBar(OhlcvBar bar) => new(PriceSources.TypicalPrice(bar.High, bar.Low, bar.Close), bar.Volume);

    /// <inheritdoc />
    protected override void Step(TradedPrice input, int index, Span<double> outputs)
    {
        _mean.Add(input.Price, input.Volume);
        outputs[0] = _mean.Value(input.Price);
    }
}
