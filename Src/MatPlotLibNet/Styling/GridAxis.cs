// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>Specifies which axes receive grid lines.</summary>
/// <remarks><b>Append-only ordinal contract (Phase O of v1.7.2):</b> never reorder,
/// remove, or renumber. See <c>EnumOrdinalContractTests</c>.</remarks>
public enum GridAxis
{
    /// <summary>Draw grid lines perpendicular to the X axis (vertical lines).</summary>
    X = 0,

    /// <summary>Draw grid lines perpendicular to the Y axis (horizontal lines).</summary>
    Y = 1,

    /// <summary>Draw grid lines for both axes (default).</summary>
    Both = 2,
}
