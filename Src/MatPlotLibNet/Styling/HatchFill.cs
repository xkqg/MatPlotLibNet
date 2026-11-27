// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>The pattern laid over a fill and the colour of its strokes, as one value: what a series that hatches holds as its
/// <c>Hatch</c> and <c>HatchColor</c>, what its renderer hands to the one rule that styles a patch
/// (<see cref="ResolvedPatchEdge.StyleOf(Color, PatchEdge, HatchFill)"/>) and what its JSON carries.</summary>
/// <param name="Pattern">The pattern; <see cref="HatchPattern.None"/> lays none.</param>
/// <param name="Color">The colour its author named for the strokes, <see langword="null"/> while none was named and the theme decides
/// (<see cref="PatchLook.HatchOver"/>).</param>
internal readonly record struct HatchFill(HatchPattern Pattern, Color? Color)
{
    /// <summary>No hatch.</summary>
    internal static HatchFill None => default;

    /// <summary>Whether a pattern is laid over the fill.</summary>
    internal bool IsDrawn => Pattern != HatchPattern.None;

    /// <summary>The pattern as the JSON writes it: <see langword="null"/> when none is laid, so an unhatched series writes no hatch bytes
    /// and its pinned wire stays as it was.</summary>
    internal HatchPattern? PatternOnTheWire => IsDrawn ? Pattern : null;

    /// <summary>The hatch a document carries, read back: the read half of <see cref="PatternOnTheWire"/>, which every wire form (a
    /// series, a patch, a tree node) asks. A pattern that is absent is none; the colour is the one named, absent while none was.</summary>
    /// <param name="hatch">The pattern the document carries, <see langword="null"/> when it carries none.</param>
    /// <param name="color">The colour the document carries for the strokes, <see langword="null"/> when none.</param>
    /// <returns>The hatch.</returns>
    internal static HatchFill FromTheWire(HatchPattern? hatch, Color? color) => new(hatch ?? HatchPattern.None, color);
}
