// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>Average True Range indicator. Measures market volatility by averaging the true range over N periods.</summary>
/// <remarks>True range = max(H-L, |H-prevC|, |L-prevC|). Best placed in a separate subplot below the price chart.</remarks>
public sealed class Atr : Indicator<double[]>
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
    public override double[] Compute() => Compute(_high, _low, _close, _period);

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

    /// <summary>Computes ATR using Wilder's smoothing method.</summary>
    public static double[] Compute(double[] high, double[] low, double[] close, int period)
    {
        int n = close.Length;
        if (n <= period) return [];
        var tr = new double[n];
        tr[0] = high[0] - low[0];
        for (int i = 1; i < n; i++)
            tr[i] = Math.Max(high[i] - low[i], Math.Max(Math.Abs(high[i] - close[i - 1]), Math.Abs(low[i] - close[i - 1])));

        var result = new double[n - period];
        double avg = 0;
        for (int i = 0; i < period; i++) avg += tr[i + 1]; // skip tr[0] for alignment with close
        avg /= period;
        result[0] = avg;
        for (int i = 1; i < result.Length; i++)
        {
            avg = (avg * (period - 1) + tr[period + i]) / period;
            result[i] = avg;
        }
        return result;
    }
}
