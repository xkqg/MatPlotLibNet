// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models;

/// <summary>Specifies the orientation of a reference line or span region.</summary>
public enum Orientation
{
    /// <summary>Horizontal (constant Y value).</summary>
    Horizontal,

    /// <summary>Vertical (constant X value).</summary>
    Vertical
}

/// <summary>Represents a horizontal or vertical reference line at a specific data value.</summary>
public sealed class ReferenceLine
{
    /// <summary>Gets the data value where the line is drawn.</summary>
    public double Value { get; }

    /// <summary>Gets the orientation of the reference line.</summary>
    public Orientation Orientation { get; }

    /// <summary>Gets or sets the line color.</summary>
    public Color? Color { get; set; }

    /// <summary>Gets or sets the line style.</summary>
    public LineStyle LineStyle { get; set; } = LineStyle.Dashed;

    /// <summary>Gets or sets the line width.</summary>
    public double LineWidth { get; set; } = 1.0;

    /// <summary>Gets or sets the optional label for the reference line.</summary>
    public string? Label { get; set; }

    /// <summary>Creates a new reference line at the given value and orientation.</summary>
    public ReferenceLine(double value, Orientation orientation)
    {
        Value = value;
        Orientation = orientation;
    }
}
