// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models;

/// <summary>Configures the legend display for an axes.</summary>
public sealed record Legend
{
    /// <summary>Whether the legend is drawn. On by default; a legend whose axes has no labelled series draws nothing
    /// whatever this says.</summary>
    public bool Visible { get; init; } = true;

    /// <summary>Where the legend box stands. <see cref="LegendPosition.Best"/> by default, which picks the corner or edge
    /// that covers the fewest marks.</summary>
    public LegendPosition Position { get; init; } = LegendPosition.Best;

    /// <summary>The number of columns the entries are laid out in, filled row by row. Default 1; a value below 1 is
    /// treated as 1.</summary>
    public int NCols { get; init; } = 1;

    /// <summary>The size of the entry text, in the units of the theme's font sizes. <see langword="null"/> (the default)
    /// uses the theme's: the tick-label size, or under the matplotlib themes matplotlib's <c>legend.fontsize</c> (the
    /// font size under v2, 1.2 times it under classic).</summary>
    public double? FontSize { get; init; }

    /// <summary>A heading drawn in bold above the entries. <see langword="null"/> or empty (the default) draws none.</summary>
    public string? Title { get; init; }

    /// <summary>The size of the <see cref="Title"/> text, in the units of the theme's font sizes. <see langword="null"/>
    /// (the default) makes it one unit larger than the theme's tick-label size.</summary>
    public double? TitleFontSize { get; init; }

    /// <summary>Whether a box (background fill and outline) is drawn around the entries. On by default.</summary>
    public bool FrameOn { get; init; } = true;

    /// <summary>The opacity of the frame's background fill, from 0 (clear) to 1 (opaque), so the marks behind the legend
    /// show through at less than 1. Default <see cref="RestingFrameAlpha"/>; read only while <see cref="FrameOn"/> is on. Given none, the frame is as opaque
    /// as the theme says: matplotlib's <c>legend.framealpha</c> under the matplotlib themes (0.8 under v2, opaque under classic) and
    /// <see cref="RestingFrameAlpha"/> under every other theme; a value that is given, the default included, is drawn as given.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is below 0, above 1 or not a number.</exception>
    public double FrameAlpha { get => GivenFrameAlpha ?? RestingFrameAlpha; init => GivenFrameAlpha = value.ThrowIfNotOpacity(nameof(FrameAlpha)); }

    /// <summary>The frame opacity a new legend reads from <see cref="FrameAlpha"/>.</summary>
    internal const double RestingFrameAlpha = 0.8;

    /// <summary>The frame opacity that was given, <see langword="null"/> when none was.</summary>
    internal double? GivenFrameAlpha { get; private init; }

    /// <summary>Asks for a rounded legend frame, as matplotlib's <c>fancybox</c> does: corners of 0.2 of the legend's text size.
    /// Off by default. Given no choice, the frame is rounded as the theme says: matplotlib's <c>legend.fancybox</c> under the
    /// matplotlib themes (on under v2, off under classic) and square under every other theme; a choice that is given, off included, is
    /// drawn as given.</summary>
    public bool FancyBox { get => GivenFancyBox ?? false; init => GivenFancyBox = value; }

    /// <summary>Whether the frame's corners were asked for, <see langword="null"/> when they were not.</summary>
    internal bool? GivenFancyBox { get; private init; }

    /// <summary>How many markers the handle of a line shows, as matplotlib's <c>numpoints</c>: one in the middle of a line that runs
    /// the whole handle, or several spread along a line that runs from the first to the last. <see langword="null"/> (the default)
    /// takes the theme's: matplotlib's <c>legend.numpoints</c> under the matplotlib themes (1 under v2, 2 under classic) and
    /// the library's own count under every other theme.</summary>
    public int? NumPoints { get; init; }

    /// <summary>How many markers the handle of a scatter shows, as matplotlib's <c>scatterpoints</c>. <see langword="null"/> (the
    /// default) takes the theme's: matplotlib's <c>legend.scatterpoints</c> under the matplotlib themes (1 under v2, 3 under classic)
    /// and the library's own count under every other theme, where the marker stands as the library has always drawn it.</summary>
    public int? ScatterPoints { get; init; }

    /// <summary>Whether the frame casts a drop shadow, a darker copy of the box offset a few pixels down and to the right.
    /// Off by default; read only while <see cref="FrameOn"/> is on.</summary>
    public bool Shadow { get; init; }

