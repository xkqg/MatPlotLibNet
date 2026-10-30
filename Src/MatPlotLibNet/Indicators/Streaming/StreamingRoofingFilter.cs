// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming RoofingFilter: the Ehlers roofing filter: a high-pass filter followed by a super smoother, which keeps the cycles between the two periods. It is the <see cref="HighPassStep"/> the batch <see cref="RoofingFilter"/> folds over its input, taken
/// one price (the close of a bar, when it is attached to a candle series) at a time, so after each one it says what the batch says over everything so far; a value the batch does not have yet
/// is NaN. O(1) per input.</summary>
public sealed class StreamingRoofingFilter : StreamingIndicator<double>
{
    private HighPassStep _highPass;
    private SuperSmootherStep _smoother;

    /// <inheritdoc />
    public override int WarmupPeriod => 3;

    /// <summary>Creates a streaming RoofingFilter.</summary>
    /// <param name="hpPeriod">The cut-off period of the high-pass filter; at least 4.</param>
    /// <param name="lpPeriod">The cut-off period of the super smoother; at least 2 and below the high-pass period.</param>
    /// <param name="capacity">How many points each output line keeps.</param>
    public StreamingRoofingFilter(int hpPeriod = 48, int lpPeriod = 10, int capacity = 10_000)
        : base(capacity)
    {
        if (hpPeriod < 4)
            throw new ArgumentException($"hpPeriod must be >= 4 (got {hpPeriod}).", nameof(hpPeriod));
        if (lpPeriod < 2)
            throw new ArgumentException($"lpPeriod must be >= 2 (got {lpPeriod}).", nameof(lpPeriod));
        if (lpPeriod >= hpPeriod)
            throw new ArgumentException($"lpPeriod must be < hpPeriod (got {lpPeriod} vs {hpPeriod}).", nameof(lpPeriod));
        _highPass = new HighPassStep(hpPeriod);
        _smoother = new SuperSmootherStep(lpPeriod);
        Label = $"Roof({hpPeriod}/{lpPeriod})";
    }

    /// <inheritdoc />
    protected override void Step(double input, int index, Span<double> outputs)
    {
        double smoothed = _smoother.Next(_highPass.Next(input));
        outputs[0] = index < 2 ? double.NaN : smoothed;
    }
}
