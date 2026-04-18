// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>Specifies the orientation of a color bar.</summary>
/// <remarks><b>Append-only ordinal contract (Phase O of v1.7.2):</b> never reorder,
/// remove, or renumber. See <c>EnumOrdinalContractTests</c>.</remarks>
public enum ColorBarOrientation
{
    /// <summary>The color bar is drawn vertically alongside the plot area (default).</summary>
    Vertical = 0,

    /// <summary>The color bar is drawn horizontally below the plot area.</summary>
    Horizontal = 1,
}
