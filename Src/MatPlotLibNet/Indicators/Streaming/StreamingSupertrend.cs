// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming Supertrend: the Supertrend: the stop line, the direction of the trend (1 up, -1 down) and 1 where it turned. It is the <see cref="SupertrendStep"/> the batch <see cref="Supertrend"/> folds over its input, taken
/// one bar at a time, so after each one it says what the batch says over everything so far; a value the batch does not have yet
/// is NaN. O(1) per input.</summary>
public sealed class StreamingSupertrend : StreamingIndicator<OhlcvBar>
{
    private SupertrendStep _step;
    private readonly int _period;

    /// <inheritdoc />
    public override int WarmupPeriod => _period + 1;

    /// <summary>Creates a streaming Supertrend.</summary>
    /// <param name="period">The period of the average true range; at least 1.</param>
    /// <param name="multiplier">How many average true ranges the bands lie from the midpoint; above 0.</param>
    /// <param name="capacity">How many points each output line keeps.</param>
    public StreamingSupertrend(int period = 10, double multiplier = 3.0, int capacity = 10_000)
        : base([
            new StreamingLineSeries(capacity) { Label = $"ST({period},{multiplier:0.#})" },
            new StreamingLineSeries(capacity) { Label = $"ST({period},{multiplier:0.#}) Direction" },
            new StreamingLineSeries(capacity) { Label = $"ST({period},{multiplier:0.#}) Flip" },
        ])
    {
        if (period < 1)
            throw new ArgumentException($"period must be >= 1 (got {period}).", nameof(period));
        if (multiplier <= 0)
            throw new ArgumentException($"multiplier must be > 0 (got {multiplier}).", nameof(multiplier));
        _step = new SupertrendStep(period, multiplier);
        _period = period;
    }

    /// <inheritdoc />
    protected override void Step(OhlcvBar input, int index, Span<double> outputs)
    {
        var point = _step.Next(input.High, input.Low, input.Close);
        if (point.Direction == 0) return;

        outputs[0] = point.Line;
        outputs[1] = point.Direction;
        outputs[2] = point.Flipped ? 1 : 0;
    }
}
