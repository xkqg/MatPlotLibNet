// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>Average True Range indicator. Measures market volatility by averaging the true range over N periods.</summary>
/// <remarks>True range = max(H-L, |H-prevC|, |L-prevC|). Best placed in a separate subplot below the price chart.</remarks>
public sealed class Atr : CandleIndicator<SignalResult>
{
    private readonly int _period;

    /// <summary>Creates a new ATR indicator.</summary>
    public Atr(double[] high, double[] low, double[] close, int period = 14)
        : base(high, low, close)
    {
        _period = period;
        Label = $"ATR({period})";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int n = BarCount;
        if (n <= _period) return Array.Empty<double>();
        var tr = ComputeTrueRange();

        var result = new double[n - _period];
        double avg = 0;
        for (int i = 0; i < _period; i++) avg += tr[i]; // tr is already offset by 1 (length = n-1)
        avg /= _period;
        result[0] = avg;
        for (int i = 1; i < result.Length; i++)
        {
            avg = (avg * (_period - 1) + tr[_period - 1 + i]) / _period;
            result[i] = avg;
        }
        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), _period);
}
