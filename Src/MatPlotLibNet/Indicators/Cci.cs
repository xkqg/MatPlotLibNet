// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>Commodity Channel Index indicator. Measures how far price has deviated from its statistical mean.</summary>
/// <remarks>Values above +100 indicate overbought conditions; below -100 indicate oversold.
/// Reference lines at ±100 are added automatically. Best placed in a separate subplot.</remarks>
public sealed class Cci : CandleIndicator<SignalResult>
{
    private readonly int _period;

    /// <summary>Creates a new CCI indicator.</summary>
    /// <param name="high">High prices.</param>
    /// <param name="low">Low prices.</param>
    /// <param name="close">Close prices.</param>
    /// <param name="period">Lookback period (default 20).</param>
    public Cci(double[] high, double[] low, double[] close, int period = 20)
        : base(high, low, close)
    {
        _period = period;
        Label = $"CCI({period})";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int n = BarCount;
        int len = n - _period + 1;
        if (len <= 0) return Array.Empty<double>();

        var tp = ComputeTypicalPrice();

        double[] sma = VectorMath.RollingMean(tp, _period);  // length = len

        var result = new double[len];
        for (int i = 0; i < len; i++)
        {
            // Mean deviation for the window
            double mean = sma[i];
            double meanDev = 0;
            for (int j = 0; j < _period; j++)
                meanDev += Math.Abs(tp[i + j] - mean);
            meanDev /= _period;

            result[i] = meanDev == 0 ? 0 : (tp[i + _period - 1] - mean) / (0.015 * meanDev);
        }
        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        double[] cci = Compute();
        if (cci.Length == 0) return;
        PlotSignal(axes, cci, _period - 1);
        axes.AxHLine(100);
        axes.AxHLine(-100);
    }
}
