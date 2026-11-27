// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Styling;

/// <summary>A line a series draws as a guide to its marks where its author names nothing (a residual plot's zero line): its colour, its
/// opacity, its dash and the rule its width is drawn by.</summary>
/// <param name="Color">The colour of the line; <see langword="null"/> draws it in the colour the series' marks are drawn in.</param>
/// <param name="Opacity">How opaque the line is, from 0 to 1.</param>
/// <param name="Style">The dash of the line.</param>
/// <param name="Width">The rule its width is drawn by.</param>
internal readonly record struct GuideLine(Color? Color, double Opacity, LineStyle Style, LineWidthRule Width)
{
    /// <summary>The stroke the line is drawn with in <paramref name="slot"/>: this look's colour, else the series', at this look's opacity,
    /// dash and width rule. The one place a guide line becomes a stroke.</summary>
    /// <param name="slot">The slot of the series that draws the line.</param>
    /// <param name="series">The colour the series' marks are drawn in.</param>
    /// <returns>The stroke.</returns>
    internal StrokeStyle StrokeIn(SeriesSlot slot, Color series) => slot.StrokeOf((Color ?? series).WithOpacityGiven(Opacity), Width, Style);
}
