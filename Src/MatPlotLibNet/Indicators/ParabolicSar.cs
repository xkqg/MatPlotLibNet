// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Indicators;

/// <summary>Result of a <see cref="ParabolicSar"/> computation.</summary>
/// <param name="Sar">The SAR values at each bar.</param>
/// <param name="IsLong">True when the SAR is below price (long/bullish position).</param>
public sealed record ParabolicSarResult(double[] Sar, bool[] IsLong) : IIndicatorResult;

/// <summary>Parabolic SAR trend-following indicator. Plotted as dots above or below price bars.</summary>
/// <remarks>Dots below price indicate an uptrend (long). Dots above indicate a downtrend (short).
/// When price crosses the SAR, the trend reverses. Overlay this on the price axes.</remarks>
public sealed class ParabolicSar : Indicator<ParabolicSarResult>
{
    private readonly double[] _high, _low;
    private readonly double _step;
    private readonly double _max;

    public Color LongColor { get; set; } = Colors.Green;

    public Color ShortColor { get; set; } = Colors.Red;

    /// <summary>Creates a new Parabolic SAR indicator.</summary>
    /// <param name="high">High prices.</param>
    /// <param name="low">Low prices.</param>
    /// <param name="step">Acceleration factor step (default 0.02).</param>
    /// <param name="max">Maximum acceleration factor (default 0.2).</param>
    public ParabolicSar(double[] high, double[] low, double step = 0.02, double max = 0.2)
    {
        _high = high; _low = low;
        _step = step; _max = max;
        Label = "SAR";
    }

    /// <inheritdoc />
    public override ParabolicSarResult Compute()
    {
        int n = Math.Min(_high.Length, _low.Length);
        if (n < 2) return new ParabolicSarResult(Array.Empty<double>(), Array.Empty<bool>());

        var sar = new double[n];
        var isLong = new bool[n];

        // Seed: start long if first bar rises
        bool trending = _high[1] >= _high[0];
        double af = _step;
        double ep = trending ? _high[0] : _low[0];
        sar[0] = trending ? _low[0] : _high[0];
        isLong[0] = trending;

        for (int i = 1; i < n; i++)
        {
            double prevSar = sar[i - 1];

            if (trending)
            {
                // SAR is below price — update EP on new highs
                if (_high[i] > ep)
                {
                    ep = _high[i];
                    af = Math.Min(af + _step, _max);
                }
                double newSar = prevSar + af * (ep - prevSar);
                // SAR must not exceed the two prior lows
                if (i >= 2) newSar = Math.Min(newSar, Math.Min(_low[i - 1], _low[i - 2]));
                else newSar = Math.Min(newSar, _low[i - 1]);

                if (newSar >= _low[i])
                {
                    // Reversal to short
                    trending = false;
                    af = _step;
                    ep = _low[i];
                    sar[i] = ep;
                }
                else
                {
                    sar[i] = newSar;
                }
            }
            else
            {
                // SAR is above price — update EP on new lows
                if (_low[i] < ep)
                {
                    ep = _low[i];
                    af = Math.Min(af + _step, _max);
                }
                double newSar = prevSar + af * (ep - prevSar);
                // SAR must not exceed the two prior highs
                if (i >= 2) newSar = Math.Max(newSar, Math.Max(_high[i - 1], _high[i - 2]));
                else newSar = Math.Max(newSar, _high[i - 1]);

                if (newSar <= _high[i])
                {
                    // Reversal to long
                    trending = true;
                    af = _step;
                    ep = _high[i];
                    sar[i] = ep;
                }
                else
                {
                    sar[i] = newSar;
                }
            }
            isLong[i] = trending;
        }

        return new ParabolicSarResult(sar, isLong);
    }

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        var result = Compute();
        if (result.Sar.Length == 0) return;

        var x = ApplyOffset(VectorMath.Linspace(result.Sar.Length, 0.0));

        // Split into long and short positions
        var longX = new List<double>();
        var longY = new List<double>();
        var shortX = new List<double>();
        var shortY = new List<double>();

        for (int i = 0; i < result.Sar.Length; i++)
        {
            if (result.IsLong[i]) { longX.Add(x[i]); longY.Add(result.Sar[i]); }
            else { shortX.Add(x[i]); shortY.Add(result.Sar[i]); }
        }

        var longSeries = axes.Scatter(longX.ToArray(), longY.ToArray());
        longSeries.Color = LongColor;
        longSeries.Label = Label + " (Long)";
        longSeries.MarkerSize = 4;

        var shortSeries = axes.Scatter(shortX.ToArray(), shortY.ToArray());
        shortSeries.Color = ShortColor;
        shortSeries.Label = Label + " (Short)";
        shortSeries.MarkerSize = 4;
    }
}
