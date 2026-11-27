// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Styling;

/// <summary>The shapes of the markers, and the edge of a line's markers where their author names nothing.</summary>
internal sealed record MarkerLook : IThemeKeys<MarkerLook>
{
    /// <summary>The library's own markers.</summary>
    internal static MarkerLook Library { get; } = new();

    /// <inheritdoc />
    public void WriteKeys(ThemeDto keys)
    {
        keys.MarkersFillTheirBox = FillTheirBox.OrNullAtRest(false);
        LineEdge?.WriteKeys(keys);
    }

    /// <inheritdoc />
    public static MarkerLook ReadKeys(ThemeDto keys) =>
        new() { FillTheirBox = keys.MarkersFillTheirBox ?? false, LineEdge = LineMarkerLook.ReadKeys(keys) };

    /// <summary>How big a marker is, in points, where its series names no size (matplotlib's <c>lines.markersize</c>): the one number a
    /// line, a streaming scatter, a polar or ternary scatter, a Piper diagram, a 3-D scatter and a 3-D stem read.</summary>
    internal const double RestingSize = 6;

    /// <summary>Whether a triangle and a diamond fill the box of the marker's size as matplotlib's marker paths do (<c>'^'</c> 1 x 1
    /// of the size, <c>'D'</c> 1.414 x 1.414). Otherwise they are inscribed in the circle of the size. <c>MarkerShape.Of(style,
    /// theme)</c> holds the shapes.</summary>
    public bool FillTheirBox { get; init; }

    /// <summary>What the markers of a line are outlined with (matplotlib's <c>lines.markeredgewidth</c> and the colour of
    /// <c>markeredgecolor = 'auto'</c>). <see langword="null"/>: no outline unless their author names an edge colour.
    /// <c>SeriesSlot.LineMarkerStyleOf</c> holds the rule.</summary>
    public LineMarkerLook? LineEdge { get; init; }
}
