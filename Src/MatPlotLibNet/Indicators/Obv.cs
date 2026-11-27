// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>On-Balance Volume indicator. Accumulates volume based on price direction to confirm trends.</summary>
/// <remarks>Rising OBV confirms an uptrend; falling OBV confirms a downtrend.
/// When price and OBV diverge, a trend reversal may be imminent.</remarks>
public sealed class Obv : Indicator<SignalResult>
{
    private readonly double[] _close;
    private readonly double[] _volume;

    /// <inheritdoc />
    private protected override int BarsIn => Math.Min(_close.Length, _volume.Length);

    /// <summary>Creates a new OBV indicator.</summary>
    /// <param name="close">Close prices.</param>
    /// <param name="volume">Volume data.</param>
    public Obv(double[] close, double[] volume) : base(Spec())
    {
        _close = close;
        _volume = volume;
    }

    internal static IndicatorSpec Spec() => IndicatorSpec.Of(0, "OBV");

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int n = ValuedBars;
        var result = new double[n];
        var step = new ObvStep();
        for (int i = 0; i < n; i++)
            result[i] = step.Next(_close[i], _volume[i]);
        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotLine(axes, Compute());
}
