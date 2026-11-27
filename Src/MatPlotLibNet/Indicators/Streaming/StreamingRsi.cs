// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming Relative Strength Index (Wilder's smoothing): the step the batch <see cref="Rsi"/> folds over
/// its prices, taken one price at a time. O(1) per append.</summary>
public sealed class StreamingRsi : StreamingIndicator<double>
{
    private RsiStep _step;

    /// <inheritdoc />
    public override int WarmupPeriod => Spec.Warmup;

    /// <inheritdoc />
    internal override PaneRange? PinnedRange => Rsi.Pane;

    /// <summary>Creates a streaming RSI indicator.</summary>
    public StreamingRsi(int period = Rsi.DefaultPeriod, int capacity = StreamingSeries.RestingCapacity) : base(Rsi.Spec(period), capacity)
    {
        _step = new RsiStep(period);
    }

    /// <inheritdoc />
    protected override void Step(double input, int index, Span<double> outputs) => outputs[0] = _step.Next(input);
}
