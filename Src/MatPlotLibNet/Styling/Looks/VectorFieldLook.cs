// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Styling;

/// <summary>How a quiver, its key, wind barbs and the arrows of streamlines are drawn where their author names nothing.</summary>
internal sealed record VectorFieldLook : IThemeKeys<VectorFieldLook>
{
    /// <summary>The library's own vector fields.</summary>
    internal static VectorFieldLook Library { get; } = new();

    /// <inheritdoc />
    public void WriteKeys(ThemeDto keys)
    {
        keys.VectorFieldColor = Color;
        keys.QuiverAutoScale = QuiverAutoScale.OrNullAtRest(false);
        keys.BarbsLikeMatplotlib = BarbsLikeMatplotlib.OrNullAtRest(false);
        keys.StreamArrowsLikeMatplotlib = StreamArrowsLikeMatplotlib.OrNullAtRest(false);
    }

    /// <inheritdoc />
    public static VectorFieldLook ReadKeys(ThemeDto keys) => new()
    {
        Color = keys.VectorFieldColor,
        QuiverAutoScale = keys.QuiverAutoScale ?? false,
        BarbsLikeMatplotlib = keys.BarbsLikeMatplotlib ?? false,
        StreamArrowsLikeMatplotlib = keys.StreamArrowsLikeMatplotlib ?? false,
    };

    /// <summary>The colour a quiver's arrows, its key and barbs are drawn in when their author names none and they hold a place of
    /// their own in the cycle: black under the matplotlib themes (<c>Quiver(..., color='k')</c>, <c>Barbs</c>'s <c>'k'</c> faces:
    /// defaults of the calls, not of a style). Unset: the colour of their place in the cycle.</summary>
    public Color? Color { get; init; }

    /// <summary>Whether a quiver that names no scale is drawn as <c>Axes.quiver</c> draws one given no <c>scale</c>: its arrows
    /// autoscaled to the width of the axes, pointing along the vector as it stands on the screen, the axes ranged over the tails.
    /// Otherwise an arrow <see cref="Models.Series.QuiverSeries.Scale"/> data units long for each unit of its vector.
    /// <c>QuiverSeries.AutoScaledUnder</c> holds the rule.</summary>
    public bool QuiverAutoScale { get; init; }

    /// <summary>Whether a wind barb is drawn as matplotlib's <c>Barbs</c> draws one: one polygon of a staff 24.5 pt long with a flag
    /// for each 50, a full barb for each 10 and a half barb for a remaining 5, a calm drawn as a circle. Otherwise the library's
    /// own barb of lines. <c>BarbsSeriesRenderer</c> holds the rule.</summary>
    public bool BarbsLikeMatplotlib { get; init; }

    /// <summary>Whether the arrow on a streamline is matplotlib's <c>FancyArrowPatch</c> (<c>arrowstyle='-|>'</c>, a filled head).
    /// Otherwise an open head of two strokes. <c>StreamplotSeriesRenderer</c> holds the rule.</summary>
    public bool StreamArrowsLikeMatplotlib { get; init; }
}
