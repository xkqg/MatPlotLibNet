// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming Commodity Channel Index: the mean of the last <c>period</c> typical prices is the moving-average step the
/// batch <see cref="Cci"/>'s moving average adds up, and the index of that window is worked out by the arithmetic the batch reads.
/// O(period) per bar (the mean deviation of the window).</summary>
public sealed class StreamingCci : StreamingIndicator<OhlcvBar>
{
    private RollingMeanStep _mean;

    /// <inheritdoc />
    public override int WarmupPeriod => Spec.Warmup;

    /// <summary>Creates a streaming CCI indicator.</summary>
    public StreamingCci(int period = Cci.DefaultPeriod, int capacity = StreamingSeries.RestingCapacity) : base(Cci.Spec(period), capacity)
    {
        _mean = new RollingMeanStep(period);
    }

    /// <inheritdoc />
    protected override void Step(OhlcvBar input, int index, Span<double> outputs)
    {
        double typical = PriceSources.TypicalPrice(input.High, input.Low, input.Close);
        double mean = _mean.Next(typical);
        if (_mean.IsFull) outputs[0] = _mean.Window.ChannelIndex(mean, typical);
    }
}
