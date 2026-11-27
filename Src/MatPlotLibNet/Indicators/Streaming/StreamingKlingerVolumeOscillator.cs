// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming KlingerVolumeOscillator: the Klinger volume oscillator and its signal. It is the step the batch <see cref="KlingerVolumeOscillator"/> folds over its input, taken
/// one bar at a time, so after each one it says what the batch says over everything so far; a value the batch does not have yet
/// is NaN. O(1) per input.</summary>
public sealed class StreamingKlingerVolumeOscillator : StreamingIndicator<OhlcvBar>
{
    private KlingerStep _step;

    /// <inheritdoc />
    public override int WarmupPeriod => Spec.Warmup;

    /// <summary>Creates a streaming KlingerVolumeOscillator.</summary>
    /// <param name="fastPeriod">The period of the fast average of the volume force; at least 2.</param>
    /// <param name="slowPeriod">The period of the slow average; above the fast one.</param>
    /// <param name="signalPeriod">The period of the signal average; at least 1.</param>
    /// <param name="capacity">How many points each output line keeps.</param>
    public StreamingKlingerVolumeOscillator(int fastPeriod = KlingerVolumeOscillator.DefaultFastPeriod, int slowPeriod = KlingerVolumeOscillator.DefaultSlowPeriod, int signalPeriod = KlingerVolumeOscillator.DefaultSignalPeriod, int capacity = StreamingSeries.RestingCapacity)
        : base(KlingerVolumeOscillator.Spec(fastPeriod, slowPeriod, signalPeriod), capacity)
    {
        _step = new KlingerStep(fastPeriod, slowPeriod, signalPeriod);
    }

    /// <inheritdoc />
    protected override void Step(OhlcvBar input, int index, Span<double> outputs)
    {
        var point = _step.Next(input.High, input.Low, input.Close, input.Volume);
        outputs[0] = point.Kvo;
        outputs[1] = point.Signal;
    }
}
