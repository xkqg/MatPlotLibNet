// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>One value for each direction of the plot: <see cref="X"/> along X, <see cref="Y"/> along Y. A type that keeps the same thing
/// for both directions (how a transform places a value, the ends a transform shows, the link of a linked axis, the scale of a device grid)
/// holds one pair, and a direction picks its member through <see cref="AlongExtensions"/> (<c>pair.Along(alongX)</c>), so no type picks
/// one with a ternary of its own. The members are fields, so a pick hands back a reference to the one held and never copies a value as
/// large as a <see cref="PixelDirection"/> on the path every point takes.</summary>
/// <typeparam name="T">What is held per direction.</typeparam>
/// <param name="x">The value along X.</param>
/// <param name="y">The value along Y.</param>
internal readonly struct AxisPair<T>(T x, T y)
{
    /// <summary>The value along X.</summary>
    internal readonly T X = x;

    /// <summary>The value along Y.</summary>
    internal readonly T Y = y;
}
