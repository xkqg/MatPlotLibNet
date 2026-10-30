// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming Vpin: the volume-synchronised probability of informed trading over a bucket of bars. It keeps the inputs the value depends on (<see cref="WindowedStreamingIndicator{TInput}.Width"/>)
/// and lets the batch <see cref="Vpin"/> say its value over them, so after each bar, with its volume where the indicator reads it, it says what the batch says over everything
/// so far; a value the batch does not have yet is NaN.</summary>
public sealed class StreamingVpin : WindowedStreamingIndicator<OhlcvBar>
{
    private readonly int _bucketPeriod;
    private readonly int _sigmaPeriod;

    /// <summary>Creates a streaming Vpin.</summary>
    /// <param name="bucketPeriod">How many bars make a volume bucket; above 0.</param>
    /// <param name="sigmaPeriod">How many returns the volatility of the price change is taken over; above 0.</param>
    /// <param name="capacity">How many points each output line keeps.</param>
    public StreamingVpin(int bucketPeriod = 50, int sigmaPeriod = 50, int capacity = 10_000)
        : base(bucketPeriod + sigmaPeriod, capacity)
    {
        _bucketPeriod = bucketPeriod;
        _sigmaPeriod = sigmaPeriod;
        Label = $"VPIN({bucketPeriod})";
    }

    /// <inheritdoc />
    /// <remarks>The first value comes one bar after the longer of the two periods; it is the full one once both periods have come.</remarks>
    public override int WarmupPeriod => Math.Max(_bucketPeriod, _sigmaPeriod) + 2;

    /// <inheritdoc />
    protected override void Recompute(ReadOnlySpan<OhlcvBar> window, Span<double> outputs)
    {
        outputs[0] = new Vpin(OhlcvBars.Cv(window.Closes(), window.Volumes()), _bucketPeriod, _sigmaPeriod).Compute().Values.LastOrNaN();
    }
}
