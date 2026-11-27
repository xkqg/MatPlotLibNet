// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>How wide a kind of series draws a line its author gives no width: the one rule every series line is drawn by
/// (<see cref="WidthOf"/>), with what differs between kinds held as values. matplotlib 3.11.2: a line made by <c>plot</c> takes the
/// width the axes' prop_cycle names, else <c>lines.linewidth</c>; a collection of lines (an eventplot, a streamplot, a wireframe, the
/// lines of a contour, a stem, a violin's extrema) takes <c>lines.linewidth</c> and never the cycler's.</summary>
/// <param name="Resting">The width, in pixels, a series of the kind reads when it was given none, and is drawn at where neither the
/// cycler nor the theme names one.</param>
/// <param name="Cycled">Whether a width the theme's cycler names is drawn: a line's is, a collection's is not.</param>
/// <param name="BeatsTheme">Whether <see cref="Resting"/> is drawn even where the theme names a width (a regression line's 2 under
/// every theme).</param>
/// <param name="Scale">The factor the theme's width (or <see cref="Resting"/>, where the theme names none) is drawn at: seaborn draws
/// a point plot's lines <c>lines.linewidth * 1.8</c> wide.</param>
/// <remarks>A record, not a struct: every rule is made once (a kind's, a look's) and handed on by reference, so the legend swatch
/// that carries one and the slot that reads one copy no more than a reference.</remarks>
internal sealed record LineWidthRule(double Resting, bool Cycled = false, bool BeatsTheme = false, double Scale = 1)
{
    /// <summary>A line (matplotlib's <c>Line2D</c>): the library's width at rest (<see cref="SeriesLineLook.LibraryWidth"/>), the
    /// cycler's width where it names one, the theme's over its own.</summary>
    internal static LineWidthRule Line { get; } = new(SeriesLineLook.LibraryWidth, Cycled: true);

    /// <summary>A collection of lines (matplotlib's <c>LineCollection</c>): as <see cref="Line"/>, but no cycler names its width.</summary>
    internal static LineWidthRule LineCollection { get; } = Line with { Cycled = false };

    /// <summary>A line of the library's own look, with no width of matplotlib's to follow (the arrows of a quiver given a scale and of
    /// its key, a 3-D quiver, the library's barbs, a box's outline, and a violin's median and mean and a point plot's lines under the
    /// library's look): as <see cref="LineCollection"/>, but its own
    /// width beats the theme's.</summary>
    internal static LineWidthRule Own { get; } = LineCollection with { BeatsTheme = true };

    /// <summary>A collection of lines one pixel wide at rest (an eventplot, a reference line, a stem, a streamplot, the lines of a
    /// triangulated contour): as <see cref="LineCollection"/>, with the theme's width drawn where it names one.</summary>
    internal static LineWidthRule OnePixelCollection { get; } = LineCollection with { Resting = 1 };

    /// <summary>A line one pixel wide at rest that takes the cycler's width (a streaming signal).</summary>
    internal static LineWidthRule OnePixelLine { get; } = Line with { Resting = 1 };

    /// <summary>A collection of hairlines, half a pixel wide at rest (a 3-D wireframe).</summary>
    internal static LineWidthRule HalfPixelCollection { get; } = LineCollection with { Resting = 0.5 };

    /// <summary>A collection of wide lines (the stems of a 3-D stem plot).</summary>
    internal static LineWidthRule WideCollection { get; } = LineCollection with { Resting = 2.5 };

    /// <summary>A wide line that takes the cycler's width (the baseline of a 3-D stem plot).</summary>
    internal static LineWidthRule WideLine { get; } = Line with { Resting = 2.5 };

    /// <summary>A line two pixels wide under every theme unless the cycler names a width (a regression line).</summary>
    internal static LineWidthRule TwoPixelLine { get; } = Line with { Resting = 2, BeatsTheme = true };

    /// <summary>A line of the library's own look two pixels wide under every theme (a violin's median under the library's look).</summary>
    internal static LineWidthRule TwoPixelOwn { get; } = Own with { Resting = 2 };

    /// <summary>seaborn's point plot lines: <c>lines.linewidth * 1.8</c>.</summary>
    internal static LineWidthRule SeabornPointplotLines { get; } = LineCollection with { Scale = 1.8 };

    /// <summary>An outline the library draws one pixel wide under every theme (a candle's wick and body, a sunburst wedge, an arrow
    /// head, a ternary triangle, a dashed zero line, a hatch line).</summary>
    internal static LineWidthRule OwnPixel { get; } = Own with { Resting = 1 };

    /// <summary>A hairline the library draws half a pixel wide under every theme (the edge of a 3-D prism, a node, a chord ribbon, a
    /// waterfall connector, the crosshair and dot edges of a relative rotation graph, a 3-D frame line).</summary>
    internal static LineWidthRule OwnHairline { get; } = Own with { Resting = 0.5 };

    /// <summary>A wide mark the library draws whatever the theme (the target of a bullet graph or a waterfall, a gauge's
    /// needle).</summary>
    internal static LineWidthRule OwnMark { get; } = Own with { Resting = 2.5 };

    /// <summary>The width a line of the kind is drawn at: the one its author gave, else the cycler's where the kind takes it, else the
    /// kind's own where it beats the theme, else the theme's (or the kind's own where the theme names none) times
    /// <see cref="Scale"/>.</summary>
    /// <param name="given">The width the author gave, <see langword="null"/> when none.</param>
    /// <param name="cycled">The width the theme's cycler names for the series' slot, <see langword="null"/> when it names none.</param>
    /// <param name="theme">The width the theme names for a line (<see cref="SeriesLineLook.Width"/>), <see langword="null"/> when it
    /// names none.</param>
    /// <returns>The width, in pixels.</returns>
    internal double WidthOf(double? given, double? cycled, double? theme) =>
        given ?? (Cycled ? cycled : null) ?? (BeatsTheme ? Resting : (theme ?? Resting) * Scale);
}
