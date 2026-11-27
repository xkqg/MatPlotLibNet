// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Styling;

/// <summary>What the markers of a line are outlined with where its author names nothing (matplotlib's <c>Line2D</c> under a style), held
/// by <see cref="MarkerLook.LineEdge"/>. <see langword="null"/> there (the library's own themes) leaves a marker without an outline unless
/// its author names an edge colour, as it has always been drawn.</summary>
/// <param name="EdgeWidth">How wide the outline is, in pixels: <c>lines.markeredgewidth</c>, which is a length in points: 1 under the default
/// style and 0.5 under the classic one. It is also the width an unfilled marker (a plus, a cross) is stroked with.</param>
/// <param name="FilledEdge">The colour a filled marker is outlined in when its author names none: <see langword="null"/> for the colour of
/// its line (<c>markeredgecolor = 'auto'</c>, as the default style draws it), black in the classic style, whose <c>'auto'</c> gives a
/// filled marker <c>'k'</c> ("Bad hard-wired default") and an unfilled one the colour of its line.</param>
internal readonly record struct LineMarkerLook(double EdgeWidth, Color? FilledEdge) : IThemeKeys<LineMarkerLook?>
{
    /// <inheritdoc />
    public void WriteKeys(ThemeDto keys)
    {
        keys.LineMarkerEdgeWidth = EdgeWidth;
        keys.LineMarkerFilledEdge = FilledEdge;
    }

    /// <inheritdoc />
    /// <remarks>A record that names no width names no look: a theme that names none leaves a marker as it has always been drawn.</remarks>
    public static LineMarkerLook? ReadKeys(ThemeDto keys) =>
        keys.LineMarkerEdgeWidth is { } width ? new LineMarkerLook(width, keys.LineMarkerFilledEdge) : null;
}