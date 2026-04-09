// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Indicators;

/// <summary>Fibonacci Retracement indicator. Adds horizontal reference lines at key Fibonacci ratios between a high and low price.</summary>
/// <remarks>Draws lines at 23.6%, 38.2%, 50%, 61.8%, and 78.6% retracement levels.
/// These levels are widely used as support and resistance zones in technical analysis.</remarks>
public sealed class FibonacciRetracement : Indicator<SignalResult>
{
    private readonly double _low;
    private readonly double _high;

    /// <summary>The standard Fibonacci retracement ratios.</summary>
    public static readonly double[] Levels = [0.236, 0.382, 0.5, 0.618, 0.786];

    /// <summary>Creates a new Fibonacci Retracement indicator between the given price extremes.</summary>
    /// <param name="low">The swing low price.</param>
    /// <param name="high">The swing high price.</param>
    public FibonacciRetracement(double low, double high)
    {
        _low = low;
        _high = high;
        Label = "Fib";
    }

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        double range = _high - _low;
        var result = new double[Levels.Length];
        for (int i = 0; i < Levels.Length; i++)
            result[i] = _high - range * Levels[i];
        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        double range = _high - _low;
        var lineColor = Color ?? Colors.FibonacciOrange;

        foreach (double level in Levels)
        {
            double value = _high - range * level;
            var line = axes.AxHLine(value);
            line.Color = lineColor;
            line.LineStyle = LineStyle.Dashed;
            line.LineWidth = LineWidth;
            line.Label = $"{level:P1}";
        }
    }
}
