// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>Average True Range indicator. Measures market volatility by averaging the true range over N periods.</summary>
/// <remarks>True range = max(H-L, |H-prevC|, |L-prevC|). Best placed in a separate subplot below the price chart.</remarks>
public sealed class Atr : Indicator<SignalResult>
{
    private readonly double[] _high, _low, _close;
    private readonly int _period;

    /// <summary>Creates a new ATR indicator.</summary>
    public Atr(double[] high, double[] low, double[] close, int period = 14)
    {
        _high = high; _low = low; _close = close; _period = period;
        Label = $"ATR({period})";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int n = _close.Length;
        if (n <= _period) return Array.Empty<double>();
        var tr = new double[n];
        tr[0] = _high[0] - _low[0];
        for (int i = 1; i < n; i++)
            tr[i] = Math.Max(_high[i] - _low[i], Math.Max(Math.Abs(_high[i] - _close[i - 1]), Math.Abs(_low[i] - _close[i - 1])));

        var result = new double[n - _period];
        double avg = 0;
        for (int i = 0; i < _period; i++) avg += tr[i + 1]; // skip tr[0] for alignment with close
        avg /= _period;
        result[0] = avg;
        for (int i = 1; i < result.Length; i++)
        {
            avg = (avg * (_period - 1) + tr[_period + i]) / _period;
            result[i] = avg;
        }
        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        var atr = Compute();
        var x = new double[atr.Length];
        for (int i = 0; i < atr.Length; i++) x[i] = _period + i;
        x = ApplyOffset(x);
        var series = axes.Plot(x, atr);
        series.Label = Label;
        if (Color.HasValue) series.Color = Color.Value;
        series.LineWidth = LineWidth;
        series.LineStyle = LineStyle;
    }
}
