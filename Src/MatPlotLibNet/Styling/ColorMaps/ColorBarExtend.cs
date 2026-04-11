// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling.ColorMaps;

/// <summary>Controls whether extension slots are drawn at the ends of a color bar
/// to indicate under-range and/or over-range values.</summary>
public enum ColorBarExtend
{
    /// <summary>No extension slots.</summary>
    Neither,

    /// <summary>Extension slot at the minimum (under-range) end only.</summary>
    Min,

    /// <summary>Extension slot at the maximum (over-range) end only.</summary>
    Max,

    /// <summary>Extension slots at both ends.</summary>
    Both,
}
