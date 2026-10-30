// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming Adx: the average directional index and the two directional indicators it comes from. It is the <see cref="AdxStep"/> the batch <see cref="Adx"/> folds over its input, taken
/// one bar at a time, so after each one it says what the batch says over everything so far; a value the batch does not have yet
/// is NaN. O(1) per input.</summary>
public sealed class StreamingAdx : StreamingIndicator<OhlcvBar>
{
    private AdxStep _step;
    private readonly int _period;

    /// <inheritdoc />
    public override int WarmupPeriod => 2 * _period + 1;

    /// <summary>Creates a streaming Adx.</summary>
    /// <param name="period">The period of the smoothing and of the mean of the directional index; at least 1.</param>
    /// <param name="capacity">How many points each output line keeps.</param>
    public StreamingAdx(int period = 14, int capacity = 10_000)
        : base([
            new StreamingLineSeries(capacity) { Label = $"ADX({period})" },
            new StreamingLineSeries(capacity) { Label = $"+DI({period})" },
            new StreamingLineSeries(capacity) { Label = $"-DI({period})" },
        ])
    {
        _step = new AdxStep(period);
        _period = period;
    }

    /// <inheritdoc />
    protected override void Step(OhlcvBar input, int index, Span<double> outputs)
    {
        var point = _step.Next(input.High, input.Low, input.Close);
        outputs[0] = point.Adx;
        outputs[1] = point.PlusDi;
        outputs[2] = point.MinusDi;
    }
}
