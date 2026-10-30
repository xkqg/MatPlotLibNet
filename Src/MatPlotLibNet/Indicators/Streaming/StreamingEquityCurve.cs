// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming EquityCurve: the equity curve of a series of trades; each input is the profit or loss of a trade and the output is the equity after it (the starting capital is not a point of its own). It is the <see cref="EquityCurveStep"/> the batch <see cref="EquityCurve"/> folds over its input, taken
/// one price (the close of a bar, when it is attached to a candle series) at a time, so after each one it says what the batch says over everything so far; a value the batch does not have yet
/// is NaN. O(1) per input.</summary>
public sealed class StreamingEquityCurve : StreamingIndicator<double>
{
    private EquityCurveStep _step;

    /// <inheritdoc />
    public override int WarmupPeriod => 1;

    /// <summary>Creates a streaming EquityCurve.</summary>
    /// <param name="startingCapital">The equity before the first trade.</param>
    /// <param name="capacity">How many points each output line keeps.</param>
    public StreamingEquityCurve(double startingCapital = 10000, int capacity = 10_000)
        : base(capacity)
    {
        _step = new EquityCurveStep(startingCapital);
        Label = "Equity";
    }

    /// <inheritdoc />
    protected override void Step(double input, int index, Span<double> outputs)
    {
        outputs[0] = _step.Next(input);
    }
}
