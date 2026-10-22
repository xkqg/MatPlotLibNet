// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>Specifies the dash pattern used to render a line.</summary>
/// <remarks><b>Append-only ordinal contract:</b> never reorder,
/// remove, or renumber.</remarks>
public enum LineStyle
{
    /// <summary>A continuous solid line.</summary>
    Solid = 0,

    /// <summary>A line composed of dashes.</summary>
    Dashed = 1,

    /// <summary>A line composed of dots.</summary>
    Dotted = 2,

    /// <summary>A line alternating between dashes and dots.</summary>
    DashDot = 3,

    /// <summary>No line is drawn.</summary>
    None = 4,
}
