// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>The three series lists of an axes, one per axis pair the series are drawn against.</summary>
internal enum SeriesAxisPair
{
    /// <summary><see cref="Axes.Series"/>: the bottom X axis and the left Y axis.</summary>
    Primary,

    /// <summary><see cref="Axes.SecondarySeries"/>: the bottom X axis and the right-hand Y axis.</summary>
    SecondaryY,

    /// <summary><see cref="Axes.XSecondarySeries"/>: the top X axis and the left Y axis.</summary>
    SecondaryX,
}

/// <summary>What the theme hands out at one colour-cycle slot.</summary>
/// <param name="CycleColor">The colour of the slot: the <see cref="Theme.PropCycler"/>'s colour when the theme has a
/// cycler, otherwise <see cref="Theme.CycleColors"/> at the slot, wrapping around. A series renderer receives it as
/// the fallback for a series that names no colour of its own.</param>
/// <param name="CycledProperties">The cycler's whole set for the slot (colour, dash, marker, width), handed to the series
/// renderer alongside the colour; <see langword="null"/> when the theme has no cycler.</param>
/// <param name="ViolinBody">The colour the theme gives the body of every violin (<see cref="Theme.ViolinBodyColor"/>);
/// <see langword="null"/> when it gives none, and a violin takes the slot's colour. It is part of the slot because it
/// decides the colour a violin is drawn in, which the renderer and the legend swatch must agree on.</param>
/// <param name="ViolinStats">The colour the theme gives the lines drawn over every violin
/// (<see cref="Theme.ViolinStatsColor"/>); <see langword="null"/> when it gives none.</param>
/// <remarks>The slot also decides how a series is stroked. A series that leaves its line style, line width or marker at
/// what a new series holds (the <c>Resting…</c> constants below, which the tests pin against the series themselves) takes
/// the cycler's entry for the slot; one that names another value keeps it, as an explicit colour does. A value the series
/// names that equals the resting one cannot be told from one it left alone, which is the rule the JSON already follows
/// (a value at rest is not written), so a figure draws the same picture before and after a round trip. A cycler that
/// lists only colours hands out solid, no marker and 1.5, which is what every series already draws.</remarks>
internal readonly record struct SeriesSlot(
    Color CycleColor, CycledProperties? CycledProperties, Color? ViolinBody = null, Color? ViolinStats = null)
{
    /// <summary>The line style a new line, step or area holds.</summary>
    internal const LineStyle RestingLineStyle = LineStyle.Solid;

    /// <summary>The line width a new line, step, area or error bar holds.</summary>
    internal const double RestingLineWidth = 1.5;

    /// <summary>The marker a new scatter holds.</summary>
    internal const MarkerStyle RestingScatterMarker = MarkerStyle.Circle;

    /// <summary>The colour <paramref name="series"/> is drawn in: its own <see cref="IHasColor.Color"/> when it sets
    /// one, otherwise the colour its theme gives a series of its kind (a violin's body: <see cref="ViolinBody"/>),
    /// otherwise the slot's colour. The legend swatch (and anything else that names a series by its colour)
    /// reads this, so it shows what the series renderer draws.</summary>
    /// <param name="series">The series that holds this slot.</param>
    /// <returns>The series' explicit colour, else the theme's colour for its kind, else <see cref="CycleColor"/>.</returns>
    internal Color ColorOf(ISeries series) =>
        (series as IHasColor)?.Color ?? (series is ViolinSeries ? ViolinBody : null) ?? CycleColor;

    /// <summary>The colour of the lines drawn over a violin (its extrema, median and mean), by the rule of
    /// <see cref="ColorOf"/>: the colour the violin names, else the theme's colour for those lines
    /// (<see cref="ViolinStats"/>), else the slot's colour.</summary>
    /// <param name="violin">The violin that holds this slot.</param>
    /// <returns>The violin's explicit colour, else the theme's colour for its lines, else <see cref="CycleColor"/>.</returns>
    internal Color StatsColorOf(ViolinSeries violin) => violin.Color ?? ViolinStats ?? CycleColor;

    /// <summary>The dash pattern a line is drawn with: the series' own when it names one, otherwise the slot's.</summary>
    /// <param name="drawn">The line style the series holds.</param>
    /// <returns><paramref name="drawn"/> unless it is the resting style and the theme's cycler names another for the slot.</returns>
    internal LineStyle LineStyleOf(LineStyle drawn) =>
        drawn == RestingLineStyle ? CycledProperties?.LineStyle ?? RestingLineStyle : drawn;

    /// <summary>The width, in points, a line is drawn with: the series' own when it names one, otherwise the slot's.</summary>
    /// <param name="drawn">The line width the series holds.</param>
    /// <param name="resting">The width a new series of this kind holds; <see cref="RestingLineWidth"/> for every kind but
    /// the few whose own line is another width (a regression line is 2). The series "names" a width when it holds another
    /// one.</param>
    /// <returns><paramref name="drawn"/> unless it is <paramref name="resting"/> and the theme's cycler names a width for
    /// the slot. A cycler that lists no width hands out <see cref="RestingLineWidth"/>, which cannot be told from one
    /// that lists it, so that width names nothing to a series whose own resting width is another.</returns>
    internal double LineWidthOf(double drawn, double resting = RestingLineWidth) =>
        drawn == resting ? CycledWidth ?? resting : drawn;

    /// <summary>The stroke a line is drawn with: <paramref name="color"/> and the dash pattern and width the series names,
    /// otherwise the slot's (<see cref="LineStyleOf"/>, <see cref="LineWidthOf"/>). The one place a renderer and a legend
    /// swatch build a series' stroke from, so they cannot disagree.</summary>
    /// <param name="color">The colour the series is drawn in.</param>
    /// <param name="lineWidth">The line width the series holds.</param>
    /// <param name="lineStyle">The line style the series holds.</param>
    /// <param name="restingLineWidth">The width a new series of this kind holds (see <see cref="LineWidthOf"/>).</param>
    /// <returns>The stroke to draw with.</returns>
    internal StrokeStyle StrokeOf(Color color, double lineWidth, LineStyle lineStyle, double restingLineWidth = RestingLineWidth) =>
        new(color, LineWidthOf(lineWidth, restingLineWidth), LineStyleOf(lineStyle));

    /// <summary>The marker drawn at each point of a line (or a step): the one the series names, including
    /// <see cref="MarkerStyle.None"/>, otherwise the cycler's marker for the slot.</summary>
    /// <param name="named">The marker the series names; <see langword="null"/> when it names none.</param>
    /// <returns>The marker to draw, or <see langword="null"/> when neither the series nor the cycler names one. A cycler
    /// marker of <see cref="MarkerStyle.None"/> names none: it is what a cycler that lists no markers holds.</returns>
    internal MarkerStyle? LineMarkerOf(MarkerStyle? named) => named ?? CycledMarker;

    /// <summary>The marker a scatter point is drawn with: the series' own when it names one, otherwise the slot's. A
    /// scatter is its markers, so a cycler marker of <see cref="MarkerStyle.None"/> (what a cycler that lists no markers
    /// holds) leaves the scatter's.</summary>
    /// <param name="drawn">The marker the series holds.</param>
    /// <returns><paramref name="drawn"/> unless it is the resting marker and the cycler names a marker for the slot.</returns>
    internal MarkerStyle ScatterMarkerOf(MarkerStyle drawn) =>
        drawn == RestingScatterMarker ? CycledMarker ?? drawn : drawn;

    /// <summary>The cycler's width for the slot, or <see langword="null"/> when there is no cycler or it hands out
    /// <see cref="RestingLineWidth"/>, which is what a cycler that lists no width hands out.</summary>
    private double? CycledWidth =>
        CycledProperties?.LineWidth is { } width && width != RestingLineWidth ? width : null;

    /// <summary>The cycler's marker for the slot, or <see langword="null"/> when it has none or names
    /// <see cref="MarkerStyle.None"/>.</summary>
    private MarkerStyle? CycledMarker =>
        CycledProperties?.MarkerStyle is { } marker and not MarkerStyle.None ? marker : null;
}

