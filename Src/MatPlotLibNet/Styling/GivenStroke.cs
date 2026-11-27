// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>The width and the dash pattern a caller gave a series' line, each <see langword="null"/> while it was not given: the one
/// value every kind whose line takes both holds (a line, a step, a signal, an ECDF, a density, a regression line, the edge of an area,
/// a 3-D, polar or ternary line, a streaming line, an indicator's lines), in place of a hidden field and an accessor for each part. A
/// part that is given is drawn as given, the resting value included; one that is not is the slot's
/// (<c>StrokeOf(color, given, rule)</c> of <see cref="Rendering.SeriesSlotExtensions"/>), so the theme decides it. The JSON writes the given
/// parts only.</summary>
/// <param name="Width">The width the caller gave, in pixels; <see langword="null"/> while none was given.</param>
/// <param name="Style">The dash pattern the caller gave; <see langword="null"/> while none was given.</param>
/// <remarks>A value with its own equality, so a series stores it through its change mark without boxing it, and a setter that gives
/// one part writes a copy with that part (<c>Stroke with { Width = value }</c>).</remarks>
internal readonly record struct GivenStroke(double? Width, LineStyle? Style)
{
    /// <summary>The dash pattern a line reads while none was given, and is drawn with where the cycler names none: solid.</summary>
    internal const LineStyle RestingStyle = LineStyle.Solid;

    /// <summary>A line given <paramref name="width"/> and no dash, whatever the slot's cycler names: the handle of a series that is always
    /// drawn solid (a sparkline, a map's outline).</summary>
    /// <param name="width">The width given, in pixels; <see langword="null"/> while none was given.</param>
    /// <returns>The given stroke.</returns>
    internal static GivenStroke Solid(double? width) => new(width, LineStyle.Solid);

    /// <summary>The dash pattern the series reports: the given one, else <see cref="RestingStyle"/>.</summary>
    internal LineStyle StyleOrResting => Style ?? RestingStyle;

    /// <summary>This stroke laid over <paramref name="held"/>: each part given here wins, and a part not given here keeps the one
    /// <paramref name="held"/> has. An indicator hands its lines what it was given this way, and the JSON's stroke is read this way (a key
    /// that is absent leaves what the series was created with).</summary>
    /// <param name="held">The stroke a series holds.</param>
    /// <returns>The stroke the series holds after.</returns>
    internal GivenStroke Over(GivenStroke held) => new(Width ?? held.Width, Style ?? held.Style);
}
