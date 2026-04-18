// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>Specifies the horizontal alignment of an axes title.</summary>
/// <remarks><b>Append-only ordinal contract (Phase O of v1.7.2):</b> never reorder,
/// remove, or renumber. See <c>EnumOrdinalContractTests</c>.</remarks>
public enum TitleLocation
{
    /// <summary>Align the title to the left edge of the plot area.</summary>
    Left = 0,

    /// <summary>Center the title above the plot area (default).</summary>
    Center = 1,

    /// <summary>Align the title to the right edge of the plot area.</summary>
    Right = 2,
}
