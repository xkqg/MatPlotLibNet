// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming EhlersSineWave: the Ehlers sine wave, its 45 degree lead, and 1 where the market is in a cycle and 0 where it trends. It is the <see cref="EhlersSineWaveStep"/> the batch <see cref="EhlersSineWave"/> folds over its input, taken
/// one price (the close of a bar, when it is attached to a candle series) at a time, so after each one it says what the batch says over everything so far; a value the batch does not have yet
/// is NaN. O(1) per input.</summary>
public sealed class StreamingEhlersSineWave : StreamingIndicator<double>
{
    private EhlersSineWaveStep _step;

    /// <inheritdoc />
    public override int WarmupPeriod => 7;

    /// <summary>Creates a streaming EhlersSineWave.</summary>
    /// <param name="capacity">How many points each output line keeps.</param>
    public StreamingEhlersSineWave(int capacity = 10_000)
        : base([
            new StreamingLineSeries(capacity) { Label = "SineWave" },
            new StreamingLineSeries(capacity) { Label = "LeadSine" },
            new StreamingLineSeries(capacity) { Label = "Cyclic" },
        ])
    {
        _step = new EhlersSineWaveStep();
    }

    /// <inheritdoc />
    protected override void Step(double input, int index, Span<double> outputs)
    {
        var point = _step.Next(input);
        outputs[0] = point.SineWave;
        outputs[1] = point.LeadSine;
        outputs[2] = index < 6 ? double.NaN : (point.IsCyclic ? 1 : 0);
    }
}
