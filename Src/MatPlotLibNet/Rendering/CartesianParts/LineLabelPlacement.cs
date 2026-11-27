// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.CartesianParts;

/// <summary>Where the label of a drawn line stands and the font it is drawn in: the one rule of the labels of a reference line, a span,
/// a trendline, a horizontal level and a Fibonacci retracement. A label is drawn in the tick font in the colour of its line, and stands
/// <see cref="Gap"/> pixels off the line: above the right end of a horizontal line, beside the top of a vertical one, inside the top of a
/// band, or at the end of a trendline that is drawn.</summary>
internal static class LineLabelPlacement
{
    /// <summary>How far a label stands off its line, in pixels.</summary>
    internal const double Gap = 2;

    /// <summary>Above the right end of a horizontal line <paramref name="across"/> the plot: ending <paramref name="inset"/> pixels
    /// before the end, its baseline <see cref="Gap"/> above the line.</summary>
    /// <param name="across">The line as drawn, left to right.</param>
    /// <param name="inset">How far before the end the label ends: none for a reference line or a level, <see cref="Gap"/> for a
    /// retracement's level.</param>
    /// <returns>Where the label stands.</returns>
    internal static TextPlacement AboveEnd(LineSegment across, double inset = 0) =>
        new(new Point(across.To.X - inset, across.To.Y - Gap), TextAlignment.Right);

    /// <summary>Beside the top of a vertical line at <paramref name="x"/>: starting <see cref="Gap"/> to its right, hung from the plot's
    /// top (<see cref="TextVerticalAlignment.Top"/>).</summary>
    /// <param name="x">Where the line stands.</param>
    /// <param name="top">The plot's top.</param>
    /// <returns>Where the label stands.</returns>
    internal static TextPlacement BesideTop(double x, double top) =>
        new(new Point(x + Gap, top), TextAlignment.Left) { Vertical = TextVerticalAlignment.Top };

    /// <summary>Inside the top of a band, at its left: starting <see cref="Gap"/> inside its left edge, hung <see cref="Gap"/> below its
    /// top.</summary>
    /// <param name="left">The band's left edge.</param>
    /// <param name="top">The band's top.</param>
    /// <returns>Where the label stands.</returns>
    internal static TextPlacement InsideTopLeft(double left, double top) =>
        new(new Point(left + Gap, top + Gap), TextAlignment.Left) { Vertical = TextVerticalAlignment.Top };

    /// <summary>Inside the top of a band, centred on <paramref name="centre"/>: hung <see cref="Gap"/> below the top.</summary>
    /// <param name="centre">The band's centre.</param>
    /// <param name="top">The band's top.</param>
    /// <returns>Where the label stands.</returns>
    internal static TextPlacement InsideTopCentre(double centre, double top) =>
        new(new Point(centre, top + Gap), TextAlignment.Center) { Vertical = TextVerticalAlignment.Top };

    /// <summary>At the end of a line that is drawn: starting there.</summary>
    /// <param name="end">The end of the line as drawn.</param>
    /// <returns>Where the label stands.</returns>
    internal static TextPlacement AtEnd(Point end) => new(end, TextAlignment.Left);

    extension(IRenderContext ctx)
    {
        /// <summary>Draws the label of a drawn line where <paramref name="at"/> puts it, in the theme's font for it
        /// (<see cref="ThemeFontExtensions.LineLabelFont"/>, the tick font in the colour of its line).</summary>
        /// <param name="text">The label.</param>
        /// <param name="at">Where it stands (the members of <see cref="LineLabelPlacement"/>).</param>
        /// <param name="theme">The theme the line is drawn under.</param>
        /// <param name="color">The colour of the line.</param>
        internal void DrawLineLabel(string text, TextPlacement at, Theme theme, Color color) =>
            ctx.Draw(TextLabel.Of(text), theme.LineLabelFont(color), at);
    }
}
