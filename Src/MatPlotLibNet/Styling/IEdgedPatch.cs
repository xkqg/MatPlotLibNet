// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>Something drawn as a patch whose edge a theme can decide: a bar, a wedge, a bin, a fill, a violin's body, a treemap's tile, a
/// table's cell, and the <see cref="Models.Patches.Patch"/> of <c>AddPatch</c>. It states what is its own, as values; the one rule
/// that joins them with the theme's is <see cref="ResolvedPatchEdge.StyleOf(Color, PatchEdge, HatchFill)"/> (the edge:
/// <see cref="Theme.PatchEdgeOf"/>), which no kind repeats.</summary>
/// <remarks>A kind that gives no edge and draws none under a library look (the default of both) says nothing.</remarks>
internal interface IEdgedPatch : Models.Series.IMatplotlibArtist
{
    /// <summary>The edge colour and width its author gave, and the opacity of the whole patch that matplotlib applies to the edge too.
    /// None by default.</summary>
    GivenPatchEdge GivenEdge => default;

    /// <summary>The edge it is drawn with where the theme does not decide (the library's own themes): its own rule. No edge by default.</summary>
    /// <param name="fill">The colour it is filled with, <see langword="null"/> when it has several and its edge follows none of them.</param>
    /// <param name="themeEdge">The theme's patch edge colour (<see cref="Theme.PatchEdgeColor"/>), <see langword="null"/> when it names none.</param>
    /// <returns>The edge.</returns>
    PatchEdge LibraryEdge(Color? fill, Color? themeEdge) => PatchEdge.None;

    /// <summary>What it is to matplotlib, whose <see cref="Models.Series.MatplotlibArtist.Collection"/> decides the join of its outline's
    /// corners: a <c>Patch</c> by default (the patch of <c>AddPatch</c>); a series says it through its kind's artist.</summary>
    Models.Series.MatplotlibArtist Models.Series.IMatplotlibArtist.Artist => Models.Series.MatplotlibArtist.Patch;
}
