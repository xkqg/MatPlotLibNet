// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models;

/// <summary>Specifies the orientation of a reference line or span region.</summary>
/// <remarks><b>Append-only ordinal contract (Phase O of v1.7.2):</b> never reorder,
/// remove, or renumber. See <c>EnumOrdinalContractTests</c>.</remarks>
public enum Orientation
{
    /// <summary>Horizontal (constant Y value).</summary>
    Horizontal = 0,

    /// <summary>Vertical (constant X value).</summary>
    Vertical = 1,
}

/// <summary>Represents a horizontal or vertical reference line at a specific data value.</summary>
public sealed class ReferenceLine
{
    public double Value { get; }

    public Orientation Orientation { get; }

    public Color? Color { get; set; }

    public LineStyle LineStyle { get; set; } = LineStyle.Dashed;

    public double LineWidth { get; set; } = 1.0;

    public string? Label { get; set; }

    /// <summary>Creates a new reference line at the given value and orientation.</summary>
    public ReferenceLine(double value, Orientation orientation)
    {
        Value = value;
        Orientation = orientation;
    }
}
