// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming MamaFama: the MESA adaptive moving average and the following adaptive moving average. It is the <see cref="MamaFamaStep"/> the batch <see cref="MamaFama"/> folds over its input, taken
/// one price (the close of a bar, when it is attached to a candle series) at a time, so after each one it says what the batch says over everything so far; a value the batch does not have yet
/// is NaN. O(1) per input.</summary>
public sealed class StreamingMamaFama : StreamingIndicator<double>
{
    private MamaFamaStep _step;

    /// <inheritdoc />
    public override int WarmupPeriod => 7;

    /// <summary>Creates a streaming MamaFama.</summary>
    /// <param name="fastLimit">The fastest rate of the average, in (0, 1].</param>
    /// <param name="slowLimit">The slowest rate of the average, above 0 and below the fast limit.</param>
    /// <param name="capacity">How many points each output line keeps.</param>
    public StreamingMamaFama(double fastLimit = 0.5, double slowLimit = 0.05, int capacity = 10_000)
        : base([
            new StreamingLineSeries(capacity) { Label = $"MAMA({fastLimit.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}/{slowLimit.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)})" },
            new StreamingLineSeries(capacity) { Label = "FAMA" },
        ])
    {
        if (fastLimit <= 0 || fastLimit > 1)
            throw new ArgumentException($"fastLimit must be in (0, 1] (got {fastLimit}).", nameof(fastLimit));
        if (slowLimit <= 0 || slowLimit >= fastLimit)
            throw new ArgumentException($"slowLimit must be in (0, fastLimit={fastLimit}) (got {slowLimit}).", nameof(slowLimit));
        _step = new MamaFamaStep(fastLimit, slowLimit);
    }

    /// <inheritdoc />
    protected override void Step(double input, int index, Span<double> outputs)
    {
        var point = _step.Next(input);
        outputs[0] = point.Mama;
        outputs[1] = point.Fama;
    }
}