/// <summary>The one owner of "the colour of series i" and of how it is stroked. The axes decides which slot a series
/// holds; the theme decides what that slot is worth.</summary>
/// <remarks>Every site that draws a series or names it by colour goes through here: the primary series passes in
/// <see cref="AxesRenderer"/>, both secondary-axis parts, and the legend. Before this owner the secondary parts read
/// <see cref="Theme.CycleColors"/> only while the legend read the <see cref="Theme.PropCycler"/> first, so under a
/// cycler theme a secondary line and its swatch wore different colours. The same goes for the dash, the marker and the
/// width: the series renderers and the legend swatch both ask the slot (the <c>SeriesSlot</c> value this hands out).</remarks>
internal static class SeriesSlotExtensions
{
    /// <summary>What <paramref name="theme"/> hands out at slot <paramref name="slotIndex"/>.</summary>
    /// <param name="theme">The active theme.</param>
    /// <param name="slotIndex">The slot, as <see cref="SlotIndex"/> computes it.</param>
    /// <returns>The slot's colour and, when the theme has a cycler, its full set of cycled properties.</returns>
    internal static SeriesSlot SeriesSlot(this Theme theme, int slotIndex)
    {
        var cycled = theme.PropCycler?[slotIndex];
        return theme.ThemedSlot(cycled?.Color ?? theme.CycleColors[slotIndex % theme.CycleColors.Length], cycled);
    }

