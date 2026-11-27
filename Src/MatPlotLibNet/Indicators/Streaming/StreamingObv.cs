// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming On-Balance Volume: the running sum of the step the batch <see cref="Obv"/>
/// folds over its bars. The first bar opens the sum with its volume; a close above the one before adds the volume, a close
/// below subtracts it, an equal close keeps the sum. A bar that comes without a volume (NaN) makes the sum NaN from the first
/// bar that adds or subtracts it, and it stays NaN: the sum cannot be known, and a guess would be a number that was never
/// traded.</summary>
public sealed class StreamingObv : StreamingIndicator<OhlcvBar>
{
    private ObvStep _step;

    /// <inheritdoc />
    public override int WarmupPeriod => Spec.Warmup;

    /// <summary>Creates a streaming OBV indicator.</summary>
    public StreamingObv(int capacity = StreamingSeries.RestingCapacity) : base(Obv.Spec(), capacity)
    {
    }

    /// <summary>Appends a close price + volume pair: the bar whose four prices are the close.</summary>
    /// <param name="close">The close.</param>
    /// <param name="volume">The volume traded.</param>
    public void AppendWithVolume(double close, double volume) => Append(new OhlcvBar(close, close, close, close, volume));

    /// <inheritdoc />
    protected override void Step(OhlcvBar input, int index, Span<double> outputs) => outputs[0] = _step.Next(input.Close, input.Volume);
}