    /// <summary>The colour of the frame's outline. <see langword="null"/> (the default) draws it as the theme does: in matplotlib's
    /// <c>legend.edgecolor</c> under the matplotlib themes (light grey under v2, black under classic) and in light grey under every
    /// other theme.</summary>
    public Color? EdgeColor { get; init; }

    /// <summary>The colour of the frame's background fill, before <see cref="FrameAlpha"/> is applied. <see langword="null"/>
    /// (the default) takes the theme's background colour.</summary>
    public Color? FaceColor { get; init; }

    /// <summary>A factor on the size of the sample swatch drawn beside each entry's label: 2 makes it twice as long and
    /// twice as tall, 0.5 half. Default 1.</summary>
    public double MarkerScale { get; init; } = 1.0;

    /// <summary>The vertical gap between entries, as a fraction of the entry text size. Default 0.5; a negative value is
    /// treated as 0.</summary>
    public double LabelSpacing { get; init; } = 0.5;

    /// <summary>The horizontal gap between columns of entries, as a multiple of the entry text size. Default 2; it matters
    /// only when <see cref="NCols"/> is above 1.</summary>
    public double ColumnSpacing { get; init; } = 2.0;

    /// <summary>When <see langword="true"/>, each legend entry's label is suffixed with the
    /// series' last data value formatted as <c>" = {value:F2}"</c> (InvariantCulture).
    /// Only <see cref="Models.Series.XYSeries"/> instances yield a value; all other series types
    /// fall back to the label text only. Default <see langword="false"/>.</summary>
    public bool LegendValues { get; init; }
}

/// <summary>Specifies the position of a legend within the axes. A legend placed outside the plot area has its margin reserved by the
/// layout of <c>TightLayout()</c> or <c>ConstrainedLayout()</c>; without either the figure edge may clip it.</summary>
/// <remarks><b>Append-only ordinal contract:</b> never reorder,
/// remove, or renumber.</remarks>
public enum LegendPosition
{
    /// <summary>The place that covers the fewest marks, as matplotlib's <c>loc='best'</c> chooses it: the first of ten places
    /// (upper right, upper left, lower left, lower right, right, centre left, centre right, lower centre, upper centre, centre;
    /// centre right stands where right does) with the lowest score, where a place scores the vertices of lines and the centres of
    /// points inside the legend's box, the bars and boxes that overlap it and the paths that cross it. Every series counts, a hidden one too, and so does the
    /// text of an annotation at its point. Cartesian, polar and 3-D axes look for room; the legend of a map stands in the
    /// upper right. A legend that names no position is at <c>Best</c>, and the theme says what that means:
    /// <see cref="Styling.Theme.DefaultLegendPosition"/> (the upper right under the classic theme, <c>Best</c> otherwise).</summary>
    Best = 0,

    /// <summary>Place the legend in the upper-right corner.</summary>
    UpperRight = 1,

    /// <summary>Place the legend in the upper-left corner.</summary>
    UpperLeft = 2,

    /// <summary>Place the legend in the lower-right corner.</summary>
    LowerRight = 3,

    /// <summary>Place the legend in the lower-left corner.</summary>
    LowerLeft = 4,

    /// <summary>Place the legend along the right edge, vertically centered.</summary>
    Right = 5,

    /// <summary>Place the legend along the left edge, vertically centered.</summary>
    CenterLeft = 6,

    /// <summary>Place the legend along the right edge, vertically centered (alias for Right).</summary>
    CenterRight = 7,

    /// <summary>Place the legend along the bottom edge, horizontally centered.</summary>
    LowerCenter = 8,

    /// <summary>Place the legend along the top edge, horizontally centered.</summary>
    UpperCenter = 9,

    /// <summary>Place the legend at the center of the plot area.</summary>
    Center = 10,

    /// <summary>Place the legend OUTSIDE the plot area, to the right of it.</summary>
    OutsideRight = 11,

    /// <summary>Place the legend OUTSIDE the plot area, to the left of it.</summary>
    OutsideLeft = 12,

    /// <summary>Place the legend OUTSIDE the plot area, above it (below the subplot title).</summary>
    OutsideTop = 13,

    /// <summary>Place the legend OUTSIDE the plot area, below the X-axis labels.</summary>
    OutsideBottom = 14,
}
