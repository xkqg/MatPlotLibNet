// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models;

/// <summary>Specifies the coordinate system used for rendering axes.</summary>
/// <remarks><b>Append-only ordinal contract (Phase O of v1.7.2):</b> never reorder,
/// remove, or renumber. See <c>EnumOrdinalContractTests</c>.</remarks>
public enum CoordinateSystem
{
    /// <summary>Standard Cartesian (X, Y) coordinate system.</summary>
    Cartesian = 0,

    /// <summary>Polar (r, theta) coordinate system with circular grid.</summary>
    Polar = 1,

    /// <summary>Three-dimensional (X, Y, Z) coordinate system with projection.</summary>
    ThreeD = 2,
}
