// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models.Series.Streaming;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>Streaming KeltnerChannels: the Keltner channel: an exponential average of the close with a band of average true ranges on each side. It is the <see cref="EmaStep"/> the batch <see cref="KeltnerChannels"/> folds over its input, taken
/// one bar at a time, so after each one it says what the batch says over everything so far; a value the batch does not have yet
/// is NaN. O(1) per input.</summary>
public sealed class StreamingKeltnerChannels : StreamingIndicator<OhlcvBar>
{
    private EmaStep _ema;
    private AtrStep _atr;
    private readonly double _atrMultiplier;
    private readonly int _period;

    /// <inheritdoc />
    public override int WarmupPeriod => _period + 1;

    /// <summary>Creates a streaming KeltnerChannels.</summary>
    /// <param name="period">The period of the middle line and of the average true range.</param>
    /// <param name="atrMultiplier">How many average true ranges the channel is wide on each side.</param>
    /// <param name="capacity">How many points each output line keeps.</param>
    public StreamingKeltnerChannels(int period = 20, double atrMultiplier = 1.5, int capacity = 10_000)
        : base([
            new StreamingLineSeries(capacity) { Label = $"KC({period},{atrMultiplier})" },
            new StreamingLineSeries(capacity) { Label = $"KC({period},{atrMultiplier}) Upper" },
            new StreamingLineSeries(capacity) { Label = $"KC({period},{atrMultiplier}) Lower" },
        ])
    {
        _ema = new EmaStep(period);
        _atr = new AtrStep(period);
        _atrMultiplier = atrMultiplier;
        _period = period;
    }

    /// <inheritdoc />
    protected override void Step(OhlcvBar input, int index, Span<double> outputs)
    {
        double middle = _ema.Next(input.Close);
        double atr = _atr.Next(input.High, input.Low, input.Close);
        if (double.IsNaN(atr)) return;

        double scaled = atr * _atrMultiplier;
        outputs[0] = middle;
        outputs[1] = middle + scaled;
        outputs[2] = middle - scaled;
    }
}
