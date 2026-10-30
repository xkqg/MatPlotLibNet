// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Tools;

/// <summary>Represents a horizontal price level (support/resistance) drawn across the full axes width.</summary>
public sealed class HorizontalLevel
{
    /// <summary>The price the level stands at, a Y value in data units.</summary>
    public double Value { get; }

    /// <summary>The colour of the line and its label. <see langword="null"/> (the default) takes the theme's neutral
    /// colour.</summary>
    public Color? Color { get; set; }
    /// <summary>The dash pattern of the line. Dashed by default.</summary>
    public LineStyle LineStyle { get; set; } = LineStyle.Dashed;
    /// <summary>The width of the line, in pixels. Default 1.</summary>
    public double LineWidth { get; set; } = 1.0;
    /// <summary>Text drawn above the right end of the line, in the line's colour. <see langword="null"/> (the default)
    /// draws none.</summary>
    public string? Label { get; set; }

    /// <summary>Creates a horizontal level at the given price.</summary>
    /// <param name="value">The price the level stands at, a Y value in data units.</param>
    public HorizontalLevel(double value)
    {
        Value = value;
    }
}
