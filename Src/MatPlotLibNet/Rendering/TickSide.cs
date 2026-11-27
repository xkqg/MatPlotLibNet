// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>The edge of the plot area an axis hangs its ticks from. What an edge means to the drawing is its
/// <see cref="EdgeTerms"/> (<see cref="EdgeTerms.Of(TickSide)"/>).</summary>
internal enum TickSide
{
    /// <summary>The primary X axis: marks run along the bottom edge, labels sit below it.</summary>
    Bottom,

    /// <summary>The primary Y axis: marks run along the left edge, labels sit to its left.</summary>
    Left,

    /// <summary>The secondary Y axis or the mirrored primary Y ticks: labels sit to the right of the right edge.</summary>
    Right,

    /// <summary>The secondary X axis or the mirrored primary X ticks: labels sit above the top edge.</summary>
    Top,
}
