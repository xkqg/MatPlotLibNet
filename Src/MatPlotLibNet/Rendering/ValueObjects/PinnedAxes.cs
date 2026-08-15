// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>
/// Which coordinate components of an emitted 3-D vertex sit ON a camera-selected cube face, and
/// therefore have to be mirrored when the browser re-runs the face selection mid-drag. In the
/// centred normalized space the SVG carries, the opposite plane of an axis is the exact negation of
/// that component, so "mirror" is a sign flip and needs no extra geometry on the wire.
/// </summary>
[Flags]
internal enum PinnedAxes
{
    /// <summary>Nothing on this element follows the face selection.</summary>
    None = 0,

    /// <summary>The X component sits on the selected X plane.</summary>
    X = 1,

    /// <summary>The Y component sits on the selected Y plane.</summary>
    Y = 2,

    /// <summary>The Z component sits on the selected Z plane.</summary>
    Z = 4,
}

/// <summary>Wire-format helpers for <see cref="PinnedAxes"/> and <see cref="CubeAxis"/>.</summary>
internal static class PinnedAxesExtensions
{
    /// <summary>The compact <c>data-v3d-pinned</c> attribute value: the axis letters, in x-y-z order.</summary>
    internal static string ToWire(this PinnedAxes axes)
    {
        Span<char> buffer = stackalloc char[3];
        int length = 0;
        if (axes.HasFlag(PinnedAxes.X)) buffer[length++] = 'x';
        if (axes.HasFlag(PinnedAxes.Y)) buffer[length++] = 'y';
        if (axes.HasFlag(PinnedAxes.Z)) buffer[length++] = 'z';
        return new string(buffer[..length]);
    }

    /// <summary>The two axes other than this one — the components a tick row along it is pinned to.</summary>
    internal static PinnedAxes OtherAxes(this CubeAxis axis) => axis switch
    {
        CubeAxis.X => PinnedAxes.Y | PinnedAxes.Z,
        CubeAxis.Y => PinnedAxes.X | PinnedAxes.Z,
        _ => PinnedAxes.X | PinnedAxes.Y,
    };
}
