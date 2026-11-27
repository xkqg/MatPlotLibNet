// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>Provides the dash-gap ratio patterns of each <see cref="LineStyle"/> and, for a backend, the lengths a line is stroked
/// with under a theme's look.</summary>
public static class DashPatterns
{
    /// <summary>Returns the dash-gap ratio array of the library's own look for the given line style.</summary>
    /// <param name="style">The line style.</param>
    /// <returns>The lengths of the dashes and the gaps of the library's own look, as shares of the line's width; empty for a
    /// solid line.</returns>
    public static ReadOnlySpan<double> GetPattern(LineStyle style) => DashLook.Library.RatiosOf(style);

    /// <summary>The dashes and gaps a line of <paramref name="style"/> and <paramref name="thickness"/> is stroked with under
    /// <paramref name="look"/>: each ratio of the look's pattern times how many pixels a unit is, and times the width when the look's
    /// dashes grow with it. The one conversion the SVG writer, Skia and MAUI share.</summary>
    /// <param name="style">The line style.</param>
    /// <param name="thickness">The width of the line, in pixels.</param>
    /// <param name="look">The dash look of the theme the line is drawn under.</param>
    /// <returns>The lengths in order; empty for a solid line and for no line.</returns>
    internal static ScaledDashPattern Lengths(LineStyle style, double thickness, DashLook look) =>
        new(look.RatiosOf(style), (look.ScalesWithWidth ? thickness : 1) * look.Unit);
}
