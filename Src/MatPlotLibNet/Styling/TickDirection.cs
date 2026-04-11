// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>Specifies the direction in which tick marks are drawn relative to the axis line.</summary>
public enum TickDirection
{
    /// <summary>Tick marks extend inward into the plot area.</summary>
    In,

    /// <summary>Tick marks extend outward away from the plot area (default).</summary>
    Out,

    /// <summary>Tick marks extend both inward and outward, crossing the axis line.</summary>
    InOut
}
