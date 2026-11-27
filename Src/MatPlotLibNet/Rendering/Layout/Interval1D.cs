// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Layout;

/// <summary>A span on one axis: where it starts and how long it is. Sankey nodes along their cross axis and
/// direct end labels along the vertical axis both reduce to this shape.</summary>
/// <param name="Start">Lower edge of the span.</param>
/// <param name="Size">Length of the span, never negative.</param>
internal readonly record struct Interval1D(double Start, double Size)
{
    /// <summary>Midpoint of the span; the key <see cref="Interval1DExtensions.SpreadApart"/> orders by.</summary>
    public double Centre => Start + Size / 2;

    /// <summary>Upper edge of the span.</summary>
    public double End => Start + Size;
}
