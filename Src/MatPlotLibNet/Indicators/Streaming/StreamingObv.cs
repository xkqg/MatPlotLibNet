// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Indicators.Steps;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming On-Balance Volume: the running sum of <see cref="ObvStep"/>, the same step the batch <see cref="Obv"/>
/// folds over its bars. The first bar opens the sum with its volume; a close above the one before adds the volume, a close
/// below subtracts it, an equal close keeps the sum. A bar that comes without a volume (NaN) makes the sum NaN from the first
/// bar that adds or subtracts it, and it stays NaN: the sum cannot be known, and a guess would be a number that was never
/// traded.</summary>
public sealed class StreamingObv : StreamingIndicator
{
    private ObvStep _step;
    private double _volume = double.NaN;

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
        _volume = volume;
        try
        {
            Append(close);
        }
        finally
        {
            _volume = double.NaN;
        }
    }

    /// <inheritdoc />
    /// <remarks>A candle carries no volume, so it counts as a bar without one.</remarks>
    public override void AppendCandle(OhlcBar bar) => Append(bar.Close);

    /// <inheritdoc />
    public override void AppendBar(OhlcvBar bar) => AppendWithVolume(bar.Close, bar.Volume);

    /// <inheritdoc />
    protected override double ComputeNext(double price) => _step.Next(price, _volume);
}
