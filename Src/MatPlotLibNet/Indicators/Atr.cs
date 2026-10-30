// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>Average True Range indicator. Measures market volatility by averaging the true range over N periods.</summary>
/// <remarks>True range = max(H-L, |H-prevC|, |L-prevC|). Best placed in a separate subplot below the price chart.</remarks>
public sealed class Atr : CandleIndicator<SignalResult>
{
    private readonly int _period;

    /// <summary>Creates a new ATR indicator.</summary>
    /// <param name="bars">High, Low and Close; built via <see cref="OhlcvBars.Hlc"/>.</param>
    /// <param name="period">Averaging period (default 14).</param>
    public Atr(OhlcvBars bars, int period = 14)
        : base(bars)
    {
        _period = period;
        Label = $"ATR({period})";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int n = BarCount;
        if (n <= _period) return Array.Empty<double>();
        var result = new double[n - _period];
        var step = new AtrStep(_period);
        for (int i = 0; i < n; i++)
        {
            double value = step.Next(High[i], Low[i], Close[i]);
            if (i >= _period) result[i - _period] = value;
        }
        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotSignal(axes, Compute(), _period);
}
