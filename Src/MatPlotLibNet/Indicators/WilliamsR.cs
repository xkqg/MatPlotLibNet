// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>Williams %R momentum indicator. Measures overbought/oversold conditions on a -100 to 0 scale.</summary>
/// <remarks>Values near 0 indicate overbought conditions; values near -100 indicate oversold.
/// Reference lines at -20 (overbought) and -80 (oversold) are added automatically.
/// Best placed in a separate subplot.</remarks>
public sealed class WilliamsR : CandleIndicator<SignalResult>
{
    private readonly int _period;

    /// <summary>Creates a new Williams %R indicator.</summary>
    /// <param name="high">High prices.</param>
    /// <param name="low">Low prices.</param>
    /// <param name="close">Close prices.</param>
    /// <param name="period">Lookback period (default 14).</param>
    public WilliamsR(double[] high, double[] low, double[] close, int period = 14)
        : base(high, low, close)
    {
        _period = period;
        Label = $"%R({period})";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int n = BarCount;
        int len = n - _period + 1;
        if (len <= 0) return Array.Empty<double>();

        var highBuf = new double[n];
        var lowBuf = new double[n];
        VectorMath.RollingMax(High, _period, highBuf);
        VectorMath.RollingMin(Low, _period, lowBuf);

        var result = new double[len];
        for (int i = 0; i < len; i++)
        {
            int idx = i + _period - 1;
            double hh = highBuf[idx];
            double ll = lowBuf[idx];
            double range = hh - ll;
            result[i] = range == 0 ? -50.0 : (hh - Close[idx]) / range * -100.0;
        }
        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        double[] wr = Compute();
        if (wr.Length == 0) return;
        PlotSignal(axes, wr, _period - 1);
        axes.YAxis.Min = -100;
        axes.YAxis.Max = 0;
        axes.AxHLine(-20);
        axes.AxHLine(-80);
    }
}
