// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.SeriesRenderers;

namespace MatPlotLibNet.Rendering;

/// <summary>A room between two parts of an axis drawn in parts, on the screen (<see cref="PixelDirection.RoomAt"/>): its first and last
/// pixel along the axis, and how much further a part sees past it than the room shows (<see cref="AxisBreakMap.Jumps"/> in pixels).</summary>
internal readonly record struct PixelRoom(double Lo, double Hi, double Jump)
{
    /// <summary>Whether a segment from <paramref name="from"/> to <paramref name="to"/> along the axis, either way, crosses the room.</summary>
    internal bool IsCrossedBy(double from, double to) => Lo >= Math.Min(from, to) && Hi <= Math.Max(from, to);
}
