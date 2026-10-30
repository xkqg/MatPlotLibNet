// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming KlingerVolumeOscillator: the Klinger volume oscillator and its signal. It is the <see cref="KlingerStep"/> the batch <see cref="KlingerVolumeOscillator"/> folds over its input, taken
/// one bar at a time, so after each one it says what the batch says over everything so far; a value the batch does not have yet
/// is NaN. O(1) per input.</summary>
public sealed class StreamingKlingerVolumeOscillator : StreamingIndicator<OhlcvBar>
{
    private KlingerStep _step;

    /// <inheritdoc />
    public override int WarmupPeriod => 2;

    /// <summary>Creates a streaming KlingerVolumeOscillator.</summary>
    /// <param name="fastPeriod">The period of the fast average of the volume force; at least 2.</param>
    /// <param name="slowPeriod">The period of the slow average; above the fast one.</param>
    /// <param name="signalPeriod">The period of the signal average; at least 1.</param>
    /// <param name="capacity">How many points each output line keeps.</param>
    public StreamingKlingerVolumeOscillator(int fastPeriod = 34, int slowPeriod = 55, int signalPeriod = 13, int capacity = 10_000)
        : base([
            new StreamingLineSeries(capacity) { Label = $"KVO({fastPeriod}/{slowPeriod}/{signalPeriod})" },
            new StreamingLineSeries(capacity) { Label = $"KVO({fastPeriod}/{slowPeriod}/{signalPeriod}) Signal" },
        ])
    {
        if (fastPeriod < 2)
            throw new ArgumentException($"fastPeriod must be >= 2 (got {fastPeriod}).", nameof(fastPeriod));
        if (slowPeriod <= fastPeriod)
            throw new ArgumentException($"slowPeriod must be > fastPeriod (got slow={slowPeriod}, fast={fastPeriod}).", nameof(slowPeriod));
        if (signalPeriod < 1)
            throw new ArgumentException($"signalPeriod must be >= 1 (got {signalPeriod}).", nameof(signalPeriod));
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
