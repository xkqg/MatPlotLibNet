// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models;

/// <summary>Specifies the orientation of a reference line or span region.</summary>
/// <remarks><b>Append-only ordinal contract:</b> never reorder,
/// remove, or renumber.</remarks>
public enum Orientation
{
    /// <summary>Horizontal (constant Y value).</summary>
    Horizontal = 0,

    /// <summary>Vertical (constant X value).</summary>
    Vertical = 1,
}

/// <summary>Specifies which side of a threshold value constitutes a breach zone.</summary>
/// <remarks><b>Append-only ordinal contract:</b> never reorder,
/// remove, or renumber.</remarks>
public enum ThresholdBreach
{
    /// <summary>Breach zone is above (or to the right of) the threshold value.</summary>
    Above = 0,

    /// <summary>Breach zone is below (or to the left of) the threshold value.</summary>
    Below = 1,
}

/// <summary>Represents a horizontal or vertical reference line at a specific data value.</summary>
public sealed class ReferenceLine
{
    /// <summary>The data value the line sits at. Settable, so a line that tracks something — a playback position,
    /// a live threshold — moves in place instead of the whole figure being rebuilt, which would also throw away the
    /// reader's zoom.</summary>
    public double Value { get; set; }

    /// <summary>Whether the line runs across the plot horizontally, at a constant Y value, or vertically, at a constant X
    /// value. Fixed when the line is created.</summary>
    public Orientation Orientation { get; }

    /// <summary>The colour of the line and its label. <see langword="null"/> (the default) takes the theme's neutral
    /// colour.</summary>
    public Color? Color { get; set; }

    /// <summary>The dash pattern of the line. Dashed by default.</summary>
    public LineStyle LineStyle { get; set; } = LineStyle.Dashed;

    /// <summary>The width of the line, in pixels. Default 1.</summary>
    public double LineWidth { get; set; } = 1.0;

    /// <summary>Text drawn beside the line, at the right end of a horizontal line and the top of a vertical one, in the
    /// line's colour. <see langword="null"/> (the default) draws none.</summary>
    public string? Label { get; set; }

    /// <summary>Creates a new reference line at the given value and orientation.</summary>
    /// <param name="value">The data value the line sits at: a Y value for a horizontal line, an X value for a vertical one.</param>
    /// <param name="orientation">Whether the line is horizontal (constant Y) or vertical (constant X).</param>
    public ReferenceLine(double value, Orientation orientation)
    {
        Value = value;
        Orientation = orientation;
    }
}
