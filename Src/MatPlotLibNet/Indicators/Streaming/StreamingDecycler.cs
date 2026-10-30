// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming Decycler: the Ehlers decycler: the price less its high-pass filtered part, which is the trend without the cycles. It is the <see cref="HighPassStep"/> the batch <see cref="Decycler"/> folds over its input, taken
/// one price (the close of a bar, when it is attached to a candle series) at a time, so after each one it says what the batch says over everything so far; a value the batch does not have yet
/// is NaN. O(1) per input.</summary>
public sealed class StreamingDecycler : StreamingIndicator<double>
{
    private HighPassStep _highPass;

    /// <inheritdoc />
    public override int WarmupPeriod => 3;

    /// <summary>Creates a streaming Decycler.</summary>
    /// <param name="hpPeriod">The cut-off period of the high-pass filter that is taken off the price; at least 4.</param>
    /// <param name="capacity">How many points each output line keeps.</param>
    public StreamingDecycler(int hpPeriod = 60, int capacity = 10_000)
        : base(capacity)
    {
        if (hpPeriod < 4)
            throw new ArgumentException($"hpPeriod must be >= 4 (got {hpPeriod}).", nameof(hpPeriod));
        _highPass = new HighPassStep(hpPeriod);
        Label = $"Decycler({hpPeriod})";
    }

    /// <inheritdoc />
    protected override void Step(double input, int index, Span<double> outputs)
    {
        double highPassed = _highPass.Next(input);
        outputs[0] = index < 2 ? double.NaN : input - highPassed;
    }
}
