// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming LaguerreRsi: the Laguerre RSI. It is the <see cref="LaguerreRsiStep"/> the batch <see cref="LaguerreRsi"/> folds over its input, taken
/// one price (the close of a bar, when it is attached to a candle series) at a time, so after each one it says what the batch says over everything so far; a value the batch does not have yet
/// is NaN. O(1) per input.</summary>
public sealed class StreamingLaguerreRsi : StreamingIndicator<double>
{
    private LaguerreRsiStep _step;

    /// <inheritdoc />
    public override int WarmupPeriod => 2;

    /// <summary>Creates a streaming LaguerreRsi.</summary>
    /// <param name="alpha">The smoothing factor, in (0, 1).</param>
    /// <param name="capacity">How many points each output line keeps.</param>
    public StreamingLaguerreRsi(double alpha = 0.2, int capacity = 10_000)
        : base(capacity)
    {
        if (alpha <= 0 || alpha >= 1)
            throw new ArgumentException($"alpha must be in (0, 1) (got {alpha}).", nameof(alpha));
        _step = new LaguerreRsiStep(alpha);
        Label = $"LagRSI({alpha.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)})";
    }

    /// <inheritdoc />
    protected override void Step(double input, int index, Span<double> outputs)
    {
        outputs[0] = _step.Next(input);
    }
}
