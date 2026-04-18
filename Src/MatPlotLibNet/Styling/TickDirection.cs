// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>Specifies the direction in which tick marks are drawn relative to the axis line.</summary>
/// <remarks><b>Append-only ordinal contract (Phase O of v1.7.2):</b> never reorder,
/// remove, or renumber. See <c>EnumOrdinalContractTests</c>.</remarks>
public enum TickDirection
{
    /// <summary>Tick marks extend inward into the plot area.</summary>
    In = 0,

    /// <summary>Tick marks extend outward away from the plot area (default).</summary>
    Out = 1,

    /// <summary>Tick marks extend both inward and outward, crossing the axis line.</summary>
    InOut = 2,
}
