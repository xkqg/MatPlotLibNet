// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models;

/// <summary>Specifies the coordinate system used for rendering axes.</summary>
public enum CoordinateSystem
{
    /// <summary>Standard Cartesian (X, Y) coordinate system.</summary>
    Cartesian,

    /// <summary>Polar (r, theta) coordinate system with circular grid.</summary>
    Polar,

    /// <summary>Three-dimensional (X, Y, Z) coordinate system with projection.</summary>
    ThreeD
}
