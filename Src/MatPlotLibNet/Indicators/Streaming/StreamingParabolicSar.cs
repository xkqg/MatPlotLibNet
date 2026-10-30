// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming ParabolicSar: Wilder's parabolic stop and reverse: the stop level, and 1 where the trend is up and 0 where it is down. It is the <see cref="ParabolicSarStep"/> the batch <see cref="ParabolicSar"/> folds over its input, taken
/// one bar at a time, so after each one it says what the batch says over everything so far; a value the batch does not have yet
/// is NaN. O(1) per input.</summary>
public sealed class StreamingParabolicSar : StreamingIndicator<OhlcvBar>
{
    private ParabolicSarStep _step;

    /// <inheritdoc />
    public override int WarmupPeriod => 2;

    /// <summary>Creates a streaming ParabolicSar.</summary>
    /// <param name="step">The acceleration factor at the start and the amount it grows by.</param>
    /// <param name="max">The largest acceleration factor.</param>
    /// <param name="capacity">How many points each output line keeps.</param>
    public StreamingParabolicSar(double step = 0.02, double max = 0.2, int capacity = 10_000)
        : base([
            new StreamingLineSeries(capacity) { Label = "SAR" },
            new StreamingLineSeries(capacity) { Label = "SAR Long" },
        ])
    {
        _step = new ParabolicSarStep(step, max);
    }

    /// <inheritdoc />
    protected override void Step(OhlcvBar input, int index, Span<double> outputs)
    {
        var point = _step.Next(input.High, input.Low);
        if (index == 0) return;

        outputs[0] = point.Sar;
        outputs[1] = point.IsLong ? 1 : 0;
    }
}
