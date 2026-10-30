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

    /// <summary>Creates a new OBV indicator.</summary>
    /// <param name="close">Close prices.</param>
    /// <param name="volume">Volume data.</param>
    public Obv(double[] close, double[] volume)
    {
        _close = close;
        _volume = volume;
        Label = "OBV";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int n = Math.Min(_close.Length, _volume.Length);
        if (n == 0) return Array.Empty<double>();

        var result = new double[n];
        var step = new ObvStep();
        for (int i = 0; i < n; i++)
            result[i] = step.Next(_close[i], _volume[i]);
        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), 0);
}
