// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
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

    /// <summary>The colour of the dots drawn below the price, while the trend is up. Green by default.</summary>
    public Color LongColor { get; set; } = Colors.Green;

    /// <summary>The colour of the dots drawn above the price, while the trend is down. Red by default.</summary>
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
        var step = new ParabolicSarStep(_step, _max);
        for (int i = 0; i < n; i++)
        {
            var point = step.Next(_high[i], _low[i]);
            sar[i] = point.Sar;
            isLong[i] = point.IsLong;
        }

        // Bar 0 is given its value once the second bar has been read: the seed of the trend.
        sar[0] = step.First.Sar;
        isLong[0] = step.First.IsLong;
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
