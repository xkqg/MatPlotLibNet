// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming FractionalDifferentiation: the fixed-width fractional differentiation of the price over as many prices as the weights reach back. It keeps the inputs the value depends on (<see cref="WindowedStreamingIndicator{TInput}.Width"/>)
/// and lets the batch <see cref="FractionalDifferentiation"/> say its value over them, so after each price (the close of a bar, when it is attached to a candle series) it says what the batch says over everything
/// so far; a value the batch does not have yet is NaN.</summary>
public sealed class StreamingFractionalDifferentiation : WindowedStreamingIndicator<double>
{
    private readonly double _d;
    private readonly double _tolerance;

    /// <summary>Creates a streaming FractionalDifferentiation.</summary>
    /// <param name="d">The order of differentiation, in (0, 1).</param>
    /// <param name="tolerance">The weight below which the series of weights is cut.</param>
    /// <param name="capacity">How many points each output line keeps.</param>
    public StreamingFractionalDifferentiation(double d = 0.4, double tolerance = 1e-3, int capacity = 10_000)
        : base(FractionalDifferentiation.ComputeWeights(d, tolerance).Length, capacity)
    {
        _d = d;
        _tolerance = tolerance;
        Label = $"FFD(d={d.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)})";
    }

    /// <inheritdoc />
    protected override void Recompute(ReadOnlySpan<double> window, Span<double> outputs)
    {
        outputs[0] = new FractionalDifferentiation(window.ToArray(), _d, _tolerance).Compute().Values.LastOrNaN();
    }
}
