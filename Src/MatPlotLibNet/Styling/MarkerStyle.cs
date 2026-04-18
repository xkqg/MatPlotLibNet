// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>Specifies the shape used to render data-point markers.</summary>
/// <remarks><b>Append-only ordinal contract (Phase O of v1.7.2):</b> never reorder,
/// remove, or renumber. See <c>EnumOrdinalContractTests</c>.</remarks>
public enum MarkerStyle
{
    /// <summary>No marker is drawn.</summary>
    None = 0,

    /// <summary>A circle marker.</summary>
    Circle = 1,

    /// <summary>A square marker.</summary>
    Square = 2,

    /// <summary>An upward-pointing triangle marker.</summary>
    Triangle = 3,

    /// <summary>A diamond marker.</summary>
    Diamond = 4,

    /// <summary>An X-shaped cross marker.</summary>
    Cross = 5,

    /// <summary>A plus-sign marker.</summary>
    Plus = 6,

    /// <summary>A star marker.</summary>
    Star = 7,

    /// <summary>A pentagon marker.</summary>
    Pentagon = 8,

    /// <summary>A hexagon marker.</summary>
    Hexagon = 9,

    /// <summary>A downward-pointing triangle marker.</summary>
    TriangleDown = 10,

    /// <summary>A left-pointing triangle marker.</summary>
    TriangleLeft = 11,

    /// <summary>A right-pointing triangle marker.</summary>
    TriangleRight = 12,
}
