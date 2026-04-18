// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling.ColorMaps;

/// <summary>Controls whether extension slots are drawn at the ends of a color bar
/// to indicate under-range and/or over-range values.</summary>
/// <remarks><b>Append-only ordinal contract (Phase O of v1.7.2):</b> never reorder,
/// remove, or renumber. See <c>EnumOrdinalContractTests</c>.</remarks>
public enum ColorBarExtend
{
    /// <summary>No extension slots.</summary>
    Neither = 0,

    /// <summary>Extension slot at the minimum (under-range) end only.</summary>
    Min = 1,

    /// <summary>Extension slot at the maximum (over-range) end only.</summary>
    Max = 2,

    /// <summary>Extension slots at both ends.</summary>
    Both = 3,
}
