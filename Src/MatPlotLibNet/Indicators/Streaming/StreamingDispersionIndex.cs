// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming dispersion index: the spread of several signals at one bar, the population standard deviation of one row. A
/// row holds one value of each signal (at least two), and every row is as long as the first; the form is given a row, not a bar
/// (<see cref="StreamingIndicator{TInput}.Append"/> takes a <c>double[]</c>), so it is not attached to a candle series. It is the
/// arithmetic of the batch <see cref="DispersionIndex"/> (<c>DispersionIndex.Of</c>) taken one row at a time. O(signals) per row.</summary>
public sealed class StreamingDispersionIndex : StreamingIndicator<double[]>
{
    private int _signalCount;

    /// <inheritdoc />
    public override int WarmupPeriod => 1;

    /// <summary>Creates a streaming dispersion index.</summary>
    /// <param name="capacity">How many points the line keeps.</param>
    public StreamingDispersionIndex(int capacity = 10_000) : base(capacity)
    {
        Label = "Dispersion";
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">The row has fewer than two signals, or a different number than the rows before it.</exception>
    protected override void Step(double[] input, int index, Span<double> outputs)
    {
        if (input.Length < 2)
            throw new ArgumentException($"DispersionIndex requires at least 2 signals per row (got {input.Length}).", nameof(input));
        if (_signalCount != 0 && input.Length != _signalCount)
            throw new ArgumentException(
                $"All feature rows must have the same length; row 0 has {_signalCount}, row {index} has {input.Length}.", nameof(input));

        _signalCount = input.Length;
        outputs[0] = DispersionIndex.Of(input);
    }
}
