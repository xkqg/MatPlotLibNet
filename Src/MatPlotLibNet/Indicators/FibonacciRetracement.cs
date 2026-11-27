// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Indicators;

/// <summary>Fibonacci Retracement indicator. Adds horizontal reference lines at key Fibonacci ratios between a high and low price.</summary>
/// <remarks>Draws lines at 23.6%, 38.2%, 50%, 61.8%, and 78.6% retracement levels.
/// These levels are widely used as support and resistance zones in technical analysis. The lines are dashed; set
/// <see cref="Indicator.LineStyle"/> for another pattern. They are orange under the library's own themes and in <c>lines.color</c>
/// (the first colour of the cycle) under the matplotlib themes, as matplotlib's <c>axhline</c> draws a line that names no colour; set
/// <see cref="Indicator.Color"/> for a colour of your own under every theme.</remarks>
public sealed class FibonacciRetracement : Indicator<SignalResult>
{
    private readonly double _low;
    private readonly double _high;

    /// <summary>The standard Fibonacci retracement ratios: the inner ratios of the retracement tool, without its 0 % and 100 %.</summary>
    public static readonly double[] Levels = Models.Tools.FibonacciRetracement.Ratios[1..^1];

    /// <summary>Creates a new Fibonacci Retracement indicator between the given price extremes.</summary>
    /// <param name="low">The swing low price.</param>
    /// <param name="high">The swing high price.</param>
    public FibonacciRetracement(double low, double high)
    {
        _low = low;
        _high = high;
        Label = "Fib";
        LineStyle = LineStyle.Dashed;
    }

    /// <inheritdoc />
    public override SignalResult Compute() => Array.ConvertAll(Levels, level => Models.Tools.FibonacciRetracement.PriceAt(level, _high, _low));

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        foreach (double level in Levels)
        {
            var line = axes.AxHLine(Models.Tools.FibonacciRetracement.PriceAt(level, _high, _low));
            line.Color = Color;
            line.VerbColor = Colors.FibonacciOrange;
            line.LineStyle = LineStyle;
            line.LineWidth = LineWidth;
            line.Label = $"{(level * 100).InvariantText("0.0")}%";
        }
    }
}
