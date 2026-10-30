// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming Cusum: the CUSUM change detector: the signal (1 up, -1 down, 0 none) and the two cumulative sums of the log returns. It is the <see cref="CusumStep"/> the batch <see cref="Cusum"/> folds over its input, taken
/// one price (the close of a bar, when it is attached to a candle series) at a time, so after each one it says what the batch says over everything so far; a value the batch does not have yet
/// is NaN. O(1) per input.</summary>
public sealed class StreamingCusum : StreamingIndicator<double>
{
    private CusumStep _step;

    /// <inheritdoc />
    public override int WarmupPeriod => 2;

    /// <summary>Creates a streaming Cusum.</summary>
    /// <param name="threshold">The level a cumulative sum must cross to signal; above 0.</param>
    /// <param name="drift">The allowance taken off each log return.</param>
    /// <param name="capacity">How many points each output line keeps.</param>
    public StreamingCusum(double threshold, double drift = 0.0, int capacity = 10_000)
        : base([
            new StreamingLineSeries(capacity) { Label = $"CUSUM(h={threshold.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)})" },
            new StreamingLineSeries(capacity) { Label = $"CUSUM(h={threshold.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}) S+" },
            new StreamingLineSeries(capacity) { Label = $"CUSUM(h={threshold.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}) S-" },
        ])
    {
        if (threshold <= 0)
            throw new ArgumentException($"CUSUM threshold must be > 0 (got {threshold}).", nameof(threshold));
        _step = new CusumStep(threshold, drift);
    }

    /// <inheritdoc />
    protected override void Step(double input, int index, Span<double> outputs)
    {
        if (input <= 0)
            throw new ArgumentException($"CUSUM requires strictly positive prices; bar {index} has {input}.", "prices");

        var point = _step.Next(input);
        outputs[0] = point.Signal;
        outputs[1] = point.SPos;
        outputs[2] = point.SNeg;
    }
}
