// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Indicators.Steps;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming ForceIndex: the force index: the volume times the change of the close, smoothed by an exponential average. It is the <see cref="ForceIndexStep"/> the batch <see cref="ForceIndex"/> folds over its input, taken
/// one bar at a time, so after each one it says what the batch says over everything so far; a value the batch does not have yet
/// is NaN. O(1) per input.</summary>
public sealed class StreamingForceIndex : StreamingIndicator<OhlcvBar>
{
    private ForceIndexStep _step;

    /// <inheritdoc />
    public override int WarmupPeriod => 2;

    /// <summary>Creates a streaming ForceIndex.</summary>
    /// <param name="period">The period of the smoothing; 1 leaves the raw force.</param>
    /// <param name="capacity">How many points each output line keeps.</param>
    public StreamingForceIndex(int period = 13, int capacity = 10_000)
        : base(capacity)
    {
        if (period < 1)
            throw new ArgumentException($"period must be >= 1 (got {period}).", nameof(period));
        _step = new ForceIndexStep(period);
        Label = $"Force({period})";
    }

    /// <inheritdoc />
    protected override void Step(OhlcvBar input, int index, Span<double> outputs)
    {
        outputs[0] = _step.Next(input.Close, input.Volume);
    }
}
