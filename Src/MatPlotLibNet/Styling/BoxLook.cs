// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Styling;

/// <summary>What a theme draws a box plot with where its author names nothing: each part of a box (its outline, its median, its whiskers,
/// its caps, the observations beyond them, its mean) and where the boxes stand. Every theme names one. The matplotlib themes name
/// matplotlib's <c>Axes.boxplot</c> under their style (<see cref="Matplotlib"/>, the <c>boxplot.*</c> rcParams): a box that takes no colour of
/// the cycle (matplotlib's <c>boxplot</c> asks the cycle for nothing: its median is the fixed <c>C1</c>), has caps, and marks the
/// observations beyond its whiskers. The library's own themes name the look the box has always had (<see cref="Library"/>): the cycle
/// colour, a red median two pixels wide, whiskers a pixel wide and nothing else.</summary>
/// <remarks>A class, so that the slot every series passes by value carries a reference to it and not a copy of its parts.</remarks>
internal sealed record BoxLook : IThemeKeys<BoxLook>
{
    /// <summary>The library's own box: the outline a line's resting width wide in the colour of the series, a red median two pixels wide,
    /// whiskers a pixel wide in the colour of the series, no caps, no observations marked beyond the whiskers, and a green dot eight pixels
    /// across for the mean, standing as the library stands its boxes (<see cref="BoxStanding.Library"/>).</summary>
    internal static BoxLook Library { get; } = new();

    /// <inheritdoc />
    /// <remarks>A look is written whole under its one key (<see cref="BoxLookDto"/>), and only a matplotlib style's
    /// (<see cref="Matplotlib"/>): the library's own look writes no key, it is the one a theme that writes none reads back.</remarks>
    public void WriteKeys(ThemeDto keys) => keys.Box = this == Library ? null : new BoxLookDto
    {
        Box = Outline.Color!.Value, Median = Median.Color!.Value, Whisker = Whisker.Color!.Value,
        WhiskerStyle = Whisker.Style, Cap = Caps!.Value.Color!.Value, CapShare = Caps.Value.Share,
        LineWidth = Outline.Width, FlierMarker = Fliers!.Value.Marker, FlierFace = Fliers.Value.Face,
        FlierEdge = Fliers.Value.Edge!.Value, MeanMarker = Mean.Marker, MeanFace = Mean.Face!.Value,
        MeanEdge = Mean.Edge!.Value, MarkerSize = Mean.Size,
    };

    /// <inheritdoc />
    public static BoxLook ReadKeys(ThemeDto keys) => keys.Box is { } box
        ? Matplotlib(box.Box, box.Median, box.Whisker, box.WhiskerStyle, box.Cap, box.CapShare, box.LineWidth, box.FlierMarker, box.FlierFace,
            box.FlierEdge, box.MeanMarker, box.MeanFace, box.MeanEdge, box.MarkerSize)
        : Library;

    /// <summary>The look of <c>Axes.boxplot</c> under a matplotlib style, from the <c>boxplot.*</c> rcParams: every line the one width,
    /// every marker the one size, the boxes standing as <c>bxp</c> stands them (<see cref="BoxStanding.Matplotlib"/>).</summary>
    /// <param name="box">The colour of the box's outline (<c>boxplot.boxprops.color</c>).</param>
    /// <param name="median">The colour of the median line (<c>boxplot.medianprops.color</c>).</param>
    /// <param name="whisker">The colour of the whiskers (<c>boxplot.whiskerprops.color</c>).</param>
    /// <param name="whiskerStyle">The dash of the whiskers (<c>boxplot.whiskerprops.linestyle</c>).</param>
    /// <param name="cap">The colour of the caps (<c>boxplot.capprops.color</c>).</param>
    /// <param name="capShare">How wide a cap is, as a share of the box's width (<c>Axes.bxp</c>: <c>capwidths = 0.5 * widths</c>).</param>
    /// <param name="lineWidth">The width, in pixels, of the box, the whiskers, the caps and the median (<c>boxplot.*props.linewidth</c>,
    /// 1 pt), and of the edge of a marker (<c>markeredgewidth</c>).</param>
    /// <param name="flierMarker">The marker of an observation beyond a whisker (<c>boxplot.flierprops.marker</c>).</param>
    /// <param name="flierFace">The fill of a flier; <see langword="null"/> leaves it open (<c>markerfacecolor: none</c>).</param>
    /// <param name="flierEdge">The colour of a flier's outline, or of its strokes where the marker is strokes alone
    /// (<c>boxplot.flierprops.markeredgecolor</c>).</param>
    /// <param name="meanMarker">The marker of a box's mean (<c>boxplot.meanprops.marker</c>).</param>
    /// <param name="meanFace">The fill of the mean's marker (<c>boxplot.meanprops.markerfacecolor</c>).</param>
    /// <param name="meanEdge">The outline of the mean's marker (<c>boxplot.meanprops.markeredgecolor</c>).</param>
    /// <param name="markerSize">How large a flier and the mean marker are, in points across (<c>markersize</c>, 6 pt).</param>
    /// <returns>The look.</returns>
    internal static BoxLook Matplotlib(
        Color box, Color median, Color whisker, LineStyle whiskerStyle, Color cap, double capShare, double lineWidth,
        MarkerStyle flierMarker, Color? flierFace, Color flierEdge,
        MarkerStyle meanMarker, Color meanFace, Color meanEdge, double markerSize) => new()
    {
        Outline = new BoxStroke(box, lineWidth),
        Median = new BoxStroke(median, lineWidth),
        Whisker = new BoxStroke(whisker, lineWidth, whiskerStyle),
        Caps = new BoxCaps(cap, capShare, lineWidth),
        Fliers = new BoxMarker(flierMarker, flierFace, flierEdge, markerSize, lineWidth),
        Mean = new BoxMarker(meanMarker, meanFace, meanEdge, markerSize, lineWidth),
        Standing = BoxStanding.Matplotlib,
    };

    /// <summary>The outline of the box: a line's resting width (<see cref="SeriesLineLook.LibraryWidth"/>), in the colour of the series.</summary>
    public BoxStroke Outline { get; init; } = new(null, SeriesLineLook.LibraryWidth);

    /// <summary>The line across the box at its median: red, two pixels wide.</summary>
    public BoxStroke Median { get; init; } = new(Colors.Red, 2);

    /// <summary>The whiskers: a pixel wide, in the colour of the series.</summary>
    public BoxStroke Whisker { get; init; } = new(null, 1);

    /// <summary>The caps across the ends of the whiskers; <see langword="null"/> draws none.</summary>
    public BoxCaps? Caps { get; init; }

    /// <summary>The marks of the observations beyond the whiskers; <see langword="null"/> marks none, whether the series shows its
    /// outliers or not.</summary>
    public BoxMarker? Fliers { get; init; }

    /// <summary>The mark of a box's mean, where the series shows it: a green dot eight pixels across, with no outline.</summary>
    public BoxMarker Mean { get; init; } = new(MarkerStyle.Circle, Colors.Green, null, 8.0.PixelsInPoints, 0);

    /// <summary>Where the boxes stand and how wide they are drawn where the series names no position and no width.</summary>
    public BoxStanding Standing { get; init; } = BoxStanding.Library;
}
