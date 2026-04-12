// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>
/// Specifies the dash pattern used to render a line.
/// </summary>
public enum LineStyle
{
    /// <summary>A continuous solid line.</summary>
    Solid,

    /// <summary>A line composed of dashes.</summary>
    Dashed,

    /// <summary>A line composed of dots.</summary>
    Dotted,

    /// <summary>A line alternating between dashes and dots.</summary>
    DashDot,

    /// <summary>No line is drawn.</summary>
    None
}
