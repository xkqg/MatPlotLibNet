// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>The edge a patch is drawn with: its colour, <see langword="null"/> for none, and its width in pixels.</summary>
/// <param name="Color">The colour of the edge; <see langword="null"/> draws none.</param>
/// <param name="Width">The width of the edge, in pixels; 0 when there is none.</param>
internal readonly record struct PatchEdge(Color? Color, double Width)
{
    /// <summary>The style of a shape filled in <paramref name="fill"/> and edged by this edge: the one place an edge and a fill become a
    /// shape, so no renderer glues them by hand.</summary>
    /// <param name="fill">The colour of the fill.</param>
    /// <param name="hatched">Whether a hatch is laid over the fill, which keeps a fill of no opacity as a clear fill under it.</param>
    /// <returns>The style: the fill, this edge's colour and width, no hatch. A fill of no opacity is no fill unless it is hatched, and
    /// an edge of no opacity is no outline, as matplotlib's <c>Patch.draw</c> passes no face and a width of 0 for them.</returns>
    internal ShapeStyle StyleOf(Color fill, bool hatched = false) =>
        ShapeStyle.Edged(!fill.IsNone || hatched ? fill : null, Color is { } drawn && !drawn.IsNone ? drawn : null, Width);

    /// <summary>No edge.</summary>
    internal static PatchEdge None => default;

    /// <summary>An edge one pixel wide in <paramref name="color"/>: the library's own outline of a wedge, a body and a cell.</summary>
    /// <param name="color">The colour of the edge, <see langword="null"/> for none.</param>
    /// <returns>The edge.</returns>
    internal static PatchEdge Pixel(Color? color) => new(color, 1);

    /// <summary>One white pixel: the library's own outline of a pie's wedge and a treemap's tile.</summary>
    internal static PatchEdge WhitePixel => Pixel(Colors.White);

    /// <summary>An edge half a pixel wide in <paramref name="color"/>: the library's own hairline round a bar.</summary>
    /// <param name="color">The colour of the edge, <see langword="null"/> for none.</param>
    /// <returns>The edge.</returns>
    internal static PatchEdge HalfPixel(Color? color) => new(color, 0.5);
}
