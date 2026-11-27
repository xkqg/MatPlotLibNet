// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming RoofingFilter: the Ehlers roofing filter: a high-pass filter followed by a super smoother, which keeps the cycles between the two periods. It is the step the batch <see cref="RoofingFilter"/> folds over its input, taken
/// one price (the close of a bar, when it is attached to a candle series) at a time, so after each one it says what the batch says over everything so far; a value the batch does not have yet
/// is NaN. O(1) per input.</summary>
public sealed class StreamingRoofingFilter : StreamingIndicator<double>
{
    private HighPassStep _highPass;
    private SuperSmootherStep _smoother;

    /// <inheritdoc />
    public override int WarmupPeriod => Spec.Warmup;

    /// <summary>Creates a streaming RoofingFilter.</summary>
    /// <param name="hpPeriod">The cut-off period of the high-pass filter; at least 4.</param>
    /// <param name="lpPeriod">The cut-off period of the super smoother; at least 2 and below the high-pass period.</param>
    /// <param name="capacity">How many points each output line keeps.</param>
    public StreamingRoofingFilter(int hpPeriod = RoofingFilter.DefaultHpPeriod, int lpPeriod = RoofingFilter.DefaultLpPeriod, int capacity = StreamingSeries.RestingCapacity)
        : base(RoofingFilter.Spec(hpPeriod, lpPeriod), capacity)
    {
        _highPass = new HighPassStep(hpPeriod);
        _smoother = new SuperSmootherStep(lpPeriod);
    }

    /// <inheritdoc />
    protected override void Step(double input, int index, Span<double> outputs)
    {
        double smoothed = _smoother.Next(_highPass.Next(input));
        outputs[0] = index < 2 ? double.NaN : smoothed;
    }
}
