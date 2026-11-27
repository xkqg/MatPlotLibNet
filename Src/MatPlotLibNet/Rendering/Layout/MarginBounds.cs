// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Layout;

/// <summary>How far one margin or one gap of a tight or a constrained layout may shrink and grow: a floor and a cap. The cap holds
/// what the layout only estimates (the tick labels of a polar or a 3-D axes, a margin the caller declared past it); what an axes is
/// measured to draw there lifts it, so measured content is never cut.</summary>
/// <param name="Floor">The least the margin or the gap is, in pixels.</param>
/// <param name="Cap">The most it is, in pixels, before what is measured there lifts it.</param>
internal readonly record struct MarginBounds(double Floor, double Cap)
{
    /// <summary>No bound: a margin or a gap is what its axes need, as matplotlib's <c>tight_layout</c> keeps it.</summary>
    internal static MarginBounds None { get; } = new(double.NegativeInfinity, double.PositiveInfinity);

    /// <summary><paramref name="value"/> held between the floor and the cap, the cap lifted to <paramref name="measured"/>.</summary>
    /// <param name="value">The margin or the gap the needs give.</param>
    /// <param name="measured">What the axes there are measured to draw: the cap is never below it.</param>
    /// <returns>The margin or the gap, in pixels.</returns>
    internal double Clamp(double value, double measured) => Math.Clamp(value, Floor, Math.Max(Cap, measured));
}