    /// <summary>The slot made of a cycle colour and a cycler set already chosen, together with what
    /// <paramref name="theme"/> gives the series that take a colour of the theme's own (a violin). The one place a slot is
    /// built, for the axes that hands out slots and for a renderer that is handed its slot in parts.</summary>
    /// <param name="theme">The active theme.</param>
    /// <param name="cycleColor">The colour of the slot.</param>
    /// <param name="cycled">The cycler's set for the slot; <see langword="null"/> when the theme has no cycler.</param>
    /// <returns>The slot.</returns>
    internal static SeriesSlot ThemedSlot(this Theme theme, Color cycleColor, CycledProperties? cycled) =>
        new(cycleColor, cycled, theme.ViolinBodyColor, theme.ViolinStatsColor);

    /// <summary>The slot a series holds: its position in <see cref="Axes.AllSeries"/>. Primary series hold their own
    /// index, secondary-Y series continue after the primary ones, secondary-X series after both.</summary>
    /// <param name="axes">The axes that owns the series.</param>
    /// <param name="pair">The list the series sits in.</param>
    /// <param name="indexInPair">The series' index within that list.</param>
    /// <returns>The slot index.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="pair"/> is not a defined value.</exception>
    internal static int SlotIndex(this Axes axes, SeriesAxisPair pair, int indexInPair) => pair switch
    {
        SeriesAxisPair.Primary => indexInPair,
        SeriesAxisPair.SecondaryY => axes.Series.Count + indexInPair,
        SeriesAxisPair.SecondaryX => axes.Series.Count + axes.SecondarySeries.Count + indexInPair,
        _ => throw new ArgumentOutOfRangeException(nameof(pair), pair, "Not a series list of an axes."),
    };

    /// <summary>The slot of the series at <paramref name="indexInPair"/> in <paramref name="pair"/>, valued by
    /// <paramref name="theme"/>.</summary>
    /// <param name="axes">The axes that owns the series.</param>
    /// <param name="theme">The active theme.</param>
    /// <param name="pair">The list the series sits in.</param>
    /// <param name="indexInPair">The series' index within that list.</param>
    /// <returns>The slot's colour and cycled properties.</returns>
    internal static SeriesSlot SeriesSlot(this Axes axes, Theme theme, SeriesAxisPair pair, int indexInPair) =>
        theme.SeriesSlot(axes.SlotIndex(pair, indexInPair));
}
