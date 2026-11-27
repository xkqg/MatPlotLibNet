// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models.Patches;

/// <summary>The axis a patch's plane stands across on a 3-D axes (<see cref="Patch.ZDir"/>), matplotlib's <c>zdir</c> of
/// <c>pathpatch_2d_to_3d</c>: the patch's own x and y become the two other axes, at the height <see cref="Patch.Z"/> along this
/// one.</summary>
public enum ZDirection
{
    /// <summary>Across the Z axis: the patch lies in the X-Y plane at z = <see cref="Patch.Z"/> (the default).</summary>
    Z,

    /// <summary>Across the X axis: the patch's x and y are the 3-D y and z, at x = <see cref="Patch.Z"/>.</summary>
    X,

    /// <summary>Across the Y axis: the patch's x and y are the 3-D x and z, at y = <see cref="Patch.Z"/>.</summary>
    Y,
}
