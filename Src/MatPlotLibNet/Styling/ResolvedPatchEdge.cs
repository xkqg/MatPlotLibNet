// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>What a theme decides about the edge of one patch series, worked out once (<see cref="Theme.PatchEdgesOf"/>), so a series
/// that draws many shapes (a treemap's tiles, a table's cells) asks its theme and itself once, not once for every shape. It is also the
/// one place a patch-like shape's style is made: its fill, its edge, the join of its corners and the colour of its hatch
/// (<see cref="StyleOf(Color, PatchEdge, HatchFill)"/>).</summary>
/// <param name="Patch">What is drawn: its own library edge.</param>
/// <param name="Themed">The edge the theme decided (<see cref="PatchLook.EdgeOver"/>), <see langword="null"/> under the library's own look.</param>
/// <param name="ThemeEdge">The theme's patch edge colour (<see cref="Theme.PatchEdgeColor"/>), <see langword="null"/> when it names none.</param>
/// <param name="Looks">The looks of the theme, which the join of a collection and the colour of a hatch are read from.</param>
/// <param name="Artist">What the series is to matplotlib (<see cref="Models.Series.ChartSeries.Artist"/>), which says whether its shapes are a
/// collection's, asked once, not for every shape.</param>
internal readonly record struct ResolvedPatchEdge(IEdgedPatch Patch, PatchEdge? Themed, Color? ThemeEdge, ThemeLooks Looks, Models.Series.MatplotlibArtist Artist)
{
    /// <summary>The edge of a shape filled in <paramref name="fill"/>: the theme's where it decided (at the opacity given to the series,
    /// <see cref="GivenPatchEdge.Alpha"/>), else the series' own.</summary>
    /// <param name="fill">The colour the shape is filled with, <see langword="null"/> when its edge follows none.</param>
    /// <returns>The edge; none is <see cref="PatchEdge.None"/>.</returns>
    internal PatchEdge Of(Color? fill = null) => Resolve(Patch, Themed, ThemeEdge, fill);

    /// <summary>The one rule that joins what a theme decides about an edge with the kind's own: the theme's where it decided, else the
    /// kind's own under the library's themes, with the theme's patch edge colour at hand. Asked by <see cref="Of"/> for a series that styles
    /// many shapes and by <see cref="Theme.PatchEdgeOf"/> for one that styles a single shape, which resolve nothing they would not use.</summary>
    /// <param name="patch">What is drawn: its own library edge.</param>
    /// <param name="themed">The edge the theme decided, <see langword="null"/> under the library's own look.</param>
    /// <param name="themeEdge">The theme's patch edge colour, <see langword="null"/> when it names none.</param>
    /// <param name="fill">The colour the shape is filled with, <see langword="null"/> when its edge follows none.</param>
    /// <returns>The edge; none is <see cref="PatchEdge.None"/>.</returns>
    internal static PatchEdge Resolve(IEdgedPatch patch, PatchEdge? themed, Color? themeEdge, Color? fill) =>
        themed ?? patch.LibraryEdge(fill, themeEdge);

    /// <summary>The style of a shape of this series filled in <paramref name="fill"/>, edged as its theme decides for an edge that follows
    /// <paramref name="follows"/>.</summary>
    /// <param name="fill">The colour the shape is filled with, its opacity included.</param>
    /// <param name="follows">The colour the series' own edge follows (a bar's, drawn in the colour the series names, before the opacity
    /// fades the fill), <see langword="null"/> when it follows none.</param>
    /// <param name="hatch">The pattern laid over the fill and the colour its author named for it; none by default.</param>
    /// <returns>The style to draw the shape with.</returns>
    internal ShapeStyle StyleOf(Color fill, Color? follows, HatchFill hatch = default) => StyleOf(fill, Of(follows), hatch);

    /// <summary>The style of a shape of this series filled in <paramref name="fill"/> and edged by <paramref name="edge"/>, an edge
    /// already resolved by <see cref="Of"/> (a series that styles many shapes resolves it once): the edge and the fill made into a shape,
    /// the pattern laid over it in the colour its author named or the theme's rule gives over the edge, the edge given and the fill
    /// (<see cref="PatchLook.HatchOver"/>, the one rule for every patch-like shape, a patch of <c>AddPatch</c> included), and the whole
    /// joined at its corners as the series is to matplotlib (a collection takes its theme's join, <see cref="StrokeLook.JoinedAs"/>).</summary>
    /// <param name="fill">The colour the shape is filled with, its opacity included.</param>
    /// <param name="edge">The edge of the shape.</param>
    /// <param name="hatch">The pattern laid over the fill and the colour its author named for it; none by default.</param>
    /// <returns>The style to draw the shape with.</returns>
    internal ShapeStyle StyleOf(Color fill, PatchEdge edge, HatchFill hatch = default)
    {
        var style = edge.StyleOf(fill, hatch.IsDrawn);
        if (hatch.IsDrawn)
        {
            var given = Patch.GivenEdge;
            style = style.WithHatch(Looks.Patch.HatchOver(hatch, new HatchGround(edge.Color, given.Color, fill, given.Alpha)));
        }

        return Looks.Strokes.JoinedAs(Artist, style);
    }
}
