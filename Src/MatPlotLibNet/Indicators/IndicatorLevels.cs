// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Indicators;

/// <summary>The two guide lines an oscillator draws across its panel beside its signal: the overbought and the oversold level,
/// such as +100 and -100 for the commodity channel index or -20 and -80 for Williams %R. An indicator that draws them offers
/// them as its <c>Levels</c>, so one way styles them for every such indicator: give each line a colour, change the dash or the
/// width of both, or set <see cref="Visible"/> to <see langword="false"/> to leave them out. Left alone they are two dashed
/// lines, a pixel wide, in the theme's neutral colour.</summary>
/// <remarks>The lines are added to the axes the indicator is applied to as ordinary reference lines, once for every axes, so the
/// same indicator applied to two axes gives each its own pair.</remarks>
public sealed class IndicatorLevels
{
    private readonly double _upper;
    private readonly double _lower;

    internal IndicatorLevels(double upper, double lower)
    {
        _upper = upper;
        _lower = lower;
    }

    /// <summary>Whether the lines are drawn. True by default.</summary>
    public bool Visible { get; set; } = true;

    /// <summary>The colour of the upper line, the overbought level, or <see langword="null"/> (the default) for the theme's
    /// neutral colour.</summary>
    public Color? UpperColor { get; set; }

    /// <summary>The colour of the lower line, the oversold level, or <see langword="null"/> (the default) for the theme's
    /// neutral colour.</summary>
    public Color? LowerColor { get; set; }

    /// <summary>The dash pattern of both lines. Dashed by default.</summary>
    public LineStyle LineStyle { get; set; } = LineStyle.Dashed;

    /// <summary>The width of both lines, in pixels. Default 1.</summary>
    public double LineWidth { get; set; } = 1.0;

    /// <summary>Adds the lines to <paramref name="axes"/>, unless they are not <see cref="Visible"/>: the upper line first, then the
    /// lower.</summary>
    /// <param name="axes">The axes the lines are added to.</param>
    internal void Apply(Axes axes)
    {
        if (!Visible) return;
        Draw(axes, _upper, UpperColor);
        Draw(axes, _lower, LowerColor);
    }

    private void Draw(Axes axes, double value, Color? color)
    {
        var line = axes.AxHLine(value);
        line.Color = color;
        line.LineStyle = LineStyle;
        line.LineWidth = LineWidth;
    }
}
