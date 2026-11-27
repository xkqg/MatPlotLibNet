// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming Stochastic Oscillator (%K and %D). It is the step the batch <see cref="Stochastic"/> folds over its
/// bars, taken one candle at a time, so after each candle it says what the batch says over everything so far; the highest high and the
/// lowest low of the last <c>k</c> candles come at O(1) per candle.</summary>
public sealed class StreamingStochastic : StreamingIndicator<OhlcvBar>
{
    private StochasticStep _step;

    /// <inheritdoc />
    public override int WarmupPeriod => Spec.Warmup;

    /// <inheritdoc />
    internal override PaneRange? PinnedRange => Stochastic.Pane;

    /// <summary>Creates streaming Stochastic: the slow stochastic, %K averaged over 3 bars, as the batch <see cref="Stochastic"/> is
    /// unless told otherwise. Two output series: %K and %D.</summary>
    /// <param name="kPeriod">The number of bars the lowest low and the highest high are taken over.</param>
    /// <param name="dPeriod">The number of %K values %D is the mean of.</param>
    /// <param name="capacity">The number of points each output series keeps.</param>
    public StreamingStochastic(int kPeriod = Stochastic.DefaultKPeriod, int dPeriod = Stochastic.DefaultDPeriod, int capacity = StreamingSeries.RestingCapacity)
        : this(kPeriod, dPeriod, Stochastic.DefaultSmoothK, capacity)
    {
    }

    /// <summary>Creates streaming Stochastic with the smoothing of %K named. Two output series: %K and %D.</summary>
    /// <param name="kPeriod">The %K lookback period.</param>
    /// <param name="dPeriod">The %D smoothing period.</param>
    /// <param name="smoothK">How many raw %K values the %K line is the mean of; 1 draws the raw (fast) %K.</param>
    /// <param name="capacity">How many points each output line keeps.</param>
    public StreamingStochastic(int kPeriod, int dPeriod, int smoothK, int capacity)
        : base(Stochastic.Spec(kPeriod, dPeriod, smoothK), capacity)
    {
        _step = new StochasticStep(kPeriod, smoothK, dPeriod);
    }

    /// <inheritdoc />
    protected override void Step(OhlcvBar input, int index, Span<double> outputs)
    {
        var point = _step.Next(input.High, input.Low, input.Close);
        outputs[0] = point.K;
        outputs[1] = point.D;
    }
}
