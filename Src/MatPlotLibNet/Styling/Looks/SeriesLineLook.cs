// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Styling;

/// <summary>The width of a series' line that names none, and the cap of an error bar.</summary>
internal sealed record SeriesLineLook : IThemeKeys<SeriesLineLook>
{
    /// <summary>The library's own lines.</summary>
    internal static SeriesLineLook Library { get; } = new();

    /// <inheritdoc />
    public void WriteKeys(ThemeDto keys)
    {
        keys.SeriesLineWidth = Width;
        keys.ErrorBarCapSize = ErrorBarCapSize;
    }

    /// <inheritdoc />
    public static SeriesLineLook ReadKeys(ThemeDto keys) => new() { Width = keys.SeriesLineWidth, ErrorBarCapSize = keys.ErrorBarCapSize };

    /// <summary>The width, in pixels, the library's own look draws a line its author, the theme's cycler and the theme name no width
    /// for: the width a line rests at (<see cref="LineWidthRule.Line"/>), and the width every series of a line reads when it was given
    /// none.</summary>
    internal const double LibraryWidth = 1.5;

    /// <summary>How wide a line of a series is drawn when neither its author nor the theme's cycler names a width, in pixels:
    /// matplotlib's <c>lines.linewidth</c>, 1.5 pt under the default style and 1.0 pt under the classic one; a style's
    /// <c>lines.linewidth</c> lays its own over it. Unset: each kind's own (<see cref="LineWidthRule.Resting"/>), a line's
    /// <see cref="LibraryWidth"/>.</summary>
    public double? Width { get; init; }

    /// <summary>The width a line of a kind drawn with no series slot (the lines of a contour's colour bar, a reference line) is drawn at:
    /// the rule of its kind over the width its author gave and <see cref="Width"/>, none cycled.</summary>
    /// <param name="given">The width its author gave, <see langword="null"/> when none.</param>
    /// <param name="rule">The rule of the kind.</param>
    /// <returns>The width, in pixels.</returns>
    internal double WidthOf(double? given, LineWidthRule rule) => rule.WidthOf(given, null, Width);

    /// <summary>The width the line of <paramref name="kind"/> is drawn at with no cycler (a reference line, a span's border): as
    /// <see cref="WidthOf(double?, LineWidthRule)"/> over the width it was given and its kind's rule.</summary>
    /// <param name="kind">The kind that states its given width and its rule.</param>
    /// <returns>The width, in pixels.</returns>
    internal double WidthOf(IWidthGiven kind) => WidthOf(kind.GivenLineWidth, kind.WidthRule);

    /// <summary>How far the cap of an error bar reaches either side of its bar when its author names no size, in pixels:
    /// matplotlib's <c>errorbar.capsize</c>, 0 under the default style and 3 pt under the classic one. Unset: the series' own.</summary>
    public double? ErrorBarCapSize { get; init; }

    /// <summary>How far an error bar's cap reaches either side of its bar: the size its author named, else
    /// <see cref="ErrorBarCapSize"/>, else the series' own.</summary>
    /// <param name="given">The size the author named, <see langword="null"/> when none.</param>
    /// <param name="resting">The size the series draws its caps at under a look that names none.</param>
    /// <returns>The reach of the cap, in pixels; a cap of 0 or less is not drawn.</returns>
    internal double CapSizeOver(double? given, double resting) => given ?? ErrorBarCapSize ?? resting;
}
